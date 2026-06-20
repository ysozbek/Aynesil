using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using MediatR;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
public record GetLeadQuery(Guid Id) : IRequest<LeadDto>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetLeadQueryHandler : IRequestHandler<GetLeadQuery, LeadDto>
{
    private readonly IAppDbContext _db;

    public GetLeadQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LeadDto> Handle(GetLeadQuery req, CancellationToken ct)
        => await LeadProjection.LoadAsync(_db, req.Id, ct)
            ?? throw new NotFoundException("Lead", req.Id);
}
