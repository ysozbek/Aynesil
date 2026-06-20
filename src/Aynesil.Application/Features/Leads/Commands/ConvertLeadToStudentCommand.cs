using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Leads.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
/// <summary>
/// Links a lead to a student record that was created by the Students module.
/// The Students module is responsible for creating the student entity first;
/// this command records the conversion on the lead side and fires LeadConvertedEvent.
/// </summary>
public record ConvertLeadToStudentCommand(
    Guid LeadId,
    Guid StudentId,
    int RowVersion) : IRequest<LeadDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class ConvertLeadToStudentCommandValidator : AbstractValidator<ConvertLeadToStudentCommand>
{
    public ConvertLeadToStudentCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class ConvertLeadToStudentCommandHandler : IRequestHandler<ConvertLeadToStudentCommand, LeadDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConvertLeadToStudentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LeadDto> Handle(ConvertLeadToStudentCommand req, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == req.LeadId, ct)
            ?? throw new NotFoundException("Lead", req.LeadId);

        if (lead.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The lead was modified by another user. Please refresh and retry.")]);

        lead.ConvertToStudent(req.StudentId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await LeadProjection.LoadAsync(_db, lead.Id, ct))!;
    }
}
