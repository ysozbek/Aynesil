using Aynesil.Application.Common.CareTeam;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Queries;

// ── Request ───────────────────────────────────────────────────────────────────

public class GetAssessmentSessionsQuery
    : PagedQuery, IRequest<PaginatedResult<AssessmentSessionListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Status { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? AssessorId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetAssessmentSessionsQueryHandler
    : IRequestHandler<GetAssessmentSessionsQuery, PaginatedResult<AssessmentSessionListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAssessmentSessionsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<AssessmentSessionListItemDto>> Handle(
        GetAssessmentSessionsQuery req, CancellationToken ct)
    {
        // Care-team pre-filter: when a specific enrolled student is requested,
        // verify the user can access that student's clinical data.
        // Lead-stage sessions (StudentId = null) are RBAC-only — no care-team check.
        if (req.StudentId.HasValue &&
            !CareTeamFilter.HasBypass(_currentUser) &&
            !await CareTeamFilter.CanAccessStudentAsync(_db, _currentUser, req.StudentId.Value, ct))
            return PaginatedResult<AssessmentSessionListItemDto>.Create([], 0, req.Page, req.PageSize);

        var query = AssessmentProjection.BuildSessionListQuery(_db);

        if (req.CorporationId.HasValue)
            query = query.Where(s => s.CorporationId == req.CorporationId);

        if (req.CampusId.HasValue)
            query = query.Where(s => s.CampusId == req.CampusId);

        if (req.TemplateId.HasValue)
            query = query.Where(s => s.TemplateId == req.TemplateId);

        if (!string.IsNullOrWhiteSpace(req.Status))
            query = query.Where(s => s.Status == req.Status);

        if (req.LeadId.HasValue)
            query = query.Where(s => s.LeadId == req.LeadId);

        if (req.StudentId.HasValue)
            query = query.Where(s => s.StudentId == req.StudentId);

        if (req.AssessorId.HasValue)
            query = query.Where(s => s.AssessorId == req.AssessorId);

        if (req.From.HasValue)
            query = query.Where(s => s.ScheduledAt >= req.From);

        if (req.To.HasValue)
            query = query.Where(s => s.ScheduledAt <= req.To);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "scheduledat" => req.IsDescending
                ? query.OrderByDescending(s => s.ScheduledAt)
                : query.OrderBy(s => s.ScheduledAt),
            "status" => req.IsDescending
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<AssessmentSessionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
