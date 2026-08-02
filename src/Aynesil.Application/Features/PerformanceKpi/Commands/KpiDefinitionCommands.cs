using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using Aynesil.Domain.Modules.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Commands;

// ── CreateKpiDefinitionCommand ────────────────────────────────────────────────

public record CreateKpiDefinitionCommand(
    Guid CorporationId,
    string Code,
    string Name,
    Guid? CategoryId,
    string? Unit,
    string? Spec,
    Guid? CreatedBy = null) : IRequest<KpiDefinitionDto>;

public class CreateKpiDefinitionCommandValidator : AbstractValidator<CreateKpiDefinitionCommand>
{
    public CreateKpiDefinitionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z][a-z0-9_.]*$")
            .WithMessage("Code must be lowercase alphanumeric with dots or underscores.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).MaximumLength(20).When(x => x.Unit != null);
    }
}

public sealed class CreateKpiDefinitionCommandHandler
    : IRequestHandler<CreateKpiDefinitionCommand, KpiDefinitionDto>
{
    private readonly IAppDbContext _db;

    public CreateKpiDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<KpiDefinitionDto> Handle(
        CreateKpiDefinitionCommand req, CancellationToken ct)
    {
        var code = req.Code.Trim().ToLowerInvariant();

        var duplicate = await _db.KpiDefinitions.AnyAsync(
            k => (k.CorporationId == req.CorporationId || k.CorporationId == null)
              && k.Code == code, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"A KPI definition with code '{code}' already exists.");

        var kpi = new KpiDefinition
        {
            CorporationId = req.CorporationId,
            Code          = code,
            Name          = req.Name.Trim(),
            CategoryId    = req.CategoryId,
            Unit          = req.Unit?.Trim(),
            Spec          = string.IsNullOrWhiteSpace(req.Spec) ? "{}" : req.Spec,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            RowVersion    = 1
        };

        _db.KpiDefinitions.Add(kpi);
        await _db.SaveChangesAsync(ct);

        var categoryCode = kpi.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == kpi.CategoryId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return kpi.ToDto(categoryCode);
    }
}

// ── UpdateKpiDefinitionCommand ────────────────────────────────────────────────

public record UpdateKpiDefinitionCommand(
    Guid Id,
    string Name,
    Guid? CategoryId,
    string? Unit,
    string? Spec,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest<KpiDefinitionDto>;

public class UpdateKpiDefinitionCommandValidator : AbstractValidator<UpdateKpiDefinitionCommand>
{
    public UpdateKpiDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).MaximumLength(20).When(x => x.Unit != null);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateKpiDefinitionCommandHandler
    : IRequestHandler<UpdateKpiDefinitionCommand, KpiDefinitionDto>
{
    private readonly IAppDbContext _db;

    public UpdateKpiDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<KpiDefinitionDto> Handle(
        UpdateKpiDefinitionCommand req, CancellationToken ct)
    {
        var kpi = await _db.KpiDefinitions
            .FirstOrDefaultAsync(k => k.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"KPI definition {req.Id} not found.");

        kpi.Name      = req.Name.Trim();
        kpi.CategoryId = req.CategoryId;
        kpi.Unit      = req.Unit?.Trim();
        kpi.Spec      = string.IsNullOrWhiteSpace(req.Spec) ? "{}" : req.Spec;
        kpi.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        var categoryCode = kpi.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == kpi.CategoryId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return kpi.ToDto(categoryCode);
    }
}

// ── ActivateKpiDefinitionCommand ──────────────────────────────────────────────

public record ActivateKpiDefinitionCommand(Guid Id) : IRequest;

public sealed class ActivateKpiDefinitionCommandHandler
    : IRequestHandler<ActivateKpiDefinitionCommand>
{
    private readonly IAppDbContext _db;

    public ActivateKpiDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivateKpiDefinitionCommand req, CancellationToken ct)
    {
        var kpi = await _db.KpiDefinitions
            .FirstOrDefaultAsync(k => k.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"KPI definition {req.Id} not found.");

        if (kpi.IsActive)
            throw new InvalidOperationException("KPI definition is already active.");

        kpi.IsActive  = true;
        kpi.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeactivateKpiDefinitionCommand ────────────────────────────────────────────

public record DeactivateKpiDefinitionCommand(Guid Id) : IRequest;

public sealed class DeactivateKpiDefinitionCommandHandler
    : IRequestHandler<DeactivateKpiDefinitionCommand>
{
    private readonly IAppDbContext _db;

    public DeactivateKpiDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeactivateKpiDefinitionCommand req, CancellationToken ct)
    {
        var kpi = await _db.KpiDefinitions
            .FirstOrDefaultAsync(k => k.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"KPI definition {req.Id} not found.");

        if (!kpi.IsActive)
            throw new InvalidOperationException("KPI definition is already inactive.");

        kpi.IsActive  = false;
        kpi.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Shared projection helper ──────────────────────────────────────────────────

file static class KpiDefinitionCommandExtensions
{
    internal static KpiDefinitionDto ToDto(this KpiDefinition kpi, string? categoryCode) =>
        new(kpi.Id, kpi.CorporationId, kpi.Code, kpi.Name,
            kpi.CategoryId, categoryCode, kpi.Unit, kpi.Spec, kpi.IsActive,
            kpi.CreatedAt, kpi.UpdatedAt, kpi.RowVersion);
}
