using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Leads.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record UpdateLeadCommand(
    Guid Id,
    string ContactName,
    Guid? CampusId,
    Guid? SourceId,
    string? ChildName,
    DateOnly? ChildBirthDate,
    string? ContactPhone,
    string? ContactEmail,
    string? PresentingNeed,
    string? ReferralDetail,
    Guid? AssignedToId,
    int? Score,
    int RowVersion) : IRequest<LeadDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class UpdateLeadCommandValidator : AbstractValidator<UpdateLeadCommand>
{
    public UpdateLeadCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPhone).MaximumLength(30).When(x => x.ContactPhone is not null);
        RuleFor(x => x.ContactEmail).MaximumLength(254).EmailAddress().When(x => x.ContactEmail is not null);
        RuleFor(x => x.ChildName).MaximumLength(200).When(x => x.ChildName is not null);
        RuleFor(x => x.PresentingNeed).MaximumLength(2000).When(x => x.PresentingNeed is not null);
        RuleFor(x => x.ReferralDetail).MaximumLength(500).When(x => x.ReferralDetail is not null);
        RuleFor(x => x.Score).InclusiveBetween(0, 100).When(x => x.Score.HasValue);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class UpdateLeadCommandHandler : IRequestHandler<UpdateLeadCommand, LeadDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateLeadCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LeadDto> Handle(UpdateLeadCommand req, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == req.Id, ct)
            ?? throw new NotFoundException("Lead", req.Id);

        if (lead.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The lead was modified by another user. Please refresh and retry.")]);

        lead.Update(
            req.ContactName,
            req.CampusId,
            req.SourceId,
            req.ChildName,
            req.ChildBirthDate,
            req.ContactPhone,
            req.ContactEmail,
            req.PresentingNeed,
            req.ReferralDetail,
            req.AssignedToId,
            req.Score,
            _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await LeadProjection.LoadAsync(_db, lead.Id, ct))!;
    }
}
