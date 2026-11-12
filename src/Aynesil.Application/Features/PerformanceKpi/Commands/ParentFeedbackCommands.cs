using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Portal;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Commands;

// ── SubmitParentFeedbackCommand ───────────────────────────────────────────────

/// <summary>
/// Submit a parent satisfaction rating for a session.
/// Validates that the session exists and belongs to the correct corporation.
/// Rating is 1–5 (enforced by DB check constraint and FluentValidation).
/// One feedback record per guardian/session pair is not enforced at DB level —
/// guardians may submit multiple ratings for the same session.
/// </summary>
public record SubmitParentFeedbackCommand(
    Guid? GuardianId,
    Guid? EducatorId,
    Guid? SessionId,
    short? Rating,
    string? Comment) : IRequest<ParentFeedbackDto>;

public class SubmitParentFeedbackCommandValidator
    : AbstractValidator<SubmitParentFeedbackCommand>
{
    public SubmitParentFeedbackCommandValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween((short)1, (short)5)
            .When(x => x.Rating.HasValue);
        RuleFor(x => x).Must(x => x.Rating.HasValue || !string.IsNullOrWhiteSpace(x.Comment))
            .WithName("Feedback")
            .WithMessage("At least a rating or a comment must be provided.");
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment != null);
    }
}

public sealed class SubmitParentFeedbackCommandHandler
    : IRequestHandler<SubmitParentFeedbackCommand, ParentFeedbackDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public SubmitParentFeedbackCommandHandler(
        IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ParentFeedbackDto> Handle(
        SubmitParentFeedbackCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var (guardianId, _) = await PortalGuardian.ResolveAsync(
            _db, corporationId, _currentUser.UserId, req.GuardianId, ct);

        if (req.SessionId.HasValue)
        {
            var sessionExists = await _db.Sessions.AnyAsync(
                s => s.Id == req.SessionId.Value
                  && s.CorporationId == corporationId
                  && s.DeletedAt == null, ct);

            if (!sessionExists)
                throw new KeyNotFoundException(
                    $"Session {req.SessionId} not found in this corporation.");
        }

        if (req.EducatorId.HasValue)
        {
            var educatorExists = await _db.Educators.AnyAsync(
                e => e.Id == req.EducatorId.Value
                  && e.CorporationId == corporationId
                  && e.DeletedAt == null, ct);

            if (!educatorExists)
                throw new KeyNotFoundException(
                    $"Educator {req.EducatorId} not found in this corporation.");
        }

        var feedback = ParentFeedback.Create(
            corporationId,
            guardianId,
            req.EducatorId,
            req.SessionId,
            req.Rating,
            req.Comment);

        _db.ParentFeedbacks.Add(feedback);
        await _db.SaveChangesAsync(ct);

        return new ParentFeedbackDto(
            feedback.Id, feedback.CorporationId,
            feedback.GuardianId, feedback.EducatorId, feedback.SessionId,
            feedback.Rating, feedback.Comment, feedback.CreatedAt);
    }
}
