using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
public record GetLeadInterviewsQuery(Guid LeadId) : IRequest<IReadOnlyList<InterviewDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetLeadInterviewsQueryHandler
    : IRequestHandler<GetLeadInterviewsQuery, IReadOnlyList<InterviewDto>>
{
    private readonly IAppDbContext _db;

    public GetLeadInterviewsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InterviewDto>> Handle(
        GetLeadInterviewsQuery req, CancellationToken ct)
    {
        var rows = await (
            from i   in _db.Interviews
            join camp in _db.Campuses     on i.CampusId    equals camp.Id into campG from camp in campG.DefaultIfEmpty()
            join usr  in _db.UserAccounts on i.ConductedBy equals usr.Id  into usrG  from usr  in usrG.DefaultIfEmpty()
            where i.LeadId == req.LeadId
            orderby i.ScheduledAt descending
            select new InterviewDto(
                i.Id, i.LeadId,
                i.CampusId, camp == null ? null : camp.Name,
                i.ScheduledAt, i.ConductedAt,
                i.ConductedBy, usr == null ? null : usr.FullName,
                i.Outcome, i.Recommendation, i.Status,
                i.CreatedAt, i.UpdatedAt, i.RowVersion)
        ).ToListAsync(ct);

        return rows;
    }
}
