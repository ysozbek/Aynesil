using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── CreateCampCommand ─────────────────────────────────────────────────────────

public record CreateCampCommand(
    Guid CorporationId,
    string Code,
    string Name,
    Guid? CampTypeId,
    Guid? CampusId,
    string? Description,
    string? Location,
    int? Capacity,
    Guid? CreatedBy = null) : IRequest<CampDto>;

public class CreateCampCommandValidator : AbstractValidator<CreateCampCommand>
{
    public CreateCampCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateCampCommandHandler : IRequestHandler<CreateCampCommand, CampDto>
{
    private readonly IAppDbContext _db;

    public CreateCampCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampDto> Handle(CreateCampCommand req, CancellationToken ct)
    {
        var duplicate = await _db.Camps.AnyAsync(
            c => c.CorporationId == req.CorporationId
              && c.Code == req.Code.Trim().ToLowerInvariant()
              && c.DeletedAt == null, ct);

        if (duplicate)
            throw new InvalidOperationException($"A camp with code '{req.Code}' already exists.");

        var camp = Camp.Create(
            req.CorporationId, req.Code, req.Name,
            req.CampTypeId, req.CampusId,
            req.Description, req.Location, req.Capacity, req.CreatedBy);

        _db.Camps.Add(camp);
        await _db.SaveChangesAsync(ct);

        var typeCode = req.CampTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.CampTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new CampDto(
            camp.Id, camp.CorporationId, camp.CampusId,
            camp.CampTypeId, typeCode,
            camp.Code, camp.Name, camp.Description, camp.Location,
            camp.Capacity, camp.IsActive,
            camp.CreatedAt, camp.UpdatedAt, camp.RowVersion, []);
    }
}

// ── UpdateCampCommand ─────────────────────────────────────────────────────────

public record UpdateCampCommand(
    Guid Id,
    string Name,
    Guid? CampTypeId,
    Guid? CampusId,
    string? Description,
    string? Location,
    int? Capacity,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateCampCommandValidator : AbstractValidator<UpdateCampCommand>
{
    public UpdateCampCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateCampCommandHandler : IRequestHandler<UpdateCampCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCampCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCampCommand req, CancellationToken ct)
    {
        var camp = await _db.Camps
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp {req.Id} not found.");

        camp.Update(req.Name, req.CampTypeId, req.CampusId,
            req.Description, req.Location, req.Capacity, req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── ActivateCampCommand ───────────────────────────────────────────────────────

public record ActivateCampCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class ActivateCampCommandHandler : IRequestHandler<ActivateCampCommand>
{
    private readonly IAppDbContext _db;

    public ActivateCampCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivateCampCommand req, CancellationToken ct)
    {
        var camp = await _db.Camps
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp {req.Id} not found.");

        camp.Activate(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeactivateCampCommand ─────────────────────────────────────────────────────

public record DeactivateCampCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class DeactivateCampCommandHandler : IRequestHandler<DeactivateCampCommand>
{
    private readonly IAppDbContext _db;

    public DeactivateCampCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeactivateCampCommand req, CancellationToken ct)
    {
        var camp = await _db.Camps
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp {req.Id} not found.");

        camp.Deactivate(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCampCommand ─────────────────────────────────────────────────────────

public record DeleteCampCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteCampCommandHandler : IRequestHandler<DeleteCampCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCampCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCampCommand req, CancellationToken ct)
    {
        var camp = await _db.Camps
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp {req.Id} not found.");

        var hasActiveEnrollments = await (
            from e in _db.CampEnrollments
            join p in _db.CampPeriods on e.CampPeriodId equals p.Id
            where p.CampId == req.Id && e.Status == "enrolled"
            select e.Id
        ).AnyAsync(ct);

        if (hasActiveEnrollments)
            throw new InvalidOperationException(
                "Cannot delete a camp with active enrollments.");

        camp.SoftDelete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
