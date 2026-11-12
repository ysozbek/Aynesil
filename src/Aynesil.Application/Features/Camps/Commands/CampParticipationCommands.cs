using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── RegisterParticipationCommand ──────────────────────────────────────────────

public record RegisterParticipationCommand(
    Guid CampActivityId,
    Guid CampEnrollmentId,
    string Status = "registered",
    string? Notes = null) : IRequest<CampActivityParticipationDto>;

public class RegisterParticipationCommandValidator
    : AbstractValidator<RegisterParticipationCommand>
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public RegisterParticipationCommandValidator()
    {
        RuleFor(x => x.CampActivityId).NotEmpty();
        RuleFor(x => x.CampEnrollmentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Status"), EnumSets.CampParticipationStatus);
    }
}

public sealed class RegisterParticipationCommandHandler
    : IRequestHandler<RegisterParticipationCommand, CampActivityParticipationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public RegisterParticipationCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampActivityParticipationDto> Handle(
        RegisterParticipationCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var activity = await _db.CampActivities
            .FirstOrDefaultAsync(a => a.CorporationId == corporationId && a.Id == req.CampActivityId
                                   && a.DeletedAt == null
                                   && a.IsActive, ct)
            ?? throw new KeyNotFoundException($"Camp activity {req.CampActivityId} not found.");

        var enrollment = await _db.CampEnrollments
            .FirstOrDefaultAsync(e => e.CorporationId == corporationId && e.Id == req.CampEnrollmentId
                                   && e.Status == "enrolled", ct)
            ?? throw new BusinessRuleViolationException(MsgKeys.Rule.EnrollmentNotActive, req.CampEnrollmentId);

        if (enrollment.CampPeriodId != activity.CampPeriodId)
            throw new BusinessRuleViolationException(MsgKeys.Rule.EnrollmentActivityPeriodMismatch);

        var duplicate = await _db.CampActivityParticipations
            .AnyAsync(p => p.CorporationId == corporationId && p.CampActivityId == req.CampActivityId
                        && p.CampEnrollmentId == req.CampEnrollmentId, ct);
        if (duplicate)
            throw new BusinessRuleViolationException(MsgKeys.Rule.ParticipationAlreadyRecorded);

        if (activity.Capacity.HasValue && req.Status is "registered" or "attended")
        {
            var count = await _db.CampActivityParticipations
                .CountAsync(p => p.CorporationId == corporationId && (p.CampActivityId == req.CampActivityId
                              && (p.Status == "registered" || p.Status == "attended")), ct);
            if (count >= activity.Capacity.Value)
                throw new BusinessRuleViolationException(MsgKeys.Rule.ActivityFull);
        }

        var participation = CampActivityParticipation.Register(
            corporationId, req.CampActivityId, req.CampEnrollmentId,
            req.Status, req.Notes, _currentUser.UserId);

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
    string? Notes = null) : IRequest;

public class UpdateParticipationCommandValidator : AbstractValidator<UpdateParticipationCommand>
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public UpdateParticipationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Status"), EnumSets.CampParticipationStatus);
    }
}

public sealed class UpdateParticipationCommandHandler : IRequestHandler<UpdateParticipationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateParticipationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateParticipationCommand req, CancellationToken ct)
    {
        var participation = await _db.CampActivityParticipations
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Participation {req.Id} not found.");

        participation.UpdateStatus(req.Status, req.Notes, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── BulkRegisterParticipationCommand ──────────────────────────────────────────

public record BulkParticipationInput(Guid EnrollmentId, string Status, string? Notes);

public record BulkRegisterParticipationCommand(
    Guid CampActivityId,
    IReadOnlyList<BulkParticipationInput> Records) : IRequest<int>;

public class BulkRegisterParticipationCommandValidator
    : AbstractValidator<BulkRegisterParticipationCommand>
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public BulkRegisterParticipationCommandValidator()
    {
        RuleFor(x => x.CampActivityId).NotEmpty();
        RuleFor(x => x.Records).NotEmpty();
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(x => x.EnrollmentId).NotEmpty();
            r.RuleFor(x => x.Status)
                .Must(s => ValidStatuses.Contains(s))
                .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Status"), EnumSets.CampParticipationStatus);
        });
    }
}

public sealed class BulkRegisterParticipationCommandHandler
    : IRequestHandler<BulkRegisterParticipationCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public BulkRegisterParticipationCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<int> Handle(BulkRegisterParticipationCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var activityExists = await _db.CampActivities
            .AnyAsync(a => a.CorporationId == corporationId && a.Id == req.CampActivityId && a.DeletedAt == null, ct);
        if (!activityExists)
            throw new KeyNotFoundException($"Camp activity {req.CampActivityId} not found.");

        var enrollmentIds = req.Records.Select(r => r.EnrollmentId).ToList();
        var existing = await _db.CampActivityParticipations
            .Where(p => p.CorporationId == corporationId && p.CampActivityId == req.CampActivityId
                     && enrollmentIds.Contains(p.CampEnrollmentId))
            .Select(p => p.CampEnrollmentId)
            .ToHashSetAsync(ct);

        var added = 0;
        foreach (var record in req.Records)
        {
            if (existing.Contains(record.EnrollmentId))
                continue;

            _db.CampActivityParticipations.Add(CampActivityParticipation.Register(
                corporationId, req.CampActivityId, record.EnrollmentId,
                record.Status, record.Notes, _currentUser.UserId));
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync(ct);

        return added;
    }
}
