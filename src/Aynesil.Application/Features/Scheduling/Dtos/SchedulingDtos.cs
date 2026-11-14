using Aynesil.Application.Common.Educators;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.RefData;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Dtos;

// ── Room DTOs ─────────────────────────────────────────────────────────────────

public record RoomDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? RoomTypeId,
    string Code,
    string Name,
    int Capacity,
    bool IsVirtual,
    string? MeetingUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    string? CampusName = null);

public record RoomListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    string Code,
    string Name,
    int Capacity,
    bool IsVirtual,
    bool IsActive,
    string? CampusName = null);

// ── Calendar Entry DTOs ───────────────────────────────────────────────────────

public record CalendarEntryDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    string Title,
    string EntryType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAllDay,
    DateTimeOffset CreatedAt);

// ── Recurring Schedule DTOs ───────────────────────────────────────────────────

public record RecurringScheduleDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? StudentProgramId,
    Guid? SessionTypeId,
    Guid? RoomId,
    string Frequency,
    int IntervalCount,
    int[]? ByWeekday,
    int[]? ByMonthday,
    TimeOnly StartTime,
    int DurationMinutes,
    DateOnly RangeStart,
    DateOnly? RangeEnd,
    int? MaxOccurrences,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int RowVersion,
    Guid? ProgramServiceId,
    IReadOnlyList<RecurringScheduleEducatorDto> Educators);

public record RecurringScheduleEducatorDto(Guid EducatorId, string Role);

public record RecurringScheduleListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? SessionTypeId,
    string Frequency,
    TimeOnly StartTime,
    int DurationMinutes,
    DateOnly RangeStart,
    DateOnly? RangeEnd,
    bool IsActive,
    string? CampusName,
    Guid? RoomId,
    string? RoomName,
    Guid? StudentProgramId,
    string? StudentFullName,
    string? ProgramName,
    string? ProgramServiceName,
    string? LeadEducatorName,
    int GeneratedCount,
    int[]? ByWeekday);

// ── Session DTOs ──────────────────────────────────────────────────────────────

public record SessionDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid SessionTypeId,
    Guid? RoomId,
    string? RoomName,
    Guid? RecurringScheduleId,
    Guid? ProgramServiceId,
    string? Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    bool IsMakeup,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<SessionParticipantDto> Participants,
    IReadOnlyList<SessionEducatorDto> Educators,
    IReadOnlyList<SessionGoalDto> Goals,
    IReadOnlyList<SessionNoteDto> Notes,
    IReadOnlyList<AttendanceDto> Attendances,
    string? CampusName = null,
    string? SessionTypeName = null);

public record SessionListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid SessionTypeId,
    Guid? RoomId,
    string? RoomName,
    string? Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    bool IsMakeup,
    int ParticipantCount,
    int EducatorCount,
    string? CampusName = null,
    string? SessionTypeName = null);

public record SessionParticipantDto(
    Guid Id,
    Guid SessionId,
    Guid StudentId,
    string StudentFullName,
    Guid? StudentProgramId,
    string Role,
    decimal CreditsConsumed = 0);   // package credits consumed by this session (auto on completion)

public record SessionEducatorDto(
    Guid Id,
    Guid SessionId,
    Guid EducatorId,
    string EducatorFullName,
    string Role);

public record SessionGoalDto(
    Guid Id,
    Guid SessionId,
    Guid StudentGoalId,
    string GoalStatement,
    bool WorkedOn,
    string? ProgressNote,
    decimal? MeasuredValue);

public record SessionNoteDto(
    Guid Id,
    Guid SessionId,
    Guid? AuthoredBy,
    string Body,
    bool ParentVisible,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    string? AuthorName = null,
    DateTimeOffset? EditedAt = null,
    Guid? StudentId = null,
    string? StudentName = null);

// ── Attendance DTOs ───────────────────────────────────────────────────────────

public record AttendanceDto(
    Guid Id,
    Guid SessionId,
    Guid StudentId,
    string StudentFullName,
    string Status,
    Guid? ReasonId,
    int? MinutesAttended,
    string? Note,
    Guid? RecordedBy,
    DateTimeOffset RecordedAt,
    string? ReasonLabel = null,
    string? RecordedByName = null);

public record AttendanceSummaryDto(
    Guid StudentId,
    string StudentFullName,
    int TotalSessions,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    int LeftEarlyCount,
    decimal AttendanceRate);

// ── Makeup DTOs ───────────────────────────────────────────────────────────────

public record MakeupRequestDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string StudentFullName,
    Guid? MissedSessionId,
    Guid? MissedReasonId,
    string Status,
    Guid? RequestedBy,
    DateTimeOffset RequestedAt,
    Guid? MakeupSessionId,
    DateTimeOffset? CompletedAt,
    DateOnly? ExpiresOn,
    string? Note,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record MakeupRequestListItemDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    Guid? MissedSessionId,
    string Status,
    DateTimeOffset RequestedAt,
    DateOnly? ExpiresOn);

// ── Conflict DTOs ─────────────────────────────────────────────────────────────

public record ConflictCheckDto(
    bool HasRoomConflict,
    bool HasEducatorConflict,
    Guid? ConflictingSessionId,
    Guid? ConflictingEducatorId);

// ── Bulk Operation DTOs ───────────────────────────────────────────────────────

public record BulkOperationResultDto(
    int AffectedCount,
    string Message,
    int RoomConflictSkipped = 0,
    int EducatorConflictSkipped = 0);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class SchedulingProjection
{
    public static async Task<SessionDto?> LoadSessionAsync(
        IAppDbContext db, Guid sessionId, string? locale, CancellationToken ct)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .Include(s => s.Room)
            .Include(s => s.Participants)
            .Include(s => s.Educators)
            .Include(s => s.Goals)
            .Include(s => s.Notes)
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null) return null;

        var campusName = session.CampusId is null ? null
            : await db.Campuses.AsNoTracking()
                .Where(c => c.Id == session.CampusId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);

        // Attendance recorders (iam.user_account) — missing user → null name
        var userIds = session.Attendances.Where(a => a.RecordedBy.HasValue).Select(a => a.RecordedBy!.Value)
            .Distinct().ToList();
        var userMap = await LoadUserNamesAsync(db, userIds, ct);

        // Note authors are educators (session_note.authored_by → educators.educator)
        var authorIds = session.Notes.Where(n => n.AuthoredBy.HasValue).Select(n => n.AuthoredBy!.Value)
            .Distinct().ToList();
        var authorMap = await ActingEducator.LoadNamesAsync(db, authorIds, ct);

        // ref_value labels: session type + attendance reasons (inactive values included)
        var refIds = session.Attendances.Where(a => a.ReasonId.HasValue).Select(a => a.ReasonId!.Value)
            .Append(session.SessionTypeId).Distinct().ToList();
        var refMap = await RefLabelLoader.LoadAsync(db, refIds, locale, ct);

        // Load student names for participants
        var studentIds = session.Participants.Select(p => p.StudentId)
            .Concat(session.Notes.Where(n => n.StudentId.HasValue).Select(n => n.StudentId!.Value)).Distinct().ToList();
        var students = await db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.FirstName, s.LastName })
            .ToListAsync(ct);
        var studentMap = students.ToDictionary(s => s.Id, s => $"{s.FirstName} {s.LastName}".Trim());

        // Load educator names
        var educatorIds = session.Educators.Select(e => e.EducatorId).ToList();
        var educators = await db.Educators.AsNoTracking()
            .Where(e => educatorIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToListAsync(ct);
        var educatorMap = educators.ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim());

        // Load goal statements
        var goalIds = session.Goals.Select(g => g.StudentGoalId).ToList();
        var goalStatements = await db.StudentGoals.AsNoTracking()
            .Where(g => goalIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Statement })
            .ToListAsync(ct);
        var goalMap = goalStatements.ToDictionary(g => g.Id, g => g.Statement);

        // Load attendance student names (may overlap with participants)
        var attendanceStudentIds = session.Attendances.Select(a => a.StudentId)
            .Except(studentIds).ToList();
        if (attendanceStudentIds.Count > 0)
        {
            var extra = await db.Students.AsNoTracking()
                .Where(s => attendanceStudentIds.Contains(s.Id))
                .Select(s => new { s.Id, s.FirstName, s.LastName })
                .ToListAsync(ct);
            foreach (var s in extra) studentMap[s.Id] = $"{s.FirstName} {s.LastName}".Trim();
        }

        var creditMap = await SessionCreditConsumer.LoadConsumedBySessionAsync(db, sessionId, ct);

        return ToSessionDto(session, studentMap, educatorMap, goalMap, creditMap, campusName, userMap, refMap, authorMap);
    }

    public static SessionDto ToSessionDto(
        Domain.Modules.Scheduling.Entities.Session s,
        Dictionary<Guid, string> studentMap,
        Dictionary<Guid, string> educatorMap,
        Dictionary<Guid, string> goalMap,
        Dictionary<Guid, decimal>? creditMap = null,
        string? campusName = null,
        Dictionary<Guid, string>? userMap = null,
        Dictionary<Guid, string>? refMap = null,
        Dictionary<Guid, string>? authorMap = null)
        => new(
            s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
            s.RoomId, s.Room?.Name,
            s.RecurringScheduleId, s.ProgramServiceId, s.Title,
            s.StartsAt, s.EndsAt, s.Status, s.IsMakeup, s.CancelReason,
            s.CreatedAt, s.UpdatedAt, s.RowVersion,
            s.Participants.Select(p => new SessionParticipantDto(
                p.Id, p.SessionId, p.StudentId,
                studentMap.GetValueOrDefault(p.StudentId, ""),
                p.StudentProgramId, p.Role,
                creditMap?.GetValueOrDefault(p.StudentId) ?? 0)).ToList(),
            s.Educators.Select(e => new SessionEducatorDto(
                e.Id, e.SessionId, e.EducatorId,
                educatorMap.GetValueOrDefault(e.EducatorId, ""),
                e.Role)).ToList(),
            s.Goals.Select(g => new SessionGoalDto(
                g.Id, g.SessionId, g.StudentGoalId,
                goalMap.GetValueOrDefault(g.StudentGoalId, ""),
                g.WorkedOn, g.ProgressNote, g.MeasuredValue)).ToList(),
            s.Notes.Select(n => new SessionNoteDto(
                n.Id, n.SessionId, n.AuthoredBy, n.Body, n.ParentVisible,
                n.CreatedAt, n.UpdatedAt, n.RowVersion,
                n.AuthoredBy.HasValue ? authorMap?.GetValueOrDefault(n.AuthoredBy.Value) : null,
                n.EditedAt,
                n.StudentId,
                n.StudentId.HasValue ? studentMap.GetValueOrDefault(n.StudentId.Value) : null)).ToList(),
            s.Attendances.Select(a => new AttendanceDto(
                a.Id, a.SessionId, a.StudentId,
                studentMap.GetValueOrDefault(a.StudentId, ""),
                a.Status, a.ReasonId, a.MinutesAttended,
                a.Note, a.RecordedBy, a.RecordedAt,
                a.ReasonId.HasValue ? refMap?.GetValueOrDefault(a.ReasonId.Value) : null,
                a.RecordedBy.HasValue ? userMap?.GetValueOrDefault(a.RecordedBy.Value) : null)).ToList(),
            campusName,
            refMap?.GetValueOrDefault(s.SessionTypeId));

    public static async Task<Dictionary<Guid, string>> LoadUserNamesAsync(
        IAppDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => ids.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.UserAccounts.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

    /// <summary>Liste/takvim satırlarına seans türü adını (ref_value) ekler.</summary>
    public static async Task<List<SessionListItemDto>> WithSessionTypeNamesAsync(
        IAppDbContext db, IReadOnlyList<SessionListItemDto> items, string? locale, CancellationToken ct)
    {
        var labels = await RefLabelLoader.LoadAsync(
            db, items.Select(i => i.SessionTypeId).Distinct().ToList(), locale, ct);
        return items.Select(i => i with { SessionTypeName = labels.GetValueOrDefault(i.SessionTypeId) }).ToList();
    }

    /// <summary>Yoklama satırlarına neden etiketini (ref_value) ve kaydeden kullanıcı adını ekler.</summary>
    public static async Task<List<AttendanceDto>> WithAttendanceLabelsAsync(
        IAppDbContext db, IReadOnlyList<AttendanceDto> items, string? locale, CancellationToken ct)
    {
        var labels = await RefLabelLoader.LoadAsync(
            db, items.Where(i => i.ReasonId.HasValue).Select(i => i.ReasonId!.Value).Distinct().ToList(), locale, ct);
        var users = await LoadUserNamesAsync(
            db, items.Where(i => i.RecordedBy.HasValue).Select(i => i.RecordedBy!.Value).Distinct().ToList(), ct);
        return items.Select(i => i with
        {
            ReasonLabel = i.ReasonId.HasValue ? labels.GetValueOrDefault(i.ReasonId.Value) : null,
            RecordedByName = i.RecordedBy.HasValue ? users.GetValueOrDefault(i.RecordedBy.Value) : null,
        }).ToList();
    }

    public static RoomDto ToRoomDto(Domain.Modules.Scheduling.Entities.Room r, string? campusName = null)
        => new(r.Id, r.CorporationId, r.CampusId, r.RoomTypeId,
               r.Code, r.Name, r.Capacity, r.IsVirtual, r.MeetingUrl,
               r.IsActive, r.CreatedAt, r.UpdatedAt, r.RowVersion, campusName);

    public static CalendarEntryDto ToCalendarEntryDto(Domain.Modules.Scheduling.Entities.CalendarEntry e)
        => new(e.Id, e.CorporationId, e.CampusId, e.Title, e.EntryType,
               e.StartsAt, e.EndsAt, e.IsAllDay, e.CreatedAt);

    public static MakeupRequestDto ToMakeupDto(
        Domain.Modules.Scheduling.Entities.MakeupRequest m, string studentName)
        => new(m.Id, m.CorporationId, m.StudentId, studentName,
               m.MissedSessionId, m.MissedReasonId, m.Status,
               m.RequestedBy, m.RequestedAt, m.MakeupSessionId,
               m.CompletedAt, m.ExpiresOn, m.Note,
               m.UpdatedAt, m.RowVersion);
}
