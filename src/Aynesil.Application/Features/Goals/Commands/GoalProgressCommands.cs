using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Commands;

// ── RecordGoalProgressCommand ─────────────────────────────────────────────────

public record RecordGoalProgressCommand(
    Guid StudentGoalId,
    DateOnly MeasuredOn,
    decimal? MeasuredValue,
    decimal? PercentComplete,
    string? Trend,
    string? Note,
    Guid? SessionId) : IRequest<GoalProgressDto>;

public class RecordGoalProgressCommandValidator : AbstractValidator<RecordGoalProgressCommand>
{
    public RecordGoalProgressCommandValidator()
    {
        RuleFor(x => x.StudentGoalId).NotEmpty();
        RuleFor(x => x.MeasuredOn).NotEmpty();
        RuleFor(x => x.PercentComplete)
            .InclusiveBetween(0, 100)
            .When(x => x.PercentComplete.HasValue)
            .WithMessage("PercentComplete must be between 0 and 100.");
        RuleFor(x => x.Trend)
            .Must(t => t is null || new[] { "improving", "stable", "declining" }.Contains(t))
            .WithMessage("Trend must be improving, stable, or declining.");
    }
}

public sealed class RecordGoalProgressCommandHandler
    : IRequestHandler<RecordGoalProgressCommand, GoalProgressDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordGoalProgressCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GoalProgressDto> Handle(RecordGoalProgressCommand req, CancellationToken ct)
    {
        var goal = await _db.StudentGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == req.StudentGoalId, ct)
            ?? throw new KeyNotFoundException($"StudentGoal {req.StudentGoalId} not found.");

        var progress = GoalProgress.Record(
            goal.CorporationId,
            req.StudentGoalId,
            req.MeasuredOn,
            req.MeasuredValue,
            req.PercentComplete,
            req.Trend,
            req.Note,
            _currentUser.UserId,
            req.SessionId);

        _db.GoalProgressRecords.Add(progress);
        await _db.SaveChangesAsync(ct);

        return GoalProjection.ToProgressDto(progress);
    }
}
