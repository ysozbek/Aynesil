using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Request ───────────────────────────────────────────────────────────────────

public record SubmitResponsesCommand(
    Guid SessionId,
    IReadOnlyList<ResponseInput> Responses) : IRequest<IReadOnlyList<AssessmentResponseDto>>;

public record ResponseInput(
    Guid ItemId,
    decimal? NumericValue,
    string? TextValue,
    string? ChoiceValue,
    string? Note);

// ── Validator ─────────────────────────────────────────────────────────────────

public class SubmitResponsesCommandValidator : AbstractValidator<SubmitResponsesCommand>
{
    public SubmitResponsesCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Responses).NotEmpty();
        RuleForEach(x => x.Responses).ChildRules(r =>
        {
            r.RuleFor(x => x.ItemId).NotEmpty();
        });
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

/// <summary>
/// Upserts evaluator responses for an in-progress session.
/// Existing responses are updated in-place; new item responses are inserted.
/// The DB UNIQUE constraint (session_id, item_id) guarantees idempotency.
/// </summary>
public sealed class SubmitResponsesCommandHandler
    : IRequestHandler<SubmitResponsesCommand, IReadOnlyList<AssessmentResponseDto>>
{
    private readonly IAppDbContext _db;

    public SubmitResponsesCommandHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AssessmentResponseDto>> Handle(
        SubmitResponsesCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new KeyNotFoundException($"Assessment session {req.SessionId} not found.");

        if (session.Status != AssessmentSession.SessionStatuses.InProgress)
            throw new InvalidOperationException(
                "Responses can only be submitted for sessions in 'in_progress' status.");

        var existing = await _db.AssessmentResponses
            .Where(r => r.AssessmentSessionId == req.SessionId)
            .ToListAsync(ct);

        var existingMap = existing.ToDictionary(r => r.ItemId);

        foreach (var input in req.Responses)
        {
            if (existingMap.TryGetValue(input.ItemId, out var resp))
            {
                resp.Update(input.NumericValue, input.TextValue, input.ChoiceValue, input.Note);
            }
            else
            {
                var newResp = AssessmentResponse.Create(
                    req.SessionId, input.ItemId,
                    input.NumericValue, input.TextValue, input.ChoiceValue, input.Note);
                _db.AssessmentResponses.Add(newResp);
            }
        }

        await _db.SaveChangesAsync(ct);

        var all = await _db.AssessmentResponses
            .Where(r => r.AssessmentSessionId == req.SessionId)
            .ToListAsync(ct);

        return all.Select(AssessmentProjection.ToResponseDto).ToList().AsReadOnly();
    }
}
