using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Reschedule ────────────────────────────────────────────────────────────────

public record UpdateAssessmentSessionCommand(
    Guid Id,
    DateTimeOffset? ScheduledAt,
    Guid? AssessorId,
    Guid? CampusId,
    int RowVersion) : IRequest<AssessmentSessionDto>;

public class UpdateAssessmentSessionCommandValidator
    : AbstractValidator<UpdateAssessmentSessionCommand>
{
    public UpdateAssessmentSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateAssessmentSessionCommandHandler
    : IRequestHandler<UpdateAssessmentSessionCommand, AssessmentSessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAssessmentSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentSessionDto> Handle(
        UpdateAssessmentSessionCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment session {req.Id} not found.");

        session.UpdateSchedule(req.ScheduledAt, req.AssessorId, req.CampusId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── Start ─────────────────────────────────────────────────────────────────────

public record StartAssessmentSessionCommand(Guid Id, int RowVersion) : IRequest<AssessmentSessionDto>;

public sealed class StartAssessmentSessionCommandHandler
    : IRequestHandler<StartAssessmentSessionCommand, AssessmentSessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public StartAssessmentSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentSessionDto> Handle(
        StartAssessmentSessionCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment session {req.Id} not found.");

        session.Start(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── Complete (with score calculation) ────────────────────────────────────────

public record CompleteAssessmentSessionCommand(Guid Id, int RowVersion) : IRequest<AssessmentSessionDto>;

public sealed class CompleteAssessmentSessionCommandHandler
    : IRequestHandler<CompleteAssessmentSessionCommand, AssessmentSessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CompleteAssessmentSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentSessionDto> Handle(
        CompleteAssessmentSessionCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .Include(s => s.Responses)
                .ThenInclude(r => r.Item)
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment session {req.Id} not found.");

        var template = await _db.AssessmentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == session.TemplateId, ct);

        var totalScore = CalculateScore(session.Responses, template?.ScoringModel);

        session.Complete(totalScore, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }

    private static decimal? CalculateScore(
        IEnumerable<Domain.Modules.Assessment.Entities.AssessmentResponse> responses,
        string? scoringModel)
    {
        if (scoringModel is null or "none" or "rubric") return null;

        var scored = responses
            .Where(r => r.NumericValue.HasValue && r.Item is not null)
            .Select(r => r.NumericValue!.Value * r.Item!.Weight)
            .ToList();

        if (scored.Count == 0) return null;

        return scoringModel switch
        {
            "sum"     => scored.Sum(),
            "average" => scored.Average(),
            _         => null
        };
    }
}

// ── Cancel ────────────────────────────────────────────────────────────────────

public record CancelAssessmentSessionCommand(Guid Id, int RowVersion) : IRequest<AssessmentSessionDto>;

public sealed class CancelAssessmentSessionCommandHandler
    : IRequestHandler<CancelAssessmentSessionCommand, AssessmentSessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelAssessmentSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentSessionDto> Handle(
        CancelAssessmentSessionCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment session {req.Id} not found.");

        session.Cancel(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── Delete (soft-delete, planned only) ───────────────────────────────────────

public record DeleteAssessmentSessionCommand(Guid Id) : IRequest;

public sealed class DeleteAssessmentSessionCommandHandler
    : IRequestHandler<DeleteAssessmentSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteAssessmentSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAssessmentSessionCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment session {req.Id} not found.");

        if (session.Status != Domain.Modules.Assessment.Entities.AssessmentSession.SessionStatuses.Planned)
            throw new InvalidOperationException(
                "Only sessions in 'planned' status can be deleted.");

        session.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
