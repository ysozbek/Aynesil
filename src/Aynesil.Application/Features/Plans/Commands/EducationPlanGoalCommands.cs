using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Commands;

// ── AddGoalToPlanCommand ──────────────────────────────────────────────────────

public record AddGoalToPlanCommand(
    Guid EducationPlanId,
    Guid StudentGoalId,
    string Horizon,
    int SortOrder) : IRequest<EducationPlanDto>;

public class AddGoalToPlanCommandValidator : AbstractValidator<AddGoalToPlanCommand>
{
    public AddGoalToPlanCommandValidator()
    {
        RuleFor(x => x.EducationPlanId).NotEmpty();
        RuleFor(x => x.StudentGoalId).NotEmpty();
        RuleFor(x => x.Horizon)
            .Must(h => new[] { "long_term", "short_term" }.Contains(h))
            .WithMessage("Horizon must be long_term or short_term.");
    }
}

public sealed class AddGoalToPlanCommandHandler
    : IRequestHandler<AddGoalToPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;

    public AddGoalToPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducationPlanDto> Handle(AddGoalToPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans
            .Include(p => p.PlanGoals)
            .FirstOrDefaultAsync(p => p.Id == req.EducationPlanId, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.EducationPlanId} not found.");

        if (plan.Status is not ("draft" or "in_review"))
            throw new InvalidOperationException(
                $"Cannot add goals to a plan with status '{plan.Status}'.");

        var goalExists = await _db.StudentGoals.AnyAsync(
            g => g.Id == req.StudentGoalId && g.StudentId == plan.StudentId, ct);

        if (!goalExists)
            throw new KeyNotFoundException(
                $"StudentGoal {req.StudentGoalId} not found for this student.");

        var alreadyLinked = plan.PlanGoals.Any(pg => pg.StudentGoalId == req.StudentGoalId);
        if (alreadyLinked)
            throw new InvalidOperationException(
                $"Goal {req.StudentGoalId} is already linked to this plan.");

        var planGoal = EducationPlanGoal.Create(
            plan.CorporationId, plan.Id, req.StudentGoalId, req.Horizon, req.SortOrder);

        _db.EducationPlanGoals.Add(planGoal);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── RemoveGoalFromPlanCommand ─────────────────────────────────────────────────

public record RemoveGoalFromPlanCommand(
    Guid EducationPlanId,
    Guid PlanGoalId) : IRequest<EducationPlanDto>;

public sealed class RemoveGoalFromPlanCommandHandler
    : IRequestHandler<RemoveGoalFromPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;

    public RemoveGoalFromPlanCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducationPlanDto> Handle(RemoveGoalFromPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans
            .FirstOrDefaultAsync(p => p.Id == req.EducationPlanId, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.EducationPlanId} not found.");

        if (plan.Status is not ("draft" or "in_review"))
            throw new InvalidOperationException(
                $"Cannot remove goals from a plan with status '{plan.Status}'.");

        var planGoal = await _db.EducationPlanGoals
            .FirstOrDefaultAsync(pg => pg.Id == req.PlanGoalId
                                    && pg.EducationPlanId == req.EducationPlanId, ct)
            ?? throw new KeyNotFoundException($"PlanGoal {req.PlanGoalId} not found.");

        _db.EducationPlanGoals.Remove(planGoal);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── ReorderPlanGoalsCommand ───────────────────────────────────────────────────

public record ReorderPlanGoalsCommand(
    Guid EducationPlanId,
    IReadOnlyList<PlanGoalOrderItem> Items) : IRequest<EducationPlanDto>;

public record PlanGoalOrderItem(Guid PlanGoalId, int SortOrder);

public sealed class ReorderPlanGoalsCommandHandler
    : IRequestHandler<ReorderPlanGoalsCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;

    public ReorderPlanGoalsCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducationPlanDto> Handle(ReorderPlanGoalsCommand req, CancellationToken ct)
    {
        var planGoals = await _db.EducationPlanGoals
            .Where(pg => pg.EducationPlanId == req.EducationPlanId)
            .ToListAsync(ct);

        foreach (var item in req.Items)
        {
            var pg = planGoals.FirstOrDefault(x => x.Id == item.PlanGoalId);
            pg?.Reorder(item.SortOrder);
        }

        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, req.EducationPlanId, ct))!;
    }
}
