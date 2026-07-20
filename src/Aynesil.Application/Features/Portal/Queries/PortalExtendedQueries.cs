using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Application.Features.Portal.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Portal.Queries;

// ── VerifyPortalAccessHelper ──────────────────────────────────────────────────
// Shared helper — verifies guardian has active portal access to the student
// and returns the access flags. Throws UnauthorizedAccessException if not.

internal static class PortalAccessHelper
{
    public static async Task<(Guid GuardianId, bool CanViewSessions, bool CanViewAttendance,
        bool CanViewReports, bool CanViewPlan, bool CanViewFinance)>
        VerifyAsync(IAppDbContext db, Guid studentId, Guid guardianUserId, CancellationToken ct)
    {
        var access = await (
            from a in db.GuardianPortalAccesses.AsNoTracking()
            join g in db.Guardians.AsNoTracking() on a.GuardianId equals g.Id
            where g.UserId == guardianUserId
               && a.StudentId == studentId
               && a.RevokedAt == null
            select new
            {
                a.GuardianId, a.CanViewSessions, a.CanViewAttendance,
                a.CanViewReports, a.CanViewPlan, a.CanViewFinance
            }
        ).FirstOrDefaultAsync(ct);

        if (access is null)
            throw new UnauthorizedAccessException(
                $"Guardian does not have active portal access to student {studentId}.");

        return (access.GuardianId, access.CanViewSessions, access.CanViewAttendance,
                access.CanViewReports, access.CanViewPlan, access.CanViewFinance);
    }
}

// ── GetPortalDashboardQuery ───────────────────────────────────────────────────

public record GetPortalDashboardQuery(Guid StudentId, Guid GuardianUserId)
    : IRequest<PortalDashboardDto>;

public sealed class GetPortalDashboardQueryHandler
    : IRequestHandler<GetPortalDashboardQuery, PortalDashboardDto>
{
    private readonly IAppDbContext _db;

    public GetPortalDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PortalDashboardDto> Handle(GetPortalDashboardQuery req, CancellationToken ct)
    {
        var (guardianId, canViewSessions, canViewAttendance, _, _, canViewFinance) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        int? upcomingSessions = null;
        if (canViewSessions)
            upcomingSessions = await _db.Sessions.AsNoTracking()
                .Where(s => s.StartsAt >= DateTimeOffset.UtcNow
                         && s.Status != "cancelled" && s.DeletedAt == null
                         && s.Participants.Any(p => p.StudentId == req.StudentId))
                .CountAsync(ct);

        int? unreadNotifications = await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == req.GuardianUserId && n.ReadAt == null
                     && n.Status != "cancelled")
            .CountAsync(ct);

        decimal? packageBalance = null;
        if (canViewFinance)
            packageBalance = await _db.StudentPackages.AsNoTracking()
                .Where(p => p.StudentId == req.StudentId && p.Status == "active")
                .SelectMany(p => _db.CreditLedgerEntries.AsNoTracking()
                    .Where(l => l.StudentPackageId == p.Id))
                .SumAsync(l => (decimal?)l.Delta, ct) ?? 0;

        int? activeGoals = await _db.StudentGoals.AsNoTracking()
            .Where(g => g.StudentId == req.StudentId && g.Status == "active")
            .CountAsync(ct);

        return new PortalDashboardDto(
            req.StudentId,
            UpcomingSessions: upcomingSessions,
            UnreadNotifications: unreadNotifications ?? 0,
            PackageBalance: packageBalance,
            ActiveGoals: activeGoals);
    }
}

// ── GetPortalSessionHistoryQuery ──────────────────────────────────────────────

public record GetPortalSessionHistoryQuery(
    Guid StudentId,
    Guid GuardianUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<IReadOnlyList<PortalSessionDto>>;

public sealed class GetPortalSessionHistoryQueryHandler
    : IRequestHandler<GetPortalSessionHistoryQuery, IReadOnlyList<PortalSessionDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalSessionHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalSessionDto>> Handle(
        GetPortalSessionHistoryQuery req, CancellationToken ct)
    {
        var (_, canView, _, _, _, _) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        if (!canView)
            return [];

        return await (
            from s in _db.Sessions.AsNoTracking()
            join sp in _db.SessionParticipants.AsNoTracking()
                on s.Id equals sp.SessionId
            where sp.StudentId == req.StudentId
               && s.DeletedAt == null
            orderby s.StartsAt descending
            select new PortalSessionDto(
                s.Id, s.Title, s.StartsAt, s.EndsAt, s.Status)
        ).Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToListAsync(ct);
    }
}

// ── GetPortalAttendanceQuery ───────────────────────────────────────────────────

public record GetPortalAttendanceQuery(
    Guid StudentId,
    Guid GuardianUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<IReadOnlyList<PortalAttendanceDto>>;

public sealed class GetPortalAttendanceQueryHandler
    : IRequestHandler<GetPortalAttendanceQuery, IReadOnlyList<PortalAttendanceDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalAttendanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalAttendanceDto>> Handle(
        GetPortalAttendanceQuery req, CancellationToken ct)
    {
        var (_, _, canView, _, _, _) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        if (!canView)
            return [];

        return await (
            from a in _db.Attendances.AsNoTracking()
            join s in _db.Sessions.AsNoTracking()
                on a.SessionId equals s.Id
            where a.StudentId == req.StudentId
               && s.DeletedAt == null
            orderby s.StartsAt descending
            select new PortalAttendanceDto(
                s.Id, s.Title, s.StartsAt, a.Status, a.ReasonId)
        ).Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToListAsync(ct);
    }
}

// ── GetPortalPackagesQuery ─────────────────────────────────────────────────────

public record GetPortalPackagesQuery(Guid StudentId, Guid GuardianUserId)
    : IRequest<IReadOnlyList<PortalPackageDto>>;

public sealed class GetPortalPackagesQueryHandler
    : IRequestHandler<GetPortalPackagesQuery, IReadOnlyList<PortalPackageDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalPackagesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalPackageDto>> Handle(
        GetPortalPackagesQuery req, CancellationToken ct)
    {
        var (_, _, _, _, _, canView) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        if (!canView)
            return [];

        return await (
            from pkg in _db.StudentPackages.AsNoTracking()
            where pkg.StudentId == req.StudentId
            let remaining = _db.CreditLedgerEntries.AsNoTracking()
                .Where(l => l.StudentPackageId == pkg.Id)
                .Sum(l => (decimal?)l.Delta) ?? 0
            select new PortalPackageDto(
                pkg.Id, pkg.StudentId, pkg.TotalCredits,
                remaining, pkg.ExpiresOn, pkg.Status)
        ).ToListAsync(ct);
    }
}

// ── GetPortalDocumentsQuery ────────────────────────────────────────────────────

public record GetPortalDocumentsQuery(Guid StudentId, Guid GuardianUserId)
    : IRequest<IReadOnlyList<PortalDocumentDto>>;

public sealed class GetPortalDocumentsQueryHandler
    : IRequestHandler<GetPortalDocumentsQuery, IReadOnlyList<PortalDocumentDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalDocumentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalDocumentDto>> Handle(
        GetPortalDocumentsQuery req, CancellationToken ct)
    {
        var (_, _, _, canView, _, _) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        if (!canView)
            return [];

        return await (
            from fa in _db.FileAttachments.AsNoTracking()
            join fo in _db.FileObjects.AsNoTracking() on fa.FileId equals fo.Id
            where fa.OwnerSchema == "students"
               && fa.OwnerId == req.StudentId
               && fo.DeletedAt == null
            orderby fo.CreatedAt descending
            select new PortalDocumentDto(
                fo.Id, fo.OriginalName, fa.Purpose,
                fo.MimeType, fo.ByteSize, fo.CreatedAt)
        ).ToListAsync(ct);
    }
}

// ── GetPortalEducationPlanQuery (BEP) ─────────────────────────────────────────

public record GetPortalEducationPlanQuery(Guid StudentId, Guid GuardianUserId)
    : IRequest<IReadOnlyList<PortalEducationPlanDto>>;

public sealed class GetPortalEducationPlanQueryHandler
    : IRequestHandler<GetPortalEducationPlanQuery, IReadOnlyList<PortalEducationPlanDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalEducationPlanQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalEducationPlanDto>> Handle(
        GetPortalEducationPlanQuery req, CancellationToken ct)
    {
        var (_, _, _, _, canView, _) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        if (!canView)
            return [];

        return await _db.EducationPlans.AsNoTracking()
            .Where(ep => ep.StudentId == req.StudentId
                      && ep.Status == "approved"
                      && ep.GuardianVisible == true
                      && ep.DeletedAt == null)
            .OrderByDescending(ep => ep.EffectiveFrom)
            .Select(ep => new PortalEducationPlanDto(
                ep.Id, ep.Title, ep.Version, ep.Status,
                ep.EffectiveFrom, ep.EffectiveTo))
            .ToListAsync(ct);
    }
}

// ── GetPortalGoalProgressQuery ────────────────────────────────────────────────

public record GetPortalGoalProgressQuery(Guid StudentId, Guid GuardianUserId)
    : IRequest<IReadOnlyList<PortalGoalProgressDto>>;

public sealed class GetPortalGoalProgressQueryHandler
    : IRequestHandler<GetPortalGoalProgressQuery, IReadOnlyList<PortalGoalProgressDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalGoalProgressQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalGoalProgressDto>> Handle(
        GetPortalGoalProgressQuery req, CancellationToken ct)
    {
        var (_, _, _, _, canView, _) =
            await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        if (!canView)
            return [];

        return await (
            from g in _db.StudentGoals.AsNoTracking()
            where g.StudentId == req.StudentId && g.Status == "active"
            let latest = g.ProgressRecords
                .Where(p => p.StudentGoalId == g.Id)
                .OrderByDescending(p => p.MeasuredOn)
                .FirstOrDefault()
            select new PortalGoalProgressDto(
                g.Id, g.Statement, g.Horizon, g.Status,
                latest != null ? latest.PercentComplete : null,
                latest != null ? latest.Trend : null,
                g.TargetDate)
        ).ToListAsync(ct);
    }
}

// ── GetPortalNotificationsQuery ───────────────────────────────────────────────

public record GetPortalNotificationsQuery(
    Guid GuardianUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<IReadOnlyList<NotificationListItemDto>>;

public sealed class GetPortalNotificationsQueryHandler
    : IRequestHandler<GetPortalNotificationsQuery, IReadOnlyList<NotificationListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalNotificationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationListItemDto>> Handle(
        GetPortalNotificationsQuery req, CancellationToken ct)
    {
        return await (
            from n in _db.Notifications.AsNoTracking()
            join cat in _db.RefValues.AsNoTracking()
                on n.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            where n.RecipientUserId == req.GuardianUserId
               && n.Status != "cancelled"
            orderby n.CreatedAt descending
            select new NotificationListItemDto(
                n.Id, n.CategoryId, cat != null ? cat.Code : null,
                n.Subject, n.Body, n.Status,
                n.CreatedAt, n.ReadAt, n.ReadAt != null)
        ).Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToListAsync(ct);
    }
}

// ── GetPortalMeetingHistoryQuery ──────────────────────────────────────────────

public record GetPortalMeetingHistoryQuery(
    Guid StudentId,
    Guid GuardianUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<IReadOnlyList<PortalMeetingDto>>;

public sealed class GetPortalMeetingHistoryQueryHandler
    : IRequestHandler<GetPortalMeetingHistoryQuery, IReadOnlyList<PortalMeetingDto>>
{
    private readonly IAppDbContext _db;

    public GetPortalMeetingHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalMeetingDto>> Handle(
        GetPortalMeetingHistoryQuery req, CancellationToken ct)
    {
        // Verify portal access (no specific flag for meetings — general portal access sufficient)
        await PortalAccessHelper.VerifyAsync(_db, req.StudentId, req.GuardianUserId, ct);

        // Find the guardian's ID from user_id
        var guardianId = await _db.Guardians.AsNoTracking()
            .Where(g => g.UserId == req.GuardianUserId)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);

        if (guardianId is null)
            return [];

        return await (
            from mp in _db.MeetingParticipants.AsNoTracking()
            join m in _db.Meetings.AsNoTracking() on mp.MeetingId equals m.Id
            where mp.GuardianId == guardianId
               && m.DeletedAt == null
            orderby m.ScheduledAt descending
            select new PortalMeetingDto(
                m.Id, m.Title, m.ScheduledAt, m.EndsAt,
                m.Status, m.Location, mp.Attendance)
        ).Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToListAsync(ct);
    }
}
