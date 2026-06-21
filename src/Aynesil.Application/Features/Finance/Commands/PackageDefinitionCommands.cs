using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── CreatePackageDefinitionCommand ────────────────────────────────────────────

public record CreatePackageDefinitionCommand(
    Guid CorporationId,
    string Code,
    string Name,
    decimal ListPrice,
    Guid? PackageTypeId,
    Guid? ProgramId,
    decimal? TotalCredits,
    int? ValidityDays,
    string Currency = "TRY") : IRequest<PackageDefinitionDto>;

public class CreatePackageDefinitionCommandValidator
    : AbstractValidator<CreatePackageDefinitionCommand>
{
    public CreatePackageDefinitionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalCredits).GreaterThan(0).When(x => x.TotalCredits.HasValue);
        RuleFor(x => x.ValidityDays).GreaterThan(0).When(x => x.ValidityDays.HasValue);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class CreatePackageDefinitionCommandHandler
    : IRequestHandler<CreatePackageDefinitionCommand, PackageDefinitionDto>
{
    private readonly IAppDbContext _db;

    public CreatePackageDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PackageDefinitionDto> Handle(
        CreatePackageDefinitionCommand req, CancellationToken ct)
    {
        var duplicate = await _db.PackageDefinitions.AnyAsync(
            p => p.CorporationId == req.CorporationId && p.Code == req.Code, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"Package definition with code '{req.Code}' already exists for this corporation.");

        var definition = PackageDefinition.Create(
            req.CorporationId, req.Code, req.Name, req.ListPrice,
            req.PackageTypeId, req.ProgramId,
            req.TotalCredits, req.ValidityDays, req.Currency);

        _db.PackageDefinitions.Add(definition);
        await _db.SaveChangesAsync(ct);

        return new PackageDefinitionDto(
            definition.Id, definition.CorporationId,
            definition.Code, definition.Name,
            definition.PackageTypeId, definition.ProgramId,
            definition.TotalCredits, definition.ValidityDays,
            definition.ListPrice, definition.Currency, definition.IsActive,
            definition.CreatedAt, definition.UpdatedAt, definition.RowVersion);
    }
}

// ── UpdatePackageDefinitionCommand ────────────────────────────────────────────

public record UpdatePackageDefinitionCommand(
    Guid Id,
    string Code,
    string Name,
    decimal ListPrice,
    Guid? PackageTypeId,
    Guid? ProgramId,
    decimal? TotalCredits,
    int? ValidityDays,
    string Currency,
    int RowVersion) : IRequest<PackageDefinitionDto>;

public class UpdatePackageDefinitionCommandValidator
    : AbstractValidator<UpdatePackageDefinitionCommand>
{
    public UpdatePackageDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalCredits).GreaterThan(0).When(x => x.TotalCredits.HasValue);
        RuleFor(x => x.ValidityDays).GreaterThan(0).When(x => x.ValidityDays.HasValue);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class UpdatePackageDefinitionCommandHandler
    : IRequestHandler<UpdatePackageDefinitionCommand, PackageDefinitionDto>
{
    private readonly IAppDbContext _db;

    public UpdatePackageDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PackageDefinitionDto> Handle(
        UpdatePackageDefinitionCommand req, CancellationToken ct)
    {
        var definition = await _db.PackageDefinitions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Package definition {req.Id} not found.");

        if (definition.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Package definition was modified by another user. Please refresh and retry.");

        definition.UpdateDetails(
            req.Code, req.Name, req.ListPrice,
            req.PackageTypeId, req.ProgramId,
            req.TotalCredits, req.ValidityDays, req.Currency);

        await _db.SaveChangesAsync(ct);

        return new PackageDefinitionDto(
            definition.Id, definition.CorporationId,
            definition.Code, definition.Name,
            definition.PackageTypeId, definition.ProgramId,
            definition.TotalCredits, definition.ValidityDays,
            definition.ListPrice, definition.Currency, definition.IsActive,
            definition.CreatedAt, definition.UpdatedAt, definition.RowVersion);
    }
}

// ── ActivatePackageDefinitionCommand ─────────────────────────────────────────

public record ActivatePackageDefinitionCommand(Guid Id) : IRequest;

public sealed class ActivatePackageDefinitionCommandHandler
    : IRequestHandler<ActivatePackageDefinitionCommand>
{
    private readonly IAppDbContext _db;

    public ActivatePackageDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivatePackageDefinitionCommand req, CancellationToken ct)
    {
        var definition = await _db.PackageDefinitions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Package definition {req.Id} not found.");

        definition.Activate();
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeactivatePackageDefinitionCommand ────────────────────────────────────────

public record DeactivatePackageDefinitionCommand(Guid Id) : IRequest;

public sealed class DeactivatePackageDefinitionCommandHandler
    : IRequestHandler<DeactivatePackageDefinitionCommand>
{
    private readonly IAppDbContext _db;

    public DeactivatePackageDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeactivatePackageDefinitionCommand req, CancellationToken ct)
    {
        var definition = await _db.PackageDefinitions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Package definition {req.Id} not found.");

        definition.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeletePackageDefinitionCommand ────────────────────────────────────────────

public record DeletePackageDefinitionCommand(Guid Id) : IRequest;

public sealed class DeletePackageDefinitionCommandHandler
    : IRequestHandler<DeletePackageDefinitionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeletePackageDefinitionCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePackageDefinitionCommand req, CancellationToken ct)
    {
        var definition = await _db.PackageDefinitions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Package definition {req.Id} not found.");

        var hasActivePurchases = await _db.StudentPackages
            .AnyAsync(sp => sp.PackageDefinitionId == req.Id && sp.Status == "active", ct);

        if (hasActivePurchases)
            throw new InvalidOperationException(
                "Cannot delete a package definition with active student packages.");

        definition.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
