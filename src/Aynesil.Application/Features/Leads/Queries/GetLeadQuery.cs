using Aynesil.Domain.Common;
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

    private readonly ITenantContext _tenant;

    public GetLeadQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<LeadDto> Handle(GetLeadQuery req, CancellationToken ct)
        => await LeadProjection.LoadAsync(_db, req.Id, _tenant.Locale, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Lead"));
}
