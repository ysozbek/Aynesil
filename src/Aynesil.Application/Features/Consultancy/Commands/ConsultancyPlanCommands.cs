using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateConsultancyPlanCommand ──────────────────────────────────────────────

public record CreateConsultancyPlanCommand(
    Guid CorporationId,
    Guid InstitutionId,
    string Name,
    Guid? ConsultancyTypeId,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string? Scope,
    Guid? LeadEducatorId,
    Guid? CreatedBy = null) : IRequest<ConsultancyPlanDto>;

public class CreateConsultancyPlanCommandValidator
    : AbstractValidator<CreateConsultancyPlanCommand>
{
    public CreateConsultancyPlanCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
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

    public CreateConsultancyPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ConsultancyPlanDto> Handle(
        CreateConsultancyPlanCommand req, CancellationToken ct)
    {
        var institutionExists = await _db.Institutions
            .AnyAsync(i => i.Id == req.InstitutionId && i.DeletedAt == null, ct);

        if (!institutionExists)
            throw new KeyNotFoundException($"Institution {req.InstitutionId} not found.");

        var plan = ConsultancyPlan.Create(
            req.CorporationId, req.InstitutionId, req.Name,
            req.ConsultancyTypeId, req.PeriodStart, req.PeriodEnd,
            req.Scope, req.LeadEducatorId, req.CreatedBy);

        _db.ConsultancyPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        var institutionName = await _db.Institutions.AsNoTracking()
            .Where(i => i.Id == req.InstitutionId)
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
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

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

    public UpdateConsultancyPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Update(req.Name, req.ConsultancyTypeId,
            req.PeriodStart, req.PeriodEnd,
            req.Scope, req.LeadEducatorId, req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── ActivateConsultancyPlanCommand ────────────────────────────────────────────

public record ActivateConsultancyPlanCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class ActivateConsultancyPlanCommandHandler
    : IRequestHandler<ActivateConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;

    public ActivateConsultancyPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivateConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Activate(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CompleteConsultancyPlanCommand ────────────────────────────────────────────

public record CompleteConsultancyPlanCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class CompleteConsultancyPlanCommandHandler
    : IRequestHandler<CompleteConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;

    public CompleteConsultancyPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CompleteConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Complete(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CancelConsultancyPlanCommand ──────────────────────────────────────────────

public record CancelConsultancyPlanCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class CancelConsultancyPlanCommandHandler
    : IRequestHandler<CancelConsultancyPlanCommand>
{
    private readonly IAppDbContext _db;

    public CancelConsultancyPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelConsultancyPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        plan.Cancel(req.UpdatedBy);
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
            throw new InvalidOperationException(
                "Only draft plans can be deleted. Use Cancel to archive active plans.");

        var hasVisits = await _db.SchoolVisits
            .AnyAsync(v => v.ConsultancyPlanId == req.Id, ct);

        if (hasVisits)
            throw new InvalidOperationException(
                "Cannot delete a plan that already has associated visits.");

        _db.ConsultancyPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
    }
}
