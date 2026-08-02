using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── RegisterParticipationCommand ──────────────────────────────────────────────

public record RegisterParticipationCommand(
    Guid CorporationId,
    Guid CampActivityId,
    Guid CampEnrollmentId,
    string Status = "registered",
    string? Notes = null,
    Guid? RecordedBy = null) : IRequest<CampActivityParticipationDto>;

public class RegisterParticipationCommandValidator
    : AbstractValidator<RegisterParticipationCommand>
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public RegisterParticipationCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampActivityId).NotEmpty();
        RuleFor(x => x.CampEnrollmentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be: registered, attended, absent, or excused.");
    }
}

public sealed class RegisterParticipationCommandHandler
    : IRequestHandler<RegisterParticipationCommand, CampActivityParticipationDto>
{
    private readonly IAppDbContext _db;

    public RegisterParticipationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampActivityParticipationDto> Handle(
        RegisterParticipationCommand req, CancellationToken ct)
    {
        var activity = await _db.CampActivities
            .FirstOrDefaultAsync(a => a.Id == req.CampActivityId
                                   && a.DeletedAt == null
                                   && a.IsActive, ct)
            ?? throw new KeyNotFoundException($"Camp activity {req.CampActivityId} not found.");

        var enrollment = await _db.CampEnrollments
            .FirstOrDefaultAsync(e => e.Id == req.CampEnrollmentId
                                   && e.Status == "enrolled", ct)
            ?? throw new InvalidOperationException(
                $"Enrollment {req.CampEnrollmentId} not found or student is not actively enrolled.");

        if (enrollment.CampPeriodId != activity.CampPeriodId)
            throw new InvalidOperationException(
                "Enrollment and activity must belong to the same camp period.");

        var duplicate = await _db.CampActivityParticipations
            .AnyAsync(p => p.CampActivityId == req.CampActivityId
                        && p.CampEnrollmentId == req.CampEnrollmentId, ct);
        if (duplicate)
            throw new InvalidOperationException(
                "Participation already recorded for this enrollment and activity.");

        if (activity.Capacity.HasValue && req.Status is "registered" or "attended")
        {
            var count = await _db.CampActivityParticipations
                .CountAsync(p => p.CampActivityId == req.CampActivityId
                              && (p.Status == "registered" || p.Status == "attended"), ct);
            if (count >= activity.Capacity.Value)
                throw new InvalidOperationException("Activity is at full capacity.");
        }

        var participation = CampActivityParticipation.Register(
            req.CorporationId, req.CampActivityId, req.CampEnrollmentId,
            req.Status, req.Notes, req.RecordedBy);

        _db.CampActivityParticipations.Add(participation);
        await _db.SaveChangesAsync(ct);

        return new CampActivityParticipationDto(
            participation.Id, participation.CorporationId,
            participation.CampActivityId, participation.CampEnrollmentId,
            participation.Status, participation.Notes,
            participation.RecordedBy, participation.RecordedAt);
    }
}

// ── UpdateParticipationCommand ────────────────────────────────────────────────

public record UpdateParticipationCommand(
    Guid Id,
    string Status,
    string? Notes = null,
    Guid? RecordedBy = null) : IRequest;

public class UpdateParticipationCommandValidator : AbstractValidator<UpdateParticipationCommand>
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public UpdateParticipationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be: registered, attended, absent, or excused.");
    }
}

public sealed class UpdateParticipationCommandHandler : IRequestHandler<UpdateParticipationCommand>
{
    private readonly IAppDbContext _db;

    public UpdateParticipationCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateParticipationCommand req, CancellationToken ct)
    {
        var participation = await _db.CampActivityParticipations
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Participation {req.Id} not found.");

        participation.UpdateStatus(req.Status, req.Notes, req.RecordedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── BulkRegisterParticipationCommand ──────────────────────────────────────────

public record BulkParticipationInput(Guid EnrollmentId, string Status, string? Notes);

public record BulkRegisterParticipationCommand(
    Guid CorporationId,
    Guid CampActivityId,
    IReadOnlyList<BulkParticipationInput> Records,
    Guid? RecordedBy = null) : IRequest<int>;

public class BulkRegisterParticipationCommandValidator
    : AbstractValidator<BulkRegisterParticipationCommand>
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public BulkRegisterParticipationCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampActivityId).NotEmpty();
        RuleFor(x => x.Records).NotEmpty();
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(x => x.EnrollmentId).NotEmpty();
            r.RuleFor(x => x.Status)
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage("Status must be: registered, attended, absent, or excused.");
        });
    }
}

public sealed class BulkRegisterParticipationCommandHandler
    : IRequestHandler<BulkRegisterParticipationCommand, int>
{
    private readonly IAppDbContext _db;

    public BulkRegisterParticipationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<int> Handle(BulkRegisterParticipationCommand req, CancellationToken ct)
    {
        var activityExists = await _db.CampActivities
            .AnyAsync(a => a.Id == req.CampActivityId && a.DeletedAt == null, ct);
        if (!activityExists)
            throw new KeyNotFoundException($"Camp activity {req.CampActivityId} not found.");

        var enrollmentIds = req.Records.Select(r => r.EnrollmentId).ToList();
        var existing = await _db.CampActivityParticipations
            .Where(p => p.CampActivityId == req.CampActivityId
                     && enrollmentIds.Contains(p.CampEnrollmentId))
            .Select(p => p.CampEnrollmentId)
            .ToHashSetAsync(ct);

        var added = 0;
        foreach (var record in req.Records)
        {
            if (existing.Contains(record.EnrollmentId))
                continue;

            _db.CampActivityParticipations.Add(CampActivityParticipation.Register(
                req.CorporationId, req.CampActivityId, record.EnrollmentId,
                record.Status, record.Notes, req.RecordedBy));
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync(ct);

        return added;
    }
}
