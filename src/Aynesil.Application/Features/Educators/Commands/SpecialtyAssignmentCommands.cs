using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Educators.Events;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Commands;

// ── AssignSpecialtyCommand ────────────────────────────────────────────────────

public record AssignSpecialtyCommand(Guid EducatorId, Guid SpecialtyId) : IRequest<EducatorSpecialtyDto>;

public class AssignSpecialtyCommandValidator : AbstractValidator<AssignSpecialtyCommand>
{
    public AssignSpecialtyCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.SpecialtyId).NotEmpty();
    }
}

public sealed class AssignSpecialtyCommandHandler : IRequestHandler<AssignSpecialtyCommand, EducatorSpecialtyDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignSpecialtyCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorSpecialtyDto> Handle(AssignSpecialtyCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.EducatorId, ct)
            ?? throw new KeyNotFoundException($"Educator {req.EducatorId} not found.");

        var validSpecialty = await _db.RefValues.AnyAsync(
            r => r.Id == req.SpecialtyId && r.RefType!.Code == "specialty", ct);
        if (!validSpecialty)
            throw new KeyNotFoundException($"Invalid specialty ref_value: {req.SpecialtyId}");

        var alreadyAssigned = await _db.EducatorSpecialties.AnyAsync(
            s => s.EducatorId == req.EducatorId && s.SpecialtyId == req.SpecialtyId, ct);
        if (alreadyAssigned)
            throw new InvalidOperationException("This specialty is already assigned to the educator.");

        var specialty = new EducatorSpecialty
        {
            CorporationId = educator.CorporationId,
            EducatorId    = req.EducatorId,
            SpecialtyId   = req.SpecialtyId
        };

        _db.EducatorSpecialties.Add(specialty);

        educator.AddDomainEvent(new EducatorSpecialtyAssignedEvent(
            req.EducatorId, educator.CorporationId, req.SpecialtyId,
            specialty.Id, _currentUser.UserId));

        await _db.SaveChangesAsync(ct);

        var label = await _db.RefValues.AsNoTracking()
            .Where(r => r.Id == req.SpecialtyId)
            .Select(r => r.Code).FirstOrDefaultAsync(ct);

        return new EducatorSpecialtyDto(specialty.Id, specialty.SpecialtyId, label);
    }
}

// ── RemoveSpecialtyCommand ────────────────────────────────────────────────────

public record RemoveSpecialtyCommand(Guid SpecialtyAssignmentId) : IRequest;

public sealed class RemoveSpecialtyCommandHandler : IRequestHandler<RemoveSpecialtyCommand>
{
    private readonly IAppDbContext _db;

    public RemoveSpecialtyCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveSpecialtyCommand req, CancellationToken ct)
    {
        var specialty = await _db.EducatorSpecialties
            .FirstOrDefaultAsync(s => s.Id == req.SpecialtyAssignmentId, ct)
            ?? throw new KeyNotFoundException(
                $"Specialty assignment {req.SpecialtyAssignmentId} not found.");

        _db.EducatorSpecialties.Remove(specialty);
        await _db.SaveChangesAsync(ct);
    }
}
