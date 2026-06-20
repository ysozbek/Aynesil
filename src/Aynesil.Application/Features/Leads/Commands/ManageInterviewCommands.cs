using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Leads.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// Complete Interview
// ═══════════════════════════════════════════════════════════════════════════════

public record CompleteInterviewCommand(
    Guid InterviewId,
    string? Outcome,
    string? Recommendation,
    Guid? ConductedBy,
    int RowVersion) : IRequest<InterviewDto>;

public class CompleteInterviewCommandValidator : AbstractValidator<CompleteInterviewCommand>
{
    public CompleteInterviewCommandValidator()
    {
        RuleFor(x => x.InterviewId).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class CompleteInterviewCommandHandler : IRequestHandler<CompleteInterviewCommand, InterviewDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CompleteInterviewCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<InterviewDto> Handle(CompleteInterviewCommand req, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .FirstOrDefaultAsync(i => i.Id == req.InterviewId, ct)
            ?? throw new NotFoundException("Interview", req.InterviewId);

        if (interview.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The interview was modified by another user. Please refresh and retry.")]);

        interview.Complete(req.Outcome, req.Recommendation, req.ConductedBy ?? _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        var campusName = interview.CampusId.HasValue
            ? await _db.Campuses.Where(c => c.Id == interview.CampusId.Value).Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;
        var conductedByName = interview.ConductedBy.HasValue
            ? await _db.UserAccounts.Where(u => u.Id == interview.ConductedBy.Value).Select(u => u.FullName).FirstOrDefaultAsync(ct)
            : null;

        return interview.ToDto(campusName, conductedByName);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Cancel Interview
// ═══════════════════════════════════════════════════════════════════════════════

public record CancelInterviewCommand(Guid InterviewId, int RowVersion) : IRequest;

public sealed class CancelInterviewCommandHandler : IRequestHandler<CancelInterviewCommand>
{
    private readonly IAppDbContext _db;

    public CancelInterviewCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelInterviewCommand req, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .FirstOrDefaultAsync(i => i.Id == req.InterviewId, ct)
            ?? throw new NotFoundException("Interview", req.InterviewId);

        if (interview.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The interview was modified by another user. Please refresh and retry.")]);

        interview.Cancel();
        await _db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Reschedule Interview
// ═══════════════════════════════════════════════════════════════════════════════

public record RescheduleInterviewCommand(
    Guid InterviewId,
    DateTimeOffset NewScheduledAt,
    int RowVersion) : IRequest<InterviewDto>;

public class RescheduleInterviewCommandValidator : AbstractValidator<RescheduleInterviewCommand>
{
    public RescheduleInterviewCommandValidator()
    {
        RuleFor(x => x.InterviewId).NotEmpty();
        RuleFor(x => x.NewScheduledAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("New scheduled date must be in the future.");
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class RescheduleInterviewCommandHandler : IRequestHandler<RescheduleInterviewCommand, InterviewDto>
{
    private readonly IAppDbContext _db;

    public RescheduleInterviewCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InterviewDto> Handle(RescheduleInterviewCommand req, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .FirstOrDefaultAsync(i => i.Id == req.InterviewId, ct)
            ?? throw new NotFoundException("Interview", req.InterviewId);

        if (interview.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The interview was modified by another user. Please refresh and retry.")]);

        interview.Reschedule(req.NewScheduledAt);
        await _db.SaveChangesAsync(ct);

        var campusName = interview.CampusId.HasValue
            ? await _db.Campuses.Where(c => c.Id == interview.CampusId.Value).Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        return interview.ToDto(campusName, null);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Mark Interview No-Show
// ═══════════════════════════════════════════════════════════════════════════════

public record MarkInterviewNoShowCommand(Guid InterviewId, int RowVersion) : IRequest;

public sealed class MarkInterviewNoShowCommandHandler : IRequestHandler<MarkInterviewNoShowCommand>
{
    private readonly IAppDbContext _db;

    public MarkInterviewNoShowCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(MarkInterviewNoShowCommand req, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .FirstOrDefaultAsync(i => i.Id == req.InterviewId, ct)
            ?? throw new NotFoundException("Interview", req.InterviewId);

        if (interview.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The interview was modified by another user. Please refresh and retry.")]);

        interview.MarkNoShow();
        await _db.SaveChangesAsync(ct);
    }
}
