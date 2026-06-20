using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Domain.Modules.Crm.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record ChangeLeadStatusCommand(
    Guid LeadId,
    Guid NewStatusId,
    Guid? NewPipelineStageId,
    int RowVersion) : IRequest<LeadDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class ChangeLeadStatusCommandValidator : AbstractValidator<ChangeLeadStatusCommand>
{
    public ChangeLeadStatusCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.NewStatusId).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class ChangeLeadStatusCommandHandler : IRequestHandler<ChangeLeadStatusCommand, LeadDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ChangeLeadStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LeadDto> Handle(ChangeLeadStatusCommand req, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == req.LeadId, ct)
            ?? throw new NotFoundException("Lead", req.LeadId);

        if (lead.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The lead was modified by another user. Please refresh and retry.")]);

        lead.ChangeStatus(req.NewStatusId, req.NewPipelineStageId, _currentUser.UserId);

        _db.LeadStatusHistories.Add(LeadStatusHistory.Record(
            lead.CorporationId, lead.Id,
            req.NewStatusId, req.NewPipelineStageId,
            _currentUser.UserId));

        await _db.SaveChangesAsync(ct);
        return (await LeadProjection.LoadAsync(_db, lead.Id, ct))!;
    }
}
