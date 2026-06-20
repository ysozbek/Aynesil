using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── ReplaceEmergencyContactsCommand ──────────────────────────────────────────

/// <summary>
/// Replaces ALL emergency contacts for a student in a single operation.
/// The existing contacts are removed and the new set is inserted.
/// </summary>
public record ReplaceEmergencyContactsCommand(
    Guid StudentId,
    IReadOnlyList<EmergencyContactInput> Contacts) : IRequest<IReadOnlyList<EmergencyContactDto>>;

public record EmergencyContactInput(
    string FullName,
    string? Relationship,
    string Phone,
    int Priority);

public class ReplaceEmergencyContactsCommandValidator
    : AbstractValidator<ReplaceEmergencyContactsCommand>
{
    public ReplaceEmergencyContactsCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleForEach(x => x.Contacts).ChildRules(c =>
        {
            c.RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            c.RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
            c.RuleFor(x => x.Priority).GreaterThan(0);
        });
    }
}

public sealed class ReplaceEmergencyContactsCommandHandler
    : IRequestHandler<ReplaceEmergencyContactsCommand, IReadOnlyList<EmergencyContactDto>>
{
    private readonly IAppDbContext _db;

    public ReplaceEmergencyContactsCommandHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmergencyContactDto>> Handle(
        ReplaceEmergencyContactsCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var existing = await _db.EmergencyContacts
            .Where(ec => ec.StudentId == req.StudentId)
            .ToListAsync(ct);

        _db.EmergencyContacts.RemoveRange(existing);

        var contacts = req.Contacts.Select(c => new EmergencyContact
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            FullName      = c.FullName,
            Relationship  = c.Relationship,
            Phone         = c.Phone,
            Priority      = c.Priority
        }).ToList();

        _db.EmergencyContacts.AddRange(contacts);
        await _db.SaveChangesAsync(ct);

        return contacts.Select(StudentProjection.ToEmergencyContactDto).ToList();
    }
}
