using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── CreatePromotionCommand ────────────────────────────────────────────────────

public record CreatePromotionCommand(
    Guid CorporationId,
    string Code,
    string Name,
    decimal Value,
    bool IsPercentage = true,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null,
    int? MaxRedemptions = null) : IRequest<PromotionDto>;

public class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.IsPercentage)
            .WithMessage("Percentage promotion cannot exceed 100%.");
        RuleFor(x => x.MaxRedemptions).GreaterThan(0).When(x => x.MaxRedemptions.HasValue);
        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithMessage("ValidTo must be on or after ValidFrom.");
    }
}

public sealed class CreatePromotionCommandHandler
    : IRequestHandler<CreatePromotionCommand, PromotionDto>
{
    private readonly IAppDbContext _db;

    public CreatePromotionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PromotionDto> Handle(CreatePromotionCommand req, CancellationToken ct)
    {
        var duplicate = await _db.Promotions.AnyAsync(
            p => p.CorporationId == req.CorporationId && p.Code == req.Code, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"Promotion with code '{req.Code}' already exists for this corporation.");

        var promotion = Promotion.Create(
            req.CorporationId, req.Code, req.Name, req.Value,
            req.IsPercentage, req.ValidFrom, req.ValidTo, req.MaxRedemptions);

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync(ct);

        return PromotionMapper.ToDto(promotion);
    }
}

// ── UpdatePromotionCommand ────────────────────────────────────────────────────

public record UpdatePromotionCommand(
    Guid Id,
    string Code,
    string Name,
    decimal Value,
    bool IsPercentage,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? MaxRedemptions) : IRequest<PromotionDto>;

public class UpdatePromotionCommandValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.IsPercentage)
            .WithMessage("Percentage promotion cannot exceed 100%.");
        RuleFor(x => x.MaxRedemptions).GreaterThan(0).When(x => x.MaxRedemptions.HasValue);
    }
}

public sealed class UpdatePromotionCommandHandler
    : IRequestHandler<UpdatePromotionCommand, PromotionDto>
{
    private readonly IAppDbContext _db;

    public UpdatePromotionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PromotionDto> Handle(UpdatePromotionCommand req, CancellationToken ct)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Promotion {req.Id} not found.");

        promotion.UpdateDetails(
            req.Code, req.Name, req.Value, req.IsPercentage,
            req.ValidFrom, req.ValidTo, req.MaxRedemptions);

        await _db.SaveChangesAsync(ct);

        return PromotionMapper.ToDto(promotion);
    }
}

// ── ActivatePromotionCommand ──────────────────────────────────────────────────

public record ActivatePromotionCommand(Guid Id) : IRequest;

public sealed class ActivatePromotionCommandHandler
    : IRequestHandler<ActivatePromotionCommand>
{
    private readonly IAppDbContext _db;

    public ActivatePromotionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivatePromotionCommand req, CancellationToken ct)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Promotion {req.Id} not found.");

        promotion.Activate();
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeactivatePromotionCommand ────────────────────────────────────────────────

public record DeactivatePromotionCommand(Guid Id) : IRequest;

public sealed class DeactivatePromotionCommandHandler
    : IRequestHandler<DeactivatePromotionCommand>
{
    private readonly IAppDbContext _db;

    public DeactivatePromotionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeactivatePromotionCommand req, CancellationToken ct)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Promotion {req.Id} not found.");

        promotion.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}

// ── Shared mapper ─────────────────────────────────────────────────────────────

file static class PromotionMapper
{
    public static PromotionDto ToDto(Promotion p)
        => new(p.Id, p.CorporationId, p.Code, p.Name,
               p.IsPercentage, p.Value,
               p.ValidFrom, p.ValidTo, p.MaxRedemptions, p.IsActive);
}
