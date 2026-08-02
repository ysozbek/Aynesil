using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── AssignCampEducatorCommand ─────────────────────────────────────────────────

public record AssignCampEducatorCommand(
    Guid CorporationId,
    Guid CampId,
    Guid EducatorId,
    string Role = "lead",
    Guid? CampPeriodId = null,
    Guid? CampActivityId = null,
    Guid? AssignedBy = null) : IRequest<CampEducatorDto>;

public class AssignCampEducatorCommandValidator : AbstractValidator<AssignCampEducatorCommand>
{
    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    public AssignCampEducatorCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampId).NotEmpty();
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be: lead, assistant, observer, or supervisor.");
    }
}

public sealed class AssignCampEducatorCommandHandler
    : IRequestHandler<AssignCampEducatorCommand, CampEducatorDto>
{
    private readonly IAppDbContext _db;

    public AssignCampEducatorCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampEducatorDto> Handle(AssignCampEducatorCommand req, CancellationToken ct)
    {
        var campExists = await _db.Camps
            .AnyAsync(c => c.Id == req.CampId
                        && c.CorporationId == req.CorporationId
                        && c.DeletedAt == null, ct);
        if (!campExists)
            throw new KeyNotFoundException($"Camp {req.CampId} not found.");

        if (req.CampPeriodId.HasValue)
        {
            var periodOk = await _db.CampPeriods
                .AnyAsync(p => p.Id == req.CampPeriodId.Value && p.CampId == req.CampId, ct);
            if (!periodOk)
                throw new KeyNotFoundException($"Camp period {req.CampPeriodId} not found for this camp.");
        }

        if (req.CampActivityId.HasValue)
        {
            var activityOk = await _db.CampActivities
                .AnyAsync(a => a.Id == req.CampActivityId.Value && a.DeletedAt == null, ct);
            if (!activityOk)
                throw new KeyNotFoundException($"Camp activity {req.CampActivityId} not found.");
        }

        var duplicate = await _db.CampEducators.AnyAsync(e =>
            e.CampId == req.CampId
            && e.EducatorId == req.EducatorId
            && e.CampPeriodId == req.CampPeriodId
            && e.CampActivityId == req.CampActivityId, ct);
        if (duplicate)
            throw new InvalidOperationException("Educator is already assigned at this scope.");

        var assignment = CampEducator.Assign(
            req.CorporationId, req.CampId, req.EducatorId, req.Role,
            req.CampPeriodId, req.CampActivityId, req.AssignedBy);

        _db.CampEducators.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return new CampEducatorDto(
            assignment.Id, assignment.CorporationId, assignment.CampId,
            assignment.CampPeriodId, assignment.CampActivityId,
            assignment.EducatorId, assignment.Role,
            assignment.AssignedAt, assignment.AssignedBy);
    }
}

// ── UpdateCampEducatorRoleCommand ─────────────────────────────────────────────

public record UpdateCampEducatorRoleCommand(Guid Id, string Role) : IRequest;

public class UpdateCampEducatorRoleCommandValidator : AbstractValidator<UpdateCampEducatorRoleCommand>
{
    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    public UpdateCampEducatorRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be: lead, assistant, observer, or supervisor.");
    }
}

public sealed class UpdateCampEducatorRoleCommandHandler
    : IRequestHandler<UpdateCampEducatorRoleCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCampEducatorRoleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCampEducatorRoleCommand req, CancellationToken ct)
    {
        var assignment = await _db.CampEducators
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camp educator assignment {req.Id} not found.");

        assignment.UpdateRole(req.Role);
        await _db.SaveChangesAsync(ct);
    }
}

// ── RemoveCampEducatorCommand ─────────────────────────────────────────────────

public record RemoveCampEducatorCommand(Guid Id) : IRequest;

public sealed class RemoveCampEducatorCommandHandler : IRequestHandler<RemoveCampEducatorCommand>
{
    private readonly IAppDbContext _db;

    public RemoveCampEducatorCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveCampEducatorCommand req, CancellationToken ct)
    {
        var assignment = await _db.CampEducators
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camp educator assignment {req.Id} not found.");

        _db.CampEducators.Remove(assignment);
        await _db.SaveChangesAsync(ct);
    }
}
