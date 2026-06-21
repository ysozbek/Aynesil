using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Educators.Events;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Commands;

// ── AssignEducatorToCampusCommand ─────────────────────────────────────────────

public record AssignEducatorToCampusCommand(
    Guid EducatorId,
    Guid CampusId,
    bool IsPrimary,
    DateOnly? ActiveFrom) : IRequest<EducatorCampusDto>;

public class AssignEducatorToCampusCommandValidator : AbstractValidator<AssignEducatorToCampusCommand>
{
    public AssignEducatorToCampusCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.CampusId).NotEmpty();
    }
}

public sealed class AssignEducatorToCampusCommandHandler
    : IRequestHandler<AssignEducatorToCampusCommand, EducatorCampusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignEducatorToCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorCampusDto> Handle(AssignEducatorToCampusCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.EducatorId, ct)
            ?? throw new KeyNotFoundException($"Educator {req.EducatorId} not found.");

        var campusExists = await _db.Campuses.AnyAsync(c => c.Id == req.CampusId, ct);
        if (!campusExists)
            throw new KeyNotFoundException($"Campus {req.CampusId} not found.");

        var existing = await _db.EducatorCampuses.AnyAsync(
            c => c.EducatorId == req.EducatorId
              && c.CampusId == req.CampusId
              && c.ActiveTo == null, ct);
        if (existing)
            throw new InvalidOperationException(
                "Educator is already actively assigned to this campus.");

        var assignment = new EducatorCampus
        {
            CorporationId = educator.CorporationId,
            EducatorId    = req.EducatorId,
            CampusId      = req.CampusId,
            IsPrimary     = req.IsPrimary,
            ActiveFrom    = req.ActiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _db.EducatorCampuses.Add(assignment);

        educator.AddDomainEvent(new EducatorCampusAssignedEvent(
            req.EducatorId, educator.CorporationId, req.CampusId,
            req.IsPrimary, _currentUser.UserId));

        await _db.SaveChangesAsync(ct);

        var campusName = await _db.Campuses.AsNoTracking()
            .Where(c => c.Id == req.CampusId).Select(c => c.Name).FirstOrDefaultAsync(ct);

        return EducatorProjection.ToCampusDto(assignment, campusName);
    }
}

// ── EndEducatorCampusAssignmentCommand ────────────────────────────────────────

public record EndEducatorCampusAssignmentCommand(
    Guid CampusAssignmentId,
    DateOnly? EndDate) : IRequest<EducatorCampusDto>;

public sealed class EndEducatorCampusAssignmentCommandHandler
    : IRequestHandler<EndEducatorCampusAssignmentCommand, EducatorCampusDto>
{
    private readonly IAppDbContext _db;

    public EndEducatorCampusAssignmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorCampusDto> Handle(EndEducatorCampusAssignmentCommand req, CancellationToken ct)
    {
        var assignment = await _db.EducatorCampuses
            .FirstOrDefaultAsync(c => c.Id == req.CampusAssignmentId, ct)
            ?? throw new KeyNotFoundException(
                $"Campus assignment {req.CampusAssignmentId} not found.");

        assignment.ActiveTo = req.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        await _db.SaveChangesAsync(ct);

        var campusName = await _db.Campuses.AsNoTracking()
            .Where(c => c.Id == assignment.CampusId).Select(c => c.Name).FirstOrDefaultAsync(ct);

        return EducatorProjection.ToCampusDto(assignment, campusName);
    }
}
