using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Commands;

// ── CreateStudentGoalCommand ──────────────────────────────────────────────────

public record CreateStudentGoalCommand(
    Guid CorporationId,
    Guid StudentId,
    string Statement,
    string Horizon,
    Guid? TemplateId,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    Guid? ParentGoalId,
    string? MasteryCriteria,
    string? Baseline,
    decimal? TargetValue,
    DateOnly? StartDate,
    DateOnly? TargetDate) : IRequest<StudentGoalDto>;

public class CreateStudentGoalCommandValidator : AbstractValidator<CreateStudentGoalCommand>
{
    public CreateStudentGoalCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Statement).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Horizon)
            .Must(h => new[] { "long_term", "short_term" }.Contains(h))
            .WithMessage("Horizon must be long_term or short_term.");
        RuleFor(x => x.TargetValue).GreaterThanOrEqualTo(0).When(x => x.TargetValue.HasValue);
    }
}

public sealed class CreateStudentGoalCommandHandler
    : IRequestHandler<CreateStudentGoalCommand, StudentGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateStudentGoalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentGoalDto> Handle(CreateStudentGoalCommand req, CancellationToken ct)
    {
        if (req.TemplateId.HasValue)
        {
            var templateExists = await _db.GoalTemplates.AnyAsync(
                t => t.Id == req.TemplateId.Value, ct);
            if (!templateExists)
                throw new KeyNotFoundException($"GoalTemplate {req.TemplateId} not found.");
        }

        if (req.ParentGoalId.HasValue)
        {
            var parentExists = await _db.StudentGoals.AnyAsync(
                g => g.Id == req.ParentGoalId.Value
                  && g.CorporationId == req.CorporationId
                  && g.StudentId == req.StudentId
                  && g.Horizon == "long_term", ct);
            if (!parentExists)
                throw new KeyNotFoundException(
                    $"Parent long-term goal {req.ParentGoalId} not found for this student.");
        }

        var goal = StudentGoal.Create(
            req.CorporationId, req.StudentId, req.Statement, req.Horizon,
            req.TemplateId, req.CategoryId, req.DevelopmentAreaId,
            req.ParentGoalId, req.MasteryCriteria, req.Baseline,
            req.TargetValue, req.StartDate, req.TargetDate,
            _currentUser.UserId);

        _db.StudentGoals.Add(goal);
        await _db.SaveChangesAsync(ct);

        return (await GoalProjection.LoadAsync(_db, goal.Id, ct))!;
    }
}

// ── UpdateStudentGoalCommand ──────────────────────────────────────────────────

public record UpdateStudentGoalCommand(
    Guid Id,
    string Statement,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    string? MasteryCriteria,
    string? Baseline,
    decimal? TargetValue,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    int RowVersion) : IRequest<StudentGoalDto>;

public class UpdateStudentGoalCommandValidator : AbstractValidator<UpdateStudentGoalCommand>
{
    public UpdateStudentGoalCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Statement).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TargetValue).GreaterThanOrEqualTo(0).When(x => x.TargetValue.HasValue);
    }
}

public sealed class UpdateStudentGoalCommandHandler
    : IRequestHandler<UpdateStudentGoalCommand, StudentGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateStudentGoalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentGoalDto> Handle(UpdateStudentGoalCommand req, CancellationToken ct)
    {
        var goal = await _db.StudentGoals.FirstOrDefaultAsync(g => g.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"StudentGoal {req.Id} not found.");

        if (goal.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "The goal was modified by another user. Please refresh and retry.");

        goal.UpdateDetails(
            req.Statement, req.CategoryId, req.DevelopmentAreaId,
            req.MasteryCriteria, req.Baseline, req.TargetValue,
            req.StartDate, req.TargetDate, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await GoalProjection.LoadAsync(_db, goal.Id, ct))!;
    }
}

// ── ChangeStudentGoalStatusCommand ────────────────────────────────────────────

public record ChangeStudentGoalStatusCommand(
    Guid Id,
    string NewStatus,
    DateOnly? AchievedDate) : IRequest<StudentGoalDto>;

public class ChangeStudentGoalStatusCommandValidator
    : AbstractValidator<ChangeStudentGoalStatusCommand>
{
    public ChangeStudentGoalStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewStatus)
            .Must(s => new[] { "active", "achieved", "discontinued", "on_hold" }.Contains(s))
            .WithMessage("Status must be active, achieved, discontinued, or on_hold.");
    }
}

public sealed class ChangeStudentGoalStatusCommandHandler
    : IRequestHandler<ChangeStudentGoalStatusCommand, StudentGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ChangeStudentGoalStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentGoalDto> Handle(ChangeStudentGoalStatusCommand req, CancellationToken ct)
    {
        var goal = await _db.StudentGoals.FirstOrDefaultAsync(g => g.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"StudentGoal {req.Id} not found.");

        goal.ChangeStatus(req.NewStatus, req.AchievedDate, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await GoalProjection.LoadAsync(_db, goal.Id, ct))!;
    }
}

// ── DeleteStudentGoalCommand ──────────────────────────────────────────────────

public record DeleteStudentGoalCommand(Guid Id) : IRequest;

public sealed class DeleteStudentGoalCommandHandler : IRequestHandler<DeleteStudentGoalCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStudentGoalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteStudentGoalCommand req, CancellationToken ct)
    {
        var goal = await _db.StudentGoals.FirstOrDefaultAsync(g => g.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"StudentGoal {req.Id} not found.");

        goal.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
