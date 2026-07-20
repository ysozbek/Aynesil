using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.CareTeam.Commands;
using Aynesil.Application.Features.CareTeam.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.CareTeam.Queries;

// ── GetStudentCareTeamQuery ───────────────────────────────────────────────────

/// <summary>
/// Returns all care-team assignments for a given student.
/// Optionally filters to active-only assignments (default: true).
/// Requires care_team:read permission.
/// </summary>
public record GetStudentCareTeamQuery(
    Guid StudentId,
    bool ActiveOnly = true) : IRequest<IReadOnlyList<CareTeamAssignmentListItemDto>>;

public sealed class GetStudentCareTeamQueryHandler
    : IRequestHandler<GetStudentCareTeamQuery, IReadOnlyList<CareTeamAssignmentListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentCareTeamQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CareTeamAssignmentListItemDto>> Handle(
        GetStudentCareTeamQuery req, CancellationToken ct)
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var query =
            from a in _db.StudentCareAssignments.AsNoTracking()
            join edu in _db.Educators.AsNoTracking() on a.EducatorId equals edu.Id
            join role in _db.RefValues.AsNoTracking() on a.RoleId equals role.Id into roleGrp
            from role in roleGrp.DefaultIfEmpty()
            where a.StudentId == req.StudentId
            select new { a, edu, role };

        if (req.ActiveOnly)
            query = query.Where(x =>
                x.a.Status == "active" &&
                x.a.ActiveFrom <= now &&
                (x.a.ActiveTo == null || x.a.ActiveTo > now));

        var items = await query
            .OrderBy(x => x.a.IsPrimary ? 0 : 1)
            .ThenBy(x => x.a.ActiveFrom)
            .Select(x => new CareTeamAssignmentListItemDto(
                x.a.Id, x.a.StudentId, x.a.EducatorId,
                x.edu.FirstName + " " + x.edu.LastName,
                x.a.RoleId, x.role != null ? x.role.Code : null,
                x.a.IsPrimary, x.a.Status, x.a.ActiveFrom, x.a.ActiveTo,
                x.a.CreatedAt))
            .ToListAsync(ct);

        return items;
    }
}


// ── GetMyCareTeamStudentsQuery ────────────────────────────────────────────────

/// <summary>
/// Returns students currently assigned to the authenticated educator's care team.
/// Derives the educator from the current user's UserId → educator.user_id.
/// Returns empty when the user is not linked to any educator profile.
/// Requires care_team:read permission.
/// </summary>
public record GetMyCareTeamStudentsQuery : IRequest<IReadOnlyList<CareTeamStudentListItemDto>>;

public sealed class GetMyCareTeamStudentsQueryHandler
    : IRequestHandler<GetMyCareTeamStudentsQuery, IReadOnlyList<CareTeamStudentListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCareTeamStudentsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CareTeamStudentListItemDto>> Handle(
        GetMyCareTeamStudentsQuery req, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            return [];

        var now        = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId     = _currentUser.UserId.Value;

        // Resolve educator(s) linked to this user account
        var educatorIds = await _db.Educators
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (educatorIds.Count == 0)
            return [];

        var items = await (
            from a in _db.StudentCareAssignments.AsNoTracking()
            join s in _db.Students.AsNoTracking() on a.StudentId equals s.Id
            join role in _db.RefValues.AsNoTracking() on a.RoleId equals role.Id into roleGrp
            from role in roleGrp.DefaultIfEmpty()
            where educatorIds.Contains(a.EducatorId)
               && a.Status == "active"
               && a.ActiveFrom <= now
               && (a.ActiveTo == null || a.ActiveTo > now)
            orderby a.IsPrimary descending, s.LastName, s.FirstName
            select new CareTeamStudentListItemDto(
                s.Id,
                s.FirstName + " " + s.LastName,
                a.Id,
                a.RoleId,
                role != null ? role.Code : null,
                a.IsPrimary,
                a.Status,
                a.ActiveFrom,
                a.ActiveTo)
        ).ToListAsync(ct);

        return items;
    }
}


// ── GetCareTeamAssignmentQuery ────────────────────────────────────────────────

/// <summary>Returns a single assignment by ID. Requires care_team:read.</summary>
public record GetCareTeamAssignmentQuery(Guid Id) : IRequest<CareTeamAssignmentDto>;

public sealed class GetCareTeamAssignmentQueryHandler
    : IRequestHandler<GetCareTeamAssignmentQuery, CareTeamAssignmentDto>
{
    private readonly IAppDbContext _db;

    public GetCareTeamAssignmentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CareTeamAssignmentDto> Handle(
        GetCareTeamAssignmentQuery req, CancellationToken ct)
        => await AssignCareTeamMemberCommandHandler
                .LoadAssignmentDtoAsync(_db, req.Id, ct)
            ?? throw new KeyNotFoundException($"CareTeamAssignment {req.Id} not found.");
}
