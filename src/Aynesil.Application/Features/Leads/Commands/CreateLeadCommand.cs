using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Domain.Modules.Crm.Entities;
using FluentValidation;
using MediatR;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record CreateLeadCommand(
    Guid CorporationId,
    string ContactName,
    Guid? CampusId,
    Guid? SourceId,
    Guid? StatusId,
    Guid? PipelineStageId,
    string? ChildName,
    DateOnly? ChildBirthDate,
    string? ContactPhone,
    string? ContactEmail,
    string? PresentingNeed,
    string? ReferralDetail,
    Guid? AssignedToId,
    int? Score) : IRequest<LeadDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPhone).MaximumLength(30).When(x => x.ContactPhone is not null);
        RuleFor(x => x.ContactEmail).MaximumLength(254).EmailAddress().When(x => x.ContactEmail is not null);
        RuleFor(x => x.ChildName).MaximumLength(200).When(x => x.ChildName is not null);
        RuleFor(x => x.PresentingNeed).MaximumLength(2000).When(x => x.PresentingNeed is not null);
        RuleFor(x => x.ReferralDetail).MaximumLength(500).When(x => x.ReferralDetail is not null);
        RuleFor(x => x.Score).InclusiveBetween(0, 100).When(x => x.Score.HasValue);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class CreateLeadCommandHandler : IRequestHandler<CreateLeadCommand, LeadDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateLeadCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LeadDto> Handle(CreateLeadCommand req, CancellationToken ct)
    {
        var lead = Lead.Create(
            req.CorporationId,
            req.ContactName,
            req.CampusId,
            req.SourceId,
            req.StatusId,
            req.PipelineStageId,
            req.ChildName,
            req.ChildBirthDate,
            req.ContactPhone,
            req.ContactEmail,
            req.PresentingNeed,
            req.ReferralDetail,
            req.AssignedToId,
            req.Score,
            _currentUser.UserId);

        _db.Leads.Add(lead);

        if (req.StatusId.HasValue || req.PipelineStageId.HasValue)
        {
            _db.LeadStatusHistories.Add(LeadStatusHistory.Record(
                lead.CorporationId, lead.Id,
                req.StatusId, req.PipelineStageId,
                _currentUser.UserId));
        }

        await _db.SaveChangesAsync(ct);
        return (await LeadProjection.LoadAsync(_db, lead.Id, ct))!;
    }
}
