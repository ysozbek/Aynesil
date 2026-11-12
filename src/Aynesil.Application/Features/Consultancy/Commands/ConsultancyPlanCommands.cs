using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateConsultancyPlanCommand ──────────────────────────────────────────────

public record CreateConsultancyPlanCommand(
    Guid InstitutionId,
    string Name,
    Guid? ConsultancyTypeId,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string? Scope,
    Guid? LeadEducatorId) : IRequest<ConsultancyPlanDto>;

public class CreateConsultancyPlanCommandValidator
    : AbstractValidator<CreateConsultancyPlanCommand>
{
    public CreateConsultancyPlanCommandValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x)
            .Must(x => !(x.PeriodStart.HasValue && x.PeriodEnd.HasValue
                      && x.PeriodEnd < x.PeriodStart))
            .WithMessage("Period end cannot be before period start.");
    }
}

public sealed class CreateConsultancyPlanCommandHandler
    : IRequestHandler<CreateConsultancyPlanCommand, ConsultancyPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public CreateConsultancyPlanCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<ConsultancyPlanDto> Handle(
        CreateConsultancyPlanCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var institutionExists = await _db.Institutions
            .AnyAsync(i => i.CorporationId == corporationId && i.Id == req.InstitutionId && i.DeletedAt == null, ct);

        if (!institutionExists)
            throw new KeyNotFoundException($"Institution {req.InstitutionId} not found.");

        var plan = ConsultancyPlan.Create(
            corporationId, req.InstitutionId, req.Name,
            req.ConsultancyTypeId, req.PeriodStart, req.PeriodEnd,
            req.Scope, req.LeadEducatorId, _currentUser.UserId);

        _db.ConsultancyPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        var institutionName = await _db.Institutions.AsNoTracking()
            .Where(i => i.CorporationId == corporationId && i.Id == req.InstitutionId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var typeCode = req.ConsultancyTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.ConsultancyTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new ConsultancyPlanDto(
            plan.Id, plan.CorporationId, plan.InstitutionId, institutionName,
            plan.ConsultancyTypeId, typeCode,
            plan.Name, plan.PeriodStart, plan.PeriodEnd,
            plan.Scope, plan.LeadEducatorId, plan.Status,
            plan.CreatedAt, plan.UpdatedAt, plan.RowVersion);
    }
}

// ── UpdateConsultancyPlanCommand ──────────────────────────────────────────────

public record UpdateConsultancyPlanCommand(
    Guid Id,
    string Name,
    Guid? ConsultancyTypeId,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string? Scope,
    Guid? LeadEducatorId,
    int RowVersion) : IRequest;

public class UpdateConsultancyPlanCommandValidator
    : AbstractValidator<UpdateConsultancyPlanCommand>
{
    public UpdateConsultancyPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => !(x.PeriodStart.HasValue && x.PeriodEnd.HasValue
                      && x.PeriodEnd < x.PeriodStart))
            .WithMessage("Period end cannot be before period start.");
    }
}

public sealed class UpdateConsultancyPlanCommandHandler
    : IRequestHandler<UpdateConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateConsultancyPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Update(req.Name, req.ConsultancyTypeId,
            req.PeriodStart, req.PeriodEnd,
            req.Scope, req.LeadEducatorId, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
    }
}

// ── ActivateConsultancyPlanCommand ────────────────────────────────────────────

public record ActivateConsultancyPlanCommand(Guid Id) : IRequest;

public sealed class ActivateConsultancyPlanCommandHandler
    : IRequestHandler<ActivateConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivateConsultancyPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CompleteConsultancyPlanCommand ────────────────────────────────────────────

public record CompleteConsultancyPlanCommand(Guid Id) : IRequest;

public sealed class CompleteConsultancyPlanCommandHandler
    : IRequestHandler<CompleteConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CompleteConsultancyPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(CompleteConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Complete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CancelConsultancyPlanCommand ──────────────────────────────────────────────

public record CancelConsultancyPlanCommand(Guid Id) : IRequest;

public sealed class CancelConsultancyPlanCommandHandler
    : IRequestHandler<CancelConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelConsultancyPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Cancel(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteConsultancyPlanCommand ──────────────────────────────────────────────

public record DeleteConsultancyPlanCommand(Guid Id) : IRequest;

public sealed class DeleteConsultancyPlanCommandHandler
    : IRequestHandler<DeleteConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;

    public DeleteConsultancyPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        if (plan.Status != "draft")
            throw new BusinessRuleViolationException(
                "Only draft plans can be deleted. Use Cancel to archive active plans.");

        var hasVisits = await _db.SchoolVisits
            .AnyAsync(v => v.ConsultancyPlanId == req.Id, ct);

        if (hasVisits)
            throw new BusinessRuleViolationException(
                "Cannot delete a plan that already has associated visits.");

        _db.ConsultancyPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
    }
}
