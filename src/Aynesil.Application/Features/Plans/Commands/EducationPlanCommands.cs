using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Commands;

// ── CreateEducationPlanCommand ────────────────────────────────────────────────

public record CreateEducationPlanCommand(
    Guid CorporationId,
    Guid StudentId,
    string Title,
    Guid? AcademicPeriodId,
    Guid? CampusId,
    Guid? PreparedBy,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<EducationPlanDto>;

public class CreateEducationPlanCommandValidator : AbstractValidator<CreateEducationPlanCommand>
{
    public CreateEducationPlanCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateEducationPlanCommandHandler
    : IRequestHandler<CreateEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(CreateEducationPlanCommand req, CancellationToken ct)
    {
        var studentExists = await _db.Students.AnyAsync(
            s => s.Id == req.StudentId && s.CorporationId == req.CorporationId, ct);

        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var plan = EducationPlan.Create(
            req.CorporationId, req.StudentId, req.Title,
            req.AcademicPeriodId, req.CampusId, req.PreparedBy,
            req.EffectiveFrom, req.EffectiveTo, _currentUser.UserId);

        _db.EducationPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── UpdateEducationPlanCommand ────────────────────────────────────────────────

public record UpdateEducationPlanCommand(
    Guid Id,
    string Title,
    Guid? AcademicPeriodId,
    Guid? CampusId,
    Guid? PreparedBy,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    int RowVersion) : IRequest<EducationPlanDto>;

public class UpdateEducationPlanCommandValidator : AbstractValidator<UpdateEducationPlanCommand>
{
    public UpdateEducationPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateEducationPlanCommandHandler
    : IRequestHandler<UpdateEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(UpdateEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        if (plan.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "The education plan was modified by another user. Please refresh and retry.");

        plan.UpdateDetails(
            req.Title, req.AcademicPeriodId, req.CampusId,
            req.PreparedBy, req.EffectiveFrom, req.EffectiveTo,
            _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── SubmitEducationPlanForReviewCommand ───────────────────────────────────────

public record SubmitEducationPlanForReviewCommand(Guid Id) : IRequest<EducationPlanDto>;

public sealed class SubmitEducationPlanForReviewCommandHandler
    : IRequestHandler<SubmitEducationPlanForReviewCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitEducationPlanForReviewCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(
        SubmitEducationPlanForReviewCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        plan.SubmitForReview(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── ApproveEducationPlanCommand ───────────────────────────────────────────────

public record ApproveEducationPlanCommand(
    Guid Id,
    Guid ApproverId,
    string? Comment) : IRequest<EducationPlanDto>;

public class ApproveEducationPlanCommandValidator : AbstractValidator<ApproveEducationPlanCommand>
{
    public ApproveEducationPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverId).NotEmpty();
    }
}

public sealed class ApproveEducationPlanCommandHandler
    : IRequestHandler<ApproveEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ApproveEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(ApproveEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        plan.Approve(req.ApproverId, _currentUser.UserId);

        var approval = EducationPlanApproval.Create(
            plan.CorporationId, plan.Id, req.ApproverId, "approved", req.Comment);

        _db.EducationPlanApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── RejectEducationPlanCommand ────────────────────────────────────────────────

public record RejectEducationPlanCommand(
    Guid Id,
    Guid ApproverId,
    string? Comment) : IRequest<EducationPlanDto>;

public sealed class RejectEducationPlanCommandHandler
    : IRequestHandler<RejectEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RejectEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(RejectEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        plan.Reject(_currentUser.UserId);

        var approval = EducationPlanApproval.Create(
            plan.CorporationId, plan.Id, req.ApproverId, "rejected", req.Comment);

        _db.EducationPlanApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── ActivateEducationPlanCommand ──────────────────────────────────────────────

public record ActivateEducationPlanCommand(Guid Id) : IRequest<EducationPlanDto>;

public sealed class ActivateEducationPlanCommandHandler
    : IRequestHandler<ActivateEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivateEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(ActivateEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        plan.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── CloseEducationPlanCommand ─────────────────────────────────────────────────

public record CloseEducationPlanCommand(Guid Id) : IRequest<EducationPlanDto>;

public sealed class CloseEducationPlanCommandHandler
    : IRequestHandler<CloseEducationPlanCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CloseEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(CloseEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        plan.Close(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── SetGuardianVisibilityCommand ──────────────────────────────────────────────

public record SetGuardianVisibilityCommand(Guid Id, bool Visible) : IRequest<EducationPlanDto>;

public sealed class SetGuardianVisibilityCommandHandler
    : IRequestHandler<SetGuardianVisibilityCommand, EducationPlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetGuardianVisibilityCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducationPlanDto> Handle(SetGuardianVisibilityCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        if (req.Visible)
            plan.MakeGuardianVisible(_currentUser.UserId);
        else
            plan.RevokeGuardianVisibility(_currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await PlanProjection.LoadAsync(_db, plan.Id, ct))!;
    }
}

// ── DeleteEducationPlanCommand ────────────────────────────────────────────────

public record DeleteEducationPlanCommand(Guid Id) : IRequest;

public sealed class DeleteEducationPlanCommandHandler : IRequestHandler<DeleteEducationPlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteEducationPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteEducationPlanCommand req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");

        if (plan.Status is not "draft")
            throw new InvalidOperationException(
                "Only draft plans can be deleted. Use Close to end an active plan.");

        plan.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
