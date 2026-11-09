using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling;

/// <summary>
/// Consumes one package credit per attending participant when a session is completed.
///
/// Rules:
///   - Only participants whose attendance is present / late / left_early consume.
///     Absent, excused or no attendance recorded → nothing is consumed.
///   - The session's program is resolved per participant: the participant's
///     student_program first, otherwise the program of the session's program_service.
///   - Package: the student's active package whose package_definition.program_id is
///     that program, not expired on the session date, with ≥ 1 remaining credit.
///     Among several, the one expiring soonest is used (no expiry = last).
///   - Idempotent: a student who already has a 'consume' entry for this session is skipped
///     (completed → completed is an allowed transition).
///   - No matching package → nothing is consumed; the session detail shows it.
/// Runs inside the caller's SaveChanges, so completion and consumption commit together.
/// </summary>
internal static class SessionCreditConsumer
{
    private static readonly string[] AttendedStatuses = ["present", "late", "left_early"];
    private const decimal CreditsPerSession = 1m;

    public static async Task ConsumeForCompletedSessionAsync(
        IAppDbContext db, Session session, Guid? userId, CancellationToken ct)
    {
        var participants = await db.SessionParticipants.AsNoTracking()
            .Where(p => p.SessionId == session.Id)
            .Select(p => new { p.StudentId, p.StudentProgramId })
            .ToListAsync(ct);
        if (participants.Count == 0) return;

        var attended = (await db.Attendances.AsNoTracking()
                .Where(a => a.SessionId == session.Id && AttendedStatuses.Contains(a.Status))
                .Select(a => a.StudentId)
                .ToListAsync(ct))
            .ToHashSet();

        var alreadyConsumed = (await (
                from l in db.CreditLedgerEntries.AsNoTracking()
                join p in db.StudentPackages.AsNoTracking() on l.StudentPackageId equals p.Id
                where l.SessionId == session.Id && l.EntryType == "consume"
                select p.StudentId)
                .ToListAsync(ct))
            .ToHashSet();

        Guid? serviceProgramId = session.ProgramServiceId is { } serviceId
            ? await db.ProgramServices.AsNoTracking()
                .Where(ps => ps.Id == serviceId)
                .Select(ps => (Guid?)ps.ProgramId)
                .FirstOrDefaultAsync(ct)
            : null;

        var studentProgramIds = participants
            .Where(p => p.StudentProgramId.HasValue)
            .Select(p => p.StudentProgramId!.Value)
            .ToList();
        var studentProgramMap = await db.StudentPrograms.AsNoTracking()
            .Where(sp => studentProgramIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, sp => sp.ProgramId, ct);

        var sessionDate = DateOnly.FromDateTime(session.StartsAt.UtcDateTime);

        foreach (var participant in participants)
        {
            if (!attended.Contains(participant.StudentId)) continue;
            if (alreadyConsumed.Contains(participant.StudentId)) continue;

            Guid? programId =
                participant.StudentProgramId is { } spId && studentProgramMap.TryGetValue(spId, out var pid)
                    ? pid
                    : serviceProgramId;
            if (programId is null) continue;

            var candidates = await db.StudentPackages
                .Include(p => p.CreditLedgerEntries)
                .Where(p => p.StudentId == participant.StudentId
                         && p.Status == "active"
                         && p.PackageDefinition != null
                         && p.PackageDefinition.ProgramId == programId
                         && (p.ExpiresOn == null || p.ExpiresOn >= sessionDate))
                .OrderBy(p => p.ExpiresOn == null)
                .ThenBy(p => p.ExpiresOn)
                .ThenBy(p => p.PurchasedOn)
                .ToListAsync(ct);

            var package = candidates.FirstOrDefault(
                p => p.CreditLedgerEntries.Sum(e => e.Delta) >= CreditsPerSession);
            if (package is null) continue;

            var remaining = package.CreditLedgerEntries.Sum(e => e.Delta);
            var entry = package.ConsumeCredits(
                CreditsPerSession, session.Id, "Session completed", userId);
            db.CreditLedgerEntries.Add(entry);

            if (remaining - CreditsPerSession <= 0)
                package.MarkExhausted();
        }
    }

    /// <summary>Net credits consumed per student for a session (consume entries linked to it).</summary>
    public static async Task<Dictionary<Guid, decimal>> LoadConsumedBySessionAsync(
        IAppDbContext db, Guid sessionId, CancellationToken ct)
    {
        var rows = await (
                from l in db.CreditLedgerEntries.AsNoTracking()
                join p in db.StudentPackages.AsNoTracking() on l.StudentPackageId equals p.Id
                where l.SessionId == sessionId && l.EntryType == "consume"
                select new { p.StudentId, l.Delta })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.StudentId)
            .ToDictionary(g => g.Key, g => -g.Sum(r => r.Delta));
    }
}
