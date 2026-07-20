using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Cameras.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Cameras.Queries;

// ── GetCamerasQuery ───────────────────────────────────────────────────────────

/// <summary>Paginated camera list. Filterable by campus, type, active status, and free-text.</summary>
public class GetCamerasQuery : PagedQuery, IRequest<PaginatedResult<CameraListItemDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? CameraTypeId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetCamerasQueryHandler
    : IRequestHandler<GetCamerasQuery, PaginatedResult<CameraListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCamerasQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CameraListItemDto>> Handle(
        GetCamerasQuery req, CancellationToken ct)
    {
        var q = _db.Cameras.AsNoTracking()
            .Where(c => c.CorporationId == req.CorporationId);

        if (req.CampusId.HasValue)
            q = q.Where(c => c.CampusId == req.CampusId.Value);

        if (req.CameraTypeId.HasValue)
            q = q.Where(c => c.CameraTypeId == req.CameraTypeId.Value);

        if (req.IsActive.HasValue)
            q = q.Where(c => c.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(c => c.Code.Contains(req.Search) || c.Name.Contains(req.Search));

        var query =
            from c in q
            join campus in _db.Campuses.AsNoTracking()
                on c.CampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            join typ in _db.RefValues.AsNoTracking()
                on c.CameraTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new CameraListItemDto(
                c.Id,
                c.CorporationId,
                c.CampusId,
                campus != null ? campus.Name : null,
                c.CameraTypeId,
                typ != null ? typ.Code : null,
                c.Code,
                c.Name,
                c.StreamProviderId,
                c.IsActive,
                c.CreatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "code"      => req.IsDescending ? query.OrderByDescending(x => x.Code)      : query.OrderBy(x => x.Code),
            "name"      => req.IsDescending ? query.OrderByDescending(x => x.Name)      : query.OrderBy(x => x.Name),
            "createdat" => req.IsDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            "isactive"  => req.IsDescending ? query.OrderByDescending(x => x.IsActive)  : query.OrderBy(x => x.IsActive),
            _           => query.OrderBy(x => x.Code)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<CameraListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetCameraQuery ────────────────────────────────────────────────────────────

/// <summary>Full camera detail including room and session assignments.</summary>
public record GetCameraQuery(Guid Id) : IRequest<CameraDto>;

public sealed class GetCameraQueryHandler : IRequestHandler<GetCameraQuery, CameraDto>
{
    private readonly IAppDbContext _db;

    public GetCameraQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CameraDto> Handle(GetCameraQuery req, CancellationToken ct)
    {
        var camera = await _db.Cameras.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        var campusName = camera.CampusId.HasValue
            ? await _db.Campuses.AsNoTracking()
                .Where(c => c.Id == camera.CampusId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var cameraTypeCode = camera.CameraTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == camera.CameraTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        var roomAssignments = await (
            from rc in _db.RoomCameras.AsNoTracking()
            where rc.CameraId == req.Id
            join room in _db.Rooms.AsNoTracking() on rc.RoomId equals room.Id into roomGrp
            from room in roomGrp.DefaultIfEmpty()
            select new RoomCameraDto(
                rc.Id, rc.RoomId,
                room != null ? room.Code : null,
                room != null ? room.Name : null,
                rc.CameraId, camera.Code)
        ).ToListAsync(ct);

        var sessionAssignments = await (
            from sc in _db.SessionCameras.AsNoTracking()
            where sc.CameraId == req.Id
            join sess in _db.Sessions.AsNoTracking() on sc.SessionId equals sess.Id
            select new SessionCameraDto(
                sc.Id, sc.SessionId,
                sess.StartsAt, sess.EndsAt,
                sc.CameraId, camera.Code)
        ).ToListAsync(ct);

        return new CameraDto(
            camera.Id,
            camera.CorporationId,
            camera.CampusId,
            campusName,
            camera.CameraTypeId,
            cameraTypeCode,
            camera.Code,
            camera.Name,
            camera.StreamProviderId,
            camera.StreamRef,
            camera.IsActive,
            camera.CreatedAt,
            camera.UpdatedAt,
            camera.RowVersion,
            roomAssignments,
            sessionAssignments);
    }
}

// ── GetCameraAssignmentsQuery ─────────────────────────────────────────────────

/// <summary>All camera assignments for a given room or session.</summary>
public class GetCameraAssignmentsQuery : IRequest<CameraAssignmentsResultDto>
{
    public Guid? RoomId { get; set; }
    public Guid? SessionId { get; set; }
}

public record CameraAssignmentsResultDto(
    IReadOnlyList<RoomCameraDto> RoomAssignments,
    IReadOnlyList<SessionCameraDto> SessionAssignments);

public sealed class GetCameraAssignmentsQueryHandler
    : IRequestHandler<GetCameraAssignmentsQuery, CameraAssignmentsResultDto>
{
    private readonly IAppDbContext _db;

    public GetCameraAssignmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CameraAssignmentsResultDto> Handle(
        GetCameraAssignmentsQuery req, CancellationToken ct)
    {
        var roomAssignments = new List<RoomCameraDto>();
        var sessionAssignments = new List<SessionCameraDto>();

        if (req.RoomId.HasValue)
        {
            roomAssignments = await (
                from rc in _db.RoomCameras.AsNoTracking()
                where rc.RoomId == req.RoomId.Value
                join cam in _db.Cameras.AsNoTracking() on rc.CameraId equals cam.Id
                join room in _db.Rooms.AsNoTracking() on rc.RoomId equals room.Id
                select new RoomCameraDto(
                    rc.Id, rc.RoomId, room.Code, room.Name,
                    rc.CameraId, cam.Code)
            ).ToListAsync(ct);
        }

        if (req.SessionId.HasValue)
        {
            sessionAssignments = await (
                from sc in _db.SessionCameras.AsNoTracking()
                where sc.SessionId == req.SessionId.Value
                join cam in _db.Cameras.AsNoTracking() on sc.CameraId equals cam.Id
                join sess in _db.Sessions.AsNoTracking() on sc.SessionId equals sess.Id
                select new SessionCameraDto(
                    sc.Id, sc.SessionId,
                    sess.StartsAt, sess.EndsAt,
                    sc.CameraId, cam.Code)
            ).ToListAsync(ct);
        }

        return new CameraAssignmentsResultDto(roomAssignments, sessionAssignments);
    }
}

// ── GetViewingAuthorizationsQuery ─────────────────────────────────────────────

/// <summary>Paginated viewing authorizations. Filterable by guardian, student, session, active-only.</summary>
public class GetViewingAuthorizationsQuery : PagedQuery,
    IRequest<PaginatedResult<ViewingAuthorizationDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? GuardianId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? SessionId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public sealed class GetViewingAuthorizationsQueryHandler
    : IRequestHandler<GetViewingAuthorizationsQuery, PaginatedResult<ViewingAuthorizationDto>>
{
    private readonly IAppDbContext _db;

    public GetViewingAuthorizationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ViewingAuthorizationDto>> Handle(
        GetViewingAuthorizationsQuery req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var q = _db.ViewingAuthorizations.AsNoTracking()
            .Where(a => a.CorporationId == req.CorporationId);

        if (req.GuardianId.HasValue)
            q = q.Where(a => a.GuardianId == req.GuardianId.Value);

        if (req.StudentId.HasValue)
            q = q.Where(a => a.StudentId == req.StudentId.Value);

        if (req.SessionId.HasValue)
            q = q.Where(a => a.SessionId == req.SessionId.Value);

        if (req.ActiveOnly == true)
            q = q.Where(a => !a.IsRevoked && a.ValidFrom <= now && now < a.ValidTo);

        var query =
            from a in q
            join guardian in _db.Guardians.AsNoTracking()
                on a.GuardianId equals guardian.Id into gGrp
            from guardian in gGrp.DefaultIfEmpty()
            join student in _db.Students.AsNoTracking()
                on a.StudentId equals student.Id into sGrp
            from student in sGrp.DefaultIfEmpty()
            join typ in _db.RefValues.AsNoTracking()
                on a.AccessTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            orderby a.ValidFrom descending
            select new ViewingAuthorizationDto(
                a.Id,
                a.CorporationId,
                a.GuardianId,
                guardian != null ? guardian.FirstName + " " + guardian.LastName : null,
                a.StudentId,
                student != null ? student.FirstName + " " + student.LastName : null,
                a.SessionId,
                a.ConsentId,
                a.AccessTypeId,
                typ != null ? typ.Code : null,
                a.ValidFrom,
                a.ValidTo,
                a.GrantedBy,
                a.IsRevoked,
                !a.IsRevoked && a.ValidFrom <= now && now < a.ValidTo,
                a.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ViewingAuthorizationDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetViewingAuthorizationQuery ──────────────────────────────────────────────

public record GetViewingAuthorizationQuery(Guid Id) : IRequest<ViewingAuthorizationDto>;

public sealed class GetViewingAuthorizationQueryHandler
    : IRequestHandler<GetViewingAuthorizationQuery, ViewingAuthorizationDto>
{
    private readonly IAppDbContext _db;

    public GetViewingAuthorizationQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ViewingAuthorizationDto> Handle(
        GetViewingAuthorizationQuery req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var a = await _db.ViewingAuthorizations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"ViewingAuthorization {req.Id} not found.");

        var guardian = await _db.Guardians.AsNoTracking()
            .Where(g => g.Id == a.GuardianId)
            .Select(g => new { g.FirstName, g.LastName })
            .FirstOrDefaultAsync(ct);

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == a.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        var typeCode = a.AccessTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == a.AccessTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new ViewingAuthorizationDto(
            a.Id, a.CorporationId,
            a.GuardianId,
            guardian != null ? $"{guardian.FirstName} {guardian.LastName}" : null,
            a.StudentId,
            student != null ? $"{student.FirstName} {student.LastName}" : null,
            a.SessionId, a.ConsentId,
            a.AccessTypeId, typeCode,
            a.ValidFrom, a.ValidTo,
            a.GrantedBy, a.IsRevoked,
            !a.IsRevoked && a.ValidFrom <= now && now < a.ValidTo,
            a.CreatedAt);
    }
}

// ── GetViewingLogsQuery ───────────────────────────────────────────────────────

/// <summary>
/// Paginated viewing log query for audit/history. Filterable by guardian, session, camera, date range.
/// Access log is immutable — this is a read-only audit trail.
/// </summary>
public class GetViewingLogsQuery : PagedQuery, IRequest<PaginatedResult<ViewingLogDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? GuardianId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? CameraId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GetViewingLogsQueryHandler
    : IRequestHandler<GetViewingLogsQuery, PaginatedResult<ViewingLogDto>>
{
    private readonly IAppDbContext _db;

    public GetViewingLogsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ViewingLogDto>> Handle(
        GetViewingLogsQuery req, CancellationToken ct)
    {
        var q = _db.ViewingLogs.AsNoTracking()
            .Where(l => l.CorporationId == req.CorporationId);

        if (req.GuardianId.HasValue)
            q = q.Where(l => l.GuardianId == req.GuardianId.Value);

        if (req.SessionId.HasValue)
            q = q.Where(l => l.SessionId == req.SessionId.Value);

        if (req.CameraId.HasValue)
            q = q.Where(l => l.CameraId == req.CameraId.Value);

        if (req.From.HasValue)
            q = q.Where(l => l.StartedAt >= req.From.Value);

        if (req.To.HasValue)
            q = q.Where(l => l.StartedAt <= req.To.Value);

        var query =
            from l in q
            join guardian in _db.Guardians.AsNoTracking()
                on l.GuardianId equals guardian.Id into gGrp
            from guardian in gGrp.DefaultIfEmpty()
            join cam in _db.Cameras.AsNoTracking()
                on l.CameraId equals cam.Id into camGrp
            from cam in camGrp.DefaultIfEmpty()
            orderby l.StartedAt descending
            select new ViewingLogDto(
                l.Id,
                l.CorporationId,
                l.GuardianId,
                guardian != null ? guardian.FirstName + " " + guardian.LastName : null,
                l.UserId,
                l.SessionId,
                l.CameraId,
                cam != null ? cam.Code : null,
                l.AuthorizationId,
                l.StartedAt,
                l.EndedAt,
                l.EndedAt.HasValue
                    ? (int?)(int)(l.EndedAt.Value - l.StartedAt).TotalSeconds
                    : null,
                l.IpAddress != null ? l.IpAddress.ToString() : null);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ViewingLogDto>.Create(items, total, req.Page, req.PageSize);
    }
}
