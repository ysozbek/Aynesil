using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Commands;

// ── AddPlanReviewCommand ──────────────────────────────────────────────────────

public record AddPlanReviewCommand(
    Guid EducationPlanId,
    DateOnly ReviewedOn,
    Guid? ReviewerId,
    string? Summary,
    string? Outcome) : IRequest<EducationPlanDto>;

public class AddPlanReviewCommandValidator : AbstractValidator<AddPlanReviewCommand>
{
    public AddPlanReviewCommandValidator()
    {
        RuleFor(x => x.EducationPlanId).NotEmpty();
        RuleFor(x => x.ReviewedOn).NotEmpty();
        RuleFor(x => x.Outcome)
            .Must(o => o is null || new[] { "on_track", "needs_revision", "met" }.Contains(o))
            .WithMessage("Outcome must be on_track, needs_revision, or met.");
    }
}

public sealed class AddPlanReviewCommandHandler
    : IRequestHandler<AddPlanReviewCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;

    public AddPlanReviewCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducationPlanDto> Handle(AddPlanReviewCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans
            .FirstOrDefaultAsync(p => p.Id == req.EducationPlanId, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.EducationPlanId} not found.");

        if (plan.Status is "draft" or "closed")
            throw new InvalidOperationException(
                $"Cannot add a review to a plan with status '{plan.Status}'.");

        var review = EducationPlanReview.Create(
            plan.CorporationId, plan.Id,
            req.ReviewedOn, req.ReviewerId, req.Summary, req.Outcome);

        _db.EducationPlanReviews.Add(review);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── ReviseEducationPlanCommand ────────────────────────────────────────────────

/// <summary>
/// Creates a new plan version: increments version, captures JSON snapshot of current state,
/// stores it in EducationPlanRevision, and sets plan status to 'revised'.
/// </summary>
public record ReviseEducationPlanCommand(
    Guid Id,
    string? ChangeSummary) : IRequest<EducationPlanDto>;

public class ReviseEducationPlanCommandValidator : AbstractValidator<ReviseEducationPlanCommand>
{
    public ReviseEducationPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ChangeSummary).MaximumLength(2000).When(x => x.ChangeSummary is not null);
    }
}

public sealed class ReviseEducationPlanCommandHandler
    : IRequestHandler<ReviseEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReviseEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(ReviseEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans
            .Include(p => p.PlanGoals)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        var snapshot = new
        {
            plan.Id,
            plan.Title,
            plan.Version,
            plan.Status,
            plan.EffectiveFrom,
            plan.EffectiveTo,
            Goals = plan.PlanGoals.Select(pg => new { pg.StudentGoalId, pg.Horizon, pg.SortOrder })
        };

        var revision = plan.CreateRevision(
            req.ChangeSummary, snapshot, _currentUser.UserId ?? Guid.Empty);

        _db.EducationPlanRevisions.Add(revision);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}
