using System.Text.Json;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Commands;

// ── ComputePerformanceSnapshotCommand ─────────────────────────────────────────

/// <summary>
/// KPI Calculation Engine entry point for a single educator and period.
///
/// Computes the following metrics from live scheduling / feedback data:
///   • Session Volume    — completed sessions led by the educator
///   • Attendance Rate   — (present + late) / total_enrolled across all completed sessions
///   • Goal Achievement  — sessions with ≥1 worked_on goal / total completed sessions
///   • Parent Satisfaction — avg of ops.parent_feedback.rating for the period
///   • Utilization Rate  — completed / (completed + cancelled + no_show + rescheduled)
///   • Program Completion — student_programs with status 'completed' / total in period (stored in detail JSON)
///
/// Results are written to:
///   1. ops.educator_performance_snapshot (upsert by educator_id + period)
///   2. core.kpi_value (upsert by kpi_id + subject + period) for each metric
/// </summary>
public record ComputePerformanceSnapshotCommand(
    Guid CorporationId,
    Guid EducatorId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<EducatorPerformanceSnapshotDto>;

public class ComputePerformanceSnapshotCommandValidator
    : AbstractValidator<ComputePerformanceSnapshotCommand>
{
    public ComputePerformanceSnapshotCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.PeriodStart).NotEmpty();
        RuleFor(x => x.PeriodEnd).NotEmpty()
            .GreaterThan(x => x.PeriodStart)
            .WithMessage("PeriodEnd must be after PeriodStart.");
    }
}

public sealed class ComputePerformanceSnapshotCommandHandler
    : IRequestHandler<ComputePerformanceSnapshotCommand, EducatorPerformanceSnapshotDto>
{
    private readonly IAppDbContext _db;

    public ComputePerformanceSnapshotCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorPerformanceSnapshotDto> Handle(
        ComputePerformanceSnapshotCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.EducatorId
                                   && e.CorporationId == req.CorporationId
                                   && e.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException(
                $"Educator {req.EducatorId} not found in this corporation.");

        // Inclusive UTC bounds for the period
        var periodFrom = new DateTimeOffset(
            req.PeriodStart.Year, req.PeriodStart.Month, req.PeriodStart.Day,
            0, 0, 0, TimeSpan.Zero);
        var periodTo = new DateTimeOffset(
            req.PeriodEnd.Year, req.PeriodEnd.Month, req.PeriodEnd.Day,
            23, 59, 59, 999, TimeSpan.Zero);

        // ── 1. Session IDs for this educator in the period ────────────────────
        var sessionIds = await (
            from se in _db.SessionEducators
            join s in _db.Sessions on se.SessionId equals s.Id
            where se.EducatorId == req.EducatorId
               && s.CorporationId == req.CorporationId
               && s.StartsAt >= periodFrom && s.StartsAt <= periodTo
               && s.DeletedAt == null
            select new { s.Id, s.Status }
        ).ToListAsync(ct);

        var completedSessionIds = sessionIds
            .Where(s => s.Status == "completed")
            .Select(s => s.Id)
            .ToList();

        var allTrackedSessionIds = sessionIds
            .Where(s => s.Status is "completed" or "cancelled" or "no_show" or "rescheduled")
            .Select(s => s.Id)
            .ToList();

        // ── 2. Session Volume ─────────────────────────────────────────────────
        var sessionCount = completedSessionIds.Count;

        // ── 3. Attendance Rate ────────────────────────────────────────────────
        decimal? attendanceRate = null;
        if (completedSessionIds.Count > 0)
        {
            var attendanceRecords = await _db.Attendances
                .AsNoTracking()
                .Where(a => completedSessionIds.Contains(a.SessionId))
                .Select(a => a.Status)
                .ToListAsync(ct);

            if (attendanceRecords.Count > 0)
            {
                var present = attendanceRecords.Count(s => s is "present" or "late");
                attendanceRate = Math.Round(
                    (decimal)present / attendanceRecords.Count * 100, 2);
            }
        }

        // ── 4. Goal Achievement Rate ──────────────────────────────────────────
        decimal? goalAchievementRate = null;
        if (completedSessionIds.Count > 0)
        {
            var sessionsWithGoals = await _db.SessionGoals
                .AsNoTracking()
                .Where(sg => completedSessionIds.Contains(sg.SessionId) && sg.WorkedOn)
                .Select(sg => sg.SessionId)
                .Distinct()
                .CountAsync(ct);

            goalAchievementRate = Math.Round(
                (decimal)sessionsWithGoals / completedSessionIds.Count * 100, 2);
        }

        // ── 5. Parent Satisfaction ────────────────────────────────────────────
        decimal? parentFeedbackAvg = null;
        var feedbackRatings = await _db.ParentFeedbacks
            .AsNoTracking()
            .Where(pf => pf.EducatorId == req.EducatorId
                      && pf.CorporationId == req.CorporationId
                      && pf.CreatedAt >= periodFrom && pf.CreatedAt <= periodTo
                      && pf.Rating.HasValue)
            .Select(pf => (decimal)pf.Rating!.Value)
            .ToListAsync(ct);

        if (feedbackRatings.Count > 0)
            parentFeedbackAvg = Math.Round(feedbackRatings.Average(), 2);

        // ── 6. Utilization Rate ───────────────────────────────────────────────
        decimal? utilizationRate = null;
        if (allTrackedSessionIds.Count > 0)
        {
            utilizationRate = Math.Round(
                (decimal)completedSessionIds.Count / allTrackedSessionIds.Count * 100, 2);
        }

        // ── 7. Program Completion (stored in detail JSON) ─────────────────────
        decimal? programCompletionRate = null;
        if (completedSessionIds.Count > 0)
        {
            var programStats = await (
                from sp in _db.SessionParticipants
                join prog in _db.StudentPrograms on sp.StudentProgramId equals prog.Id
                where completedSessionIds.Contains(sp.SessionId)
                   && prog.DeletedAt == null
                select new { prog.Id, prog.Status }
            ).Distinct().ToListAsync(ct);

            if (programStats.Count > 0)
            {
                var completed = programStats.Count(p => p.Status == "completed");
                programCompletionRate = Math.Round(
                    (decimal)completed / programStats.Count * 100, 2);
            }
        }

        var detail = JsonSerializer.Serialize(new
        {
            program_completion_rate = programCompletionRate,
            total_sessions_tracked  = allTrackedSessionIds.Count,
            feedback_count          = feedbackRatings.Count
        });

        // ── 8. Upsert educator_performance_snapshot ───────────────────────────
        var existing = await _db.EducatorPerformanceSnapshots
            .FirstOrDefaultAsync(s => s.EducatorId == req.EducatorId
                                   && s.PeriodStart == req.PeriodStart
                                   && s.PeriodEnd == req.PeriodEnd, ct);

        EducatorPerformanceSnapshot snapshot;
        if (existing is not null)
        {
            existing.Refresh(sessionCount, attendanceRate, goalAchievementRate,
                             parentFeedbackAvg, utilizationRate, detail);
            snapshot = existing;
        }
        else
        {
            snapshot = EducatorPerformanceSnapshot.Compute(
                req.CorporationId, req.EducatorId,
                req.PeriodStart, req.PeriodEnd,
                sessionCount, attendanceRate, goalAchievementRate,
                parentFeedbackAvg, utilizationRate, detail);
            _db.EducatorPerformanceSnapshots.Add(snapshot);
        }

        // ── 9. Upsert core.kpi_value for each metric ──────────────────────────
        var kpiDefs = await _db.KpiDefinitions
            .AsNoTracking()
            .Where(k => k.IsActive
                     && (k.CorporationId == null || k.CorporationId == req.CorporationId)
                     && k.Code.StartsWith("educator."))
            .ToDictionaryAsync(k => k.Code, ct);

        var metricsToUpsert = new Dictionary<string, decimal?>
        {
            ["educator.session_volume"]      = sessionCount,
            ["educator.attendance_rate"]     = attendanceRate,
            ["educator.goal_achievement"]    = goalAchievementRate,
            ["educator.parent_satisfaction"] = parentFeedbackAvg,
            ["educator.utilization_rate"]    = utilizationRate,
            ["educator.program_completion"]  = programCompletionRate,
        };

        foreach (var (code, value) in metricsToUpsert)
        {
            if (!kpiDefs.TryGetValue(code, out var kpiDef)) continue;

            var kpiValue = await _db.KpiValues
                .FirstOrDefaultAsync(kv =>
                    kv.CorporationId == req.CorporationId
                 && kv.KpiId == kpiDef.Id
                 && kv.SubjectType == "educator"
                 && kv.SubjectId == req.EducatorId
                 && kv.PeriodStart == req.PeriodStart
                 && kv.PeriodEnd == req.PeriodEnd, ct);

            var rounded = value.HasValue ? Math.Round(value.Value, 4) : (decimal?)null;

            if (kpiValue is not null)
            {
                kpiValue.NumericValue = rounded;
                kpiValue.ComputedAt   = DateTimeOffset.UtcNow;
            }
            else
            {
                _db.KpiValues.Add(new KpiValue
                {
                    CorporationId = req.CorporationId,
                    KpiId         = kpiDef.Id,
                    SubjectType   = "educator",
                    SubjectId     = req.EducatorId,
                    PeriodStart   = req.PeriodStart,
                    PeriodEnd     = req.PeriodEnd,
                    NumericValue  = rounded,
                    ComputedAt    = DateTimeOffset.UtcNow,
                    Detail        = "{}"
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return new EducatorPerformanceSnapshotDto(
            snapshot.Id, snapshot.CorporationId, snapshot.EducatorId,
            $"{educator.FirstName} {educator.LastName}",
            snapshot.PeriodStart, snapshot.PeriodEnd,
            snapshot.SessionCount, snapshot.AttendanceRate,
            snapshot.GoalAchievementRate, snapshot.ParentFeedbackAvg,
            snapshot.UtilizationRate, snapshot.Detail, snapshot.ComputedAt);
    }
}

// ── BulkComputeSnapshotsCommand ───────────────────────────────────────────────

/// <summary>
/// Triggers KPI computation for ALL active educators in a corporation for a given period.
/// Each educator is computed individually and errors are collected (non-fatal per educator).
/// Returns the count of successfully computed snapshots.
/// </summary>
public record BulkComputeSnapshotsCommand(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<int>;

public class BulkComputeSnapshotsCommandValidator
    : AbstractValidator<BulkComputeSnapshotsCommand>
{
    public BulkComputeSnapshotsCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.PeriodStart).NotEmpty();
        RuleFor(x => x.PeriodEnd).NotEmpty()
            .GreaterThan(x => x.PeriodStart)
            .WithMessage("PeriodEnd must be after PeriodStart.");
    }
}

public sealed class BulkComputeSnapshotsCommandHandler
    : IRequestHandler<BulkComputeSnapshotsCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ISender _sender;

    public BulkComputeSnapshotsCommandHandler(IAppDbContext db, ISender sender)
    {
        _db     = db;
        _sender = sender;
    }

    public async Task<int> Handle(BulkComputeSnapshotsCommand req, CancellationToken ct)
    {
        var educatorIds = await _db.Educators
            .AsNoTracking()
            .Where(e => e.CorporationId == req.CorporationId
                     && e.IsActive
                     && e.DeletedAt == null)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var success = 0;
        foreach (var educatorId in educatorIds)
        {
            try
            {
                await _sender.Send(new ComputePerformanceSnapshotCommand(
                    req.CorporationId, educatorId,
                    req.PeriodStart, req.PeriodEnd), ct);
                success++;
            }
            catch
            {
                // Individual failures are non-fatal; continue with remaining educators.
            }
        }

        return success;
    }
}
