using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Queries;

// ── GetMyStudentsQuery (Parent Portal) ────────────────────────────────────────

/// <summary>
/// Returns the list of students the currently authenticated guardian may access
/// via the parent portal. Filters by the guardian's user_id in iam.user_account.
/// </summary>
public record GetMyStudentsQuery(Guid GuardianUserId) : IRequest<IReadOnlyList<PortalStudentDto>>;

public sealed class GetMyStudentsQueryHandler
    : IRequestHandler<GetMyStudentsQuery, IReadOnlyList<PortalStudentDto>>
{
    private readonly IAppDbContext _db;

    public GetMyStudentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortalStudentDto>> Handle(
        GetMyStudentsQuery req, CancellationToken ct)
    {
        return await (
            from access in _db.GuardianPortalAccesses.AsNoTracking()
            join guardian in _db.Guardians.AsNoTracking()
                on access.GuardianId equals guardian.Id
            join student in _db.Students.AsNoTracking()
                on access.StudentId equals student.Id
            join campus in _db.Campuses.AsNoTracking()
                on student.PrimaryCampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            where guardian.UserId == req.GuardianUserId
               && access.RevokedAt == null
            select new PortalStudentDto(
                student.Id, student.FirstName, student.LastName,
                student.FirstName + " " + student.LastName,
                student.BirthDate, student.PhotoFileId,
                campus != null ? campus.Name : null,
                access.CanViewSessions, access.CanViewAttendance,
                access.CanViewReports, access.CanViewPlan,
                access.CanViewFinance, access.CanViewCamera)
        ).ToListAsync(ct);
    }
}

// ── GetPortalStudentSummaryQuery ──────────────────────────────────────────────

/// <summary>
/// Returns a student's summary for the parent portal.
/// Verifies the requesting guardian has active portal access to this student.
/// </summary>
public record GetPortalStudentSummaryQuery(
    Guid StudentId,
    Guid GuardianUserId) : IRequest<PortalStudentDto>;

public sealed class GetPortalStudentSummaryQueryHandler
    : IRequestHandler<GetPortalStudentSummaryQuery, PortalStudentDto>
{
    private readonly IAppDbContext _db;

    public GetPortalStudentSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PortalStudentDto> Handle(GetPortalStudentSummaryQuery req, CancellationToken ct)
    {
        var dto = await (
            from access in _db.GuardianPortalAccesses.AsNoTracking()
            join guardian in _db.Guardians.AsNoTracking()
                on access.GuardianId equals guardian.Id
            join student in _db.Students.AsNoTracking()
                on access.StudentId equals student.Id
            join campus in _db.Campuses.AsNoTracking()
                on student.PrimaryCampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            where guardian.UserId == req.GuardianUserId
               && student.Id == req.StudentId
               && access.RevokedAt == null
            select new PortalStudentDto(
                student.Id, student.FirstName, student.LastName,
                student.FirstName + " " + student.LastName,
                student.BirthDate, student.PhotoFileId,
                campus != null ? campus.Name : null,
                access.CanViewSessions, access.CanViewAttendance,
                access.CanViewReports, access.CanViewPlan,
                access.CanViewFinance, access.CanViewCamera)
        ).FirstOrDefaultAsync(ct)
         ?? throw new UnauthorizedAccessException(
             $"Guardian does not have active portal access to student {req.StudentId}.");

        return dto;
    }
}
