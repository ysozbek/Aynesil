using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using Aynesil.Domain.Modules.Legal.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Commands;

// ── CreateContractTemplateCommand ─────────────────────────────────────────────

public record TemplateTranslationRequest(string Locale, string Title, string Body);

public record CreateContractTemplateCommand(
    Guid CorporationId,
    string Code,
    Guid? ContractTypeId,
    DateOnly? EffectiveFrom,
    IReadOnlyList<TemplateTranslationRequest>? Translations,
    Guid? CreatedBy = null) : IRequest<ContractTemplateDto>;

public class CreateContractTemplateCommandValidator : AbstractValidator<CreateContractTemplateCommand>
{
    public CreateContractTemplateCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9_]+$").WithMessage("Code must be lowercase alphanumeric with underscores.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class CreateContractTemplateCommandHandler
    : IRequestHandler<CreateContractTemplateCommand, ContractTemplateDto>
{
    private readonly IAppDbContext _db;

    public CreateContractTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ContractTemplateDto> Handle(
        CreateContractTemplateCommand req, CancellationToken ct)
    {
        var alreadyExists = await _db.ContractTemplates
            .AnyAsync(t => t.CorporationId == req.CorporationId
                        && t.Code == req.Code.Trim().ToLowerInvariant()
                        && t.IsCurrent, ct);

        if (alreadyExists)
            throw new InvalidOperationException(
                $"A current contract template with code '{req.Code}' already exists for this corporation.");

        var template = ContractTemplate.Create(
            req.CorporationId, req.Code, req.ContractTypeId, req.EffectiveFrom);

        if (req.Translations is { Count: > 0 })
            foreach (var tr in req.Translations)
                template.UpsertTranslation(tr.Locale, tr.Title, tr.Body);

        _db.ContractTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(template.Id, ct);
    }

    private async Task<ContractTemplateDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var t = await _db.ContractTemplates
            .Include(x => x.Translations)
            .AsNoTracking()
            .FirstAsync(x => x.Id == id, ct);

        var typeCode = t.ContractTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.ContractTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return MapToDto(t, typeCode);
    }

    internal static ContractTemplateDto MapToDto(
        ContractTemplate t, string? typeCode) => new(
        t.Id, t.CorporationId, t.Code,
        t.ContractTypeId, typeCode,
        t.Version, t.IsCurrent, t.EffectiveFrom,
        t.Translations.Select(x => new ContractTemplateTranslationDto(x.Locale, x.Title, x.Body)).ToList(),
        t.CreatedAt, t.UpdatedAt, t.RowVersion);
}

// ── UpdateContractTemplateCommand ─────────────────────────────────────────────

public record UpdateContractTemplateCommand(
    Guid Id,
    Guid? ContractTypeId,
    DateOnly? EffectiveFrom,
    IReadOnlyList<TemplateTranslationRequest>? Translations,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateContractTemplateCommandValidator : AbstractValidator<UpdateContractTemplateCommand>
{
    public UpdateContractTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class UpdateContractTemplateCommandHandler : IRequestHandler<UpdateContractTemplateCommand>
{
    private readonly IAppDbContext _db;

    public UpdateContractTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateContractTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.ContractTemplates
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Contract template {req.Id} not found.");

        template.Update(req.ContractTypeId, req.EffectiveFrom);

        if (req.Translations is { Count: > 0 })
            foreach (var tr in req.Translations)
                template.UpsertTranslation(tr.Locale, tr.Title, tr.Body);

        await _db.SaveChangesAsync(ct);
    }
}

// ── NewVersionContractTemplateCommand ─────────────────────────────────────────

public record NewVersionContractTemplateCommand(
    Guid CurrentTemplateId,
    DateOnly? EffectiveFrom,
    IReadOnlyList<TemplateTranslationRequest>? Translations,
    Guid? CreatedBy = null) : IRequest<ContractTemplateDto>;

public class NewVersionContractTemplateCommandValidator
    : AbstractValidator<NewVersionContractTemplateCommand>
{
    public NewVersionContractTemplateCommandValidator()
    {
        RuleFor(x => x.CurrentTemplateId).NotEmpty();
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class NewVersionContractTemplateCommandHandler
    : IRequestHandler<NewVersionContractTemplateCommand, ContractTemplateDto>
{
    private readonly IAppDbContext _db;

    public NewVersionContractTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ContractTemplateDto> Handle(
        NewVersionContractTemplateCommand req, CancellationToken ct)
    {
        var current = await _db.ContractTemplates
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == req.CurrentTemplateId, ct)
            ?? throw new KeyNotFoundException($"Contract template {req.CurrentTemplateId} not found.");

        // NewVersion() archives `current` and returns the new version entity.
        var next = current.NewVersion(req.EffectiveFrom);

        if (req.Translations is { Count: > 0 })
        {
            foreach (var tr in req.Translations)
                next.UpsertTranslation(tr.Locale, tr.Title, tr.Body);
        }
        else
        {
            // Copy translations from the current version as starting point.
            foreach (var tr in current.Translations)
                next.UpsertTranslation(tr.Locale, tr.Title, tr.Body);
        }

        _db.ContractTemplates.Add(next);
        await _db.SaveChangesAsync(ct);

        var typeCode = next.ContractTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == next.ContractTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return CreateContractTemplateCommandHandler.MapToDto(next, typeCode);
    }
}

// ── DeleteContractTemplateCommand ─────────────────────────────────────────────

public record DeleteContractTemplateCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteContractTemplateCommandHandler : IRequestHandler<DeleteContractTemplateCommand>
{
    private readonly IAppDbContext _db;

    public DeleteContractTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteContractTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.ContractTemplates
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Contract template {req.Id} not found.");

        var hasContracts = await _db.StudentContracts
            .AnyAsync(c => c.TemplateId == req.Id, ct);

        if (hasContracts)
            throw new InvalidOperationException(
                "Cannot delete a contract template that is referenced by student contracts.");

        template.Delete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
