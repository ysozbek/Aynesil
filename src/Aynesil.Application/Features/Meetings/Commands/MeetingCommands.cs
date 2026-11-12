using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Meetings.Dtos;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Meetings.Commands;

// ── ScheduleMeetingCommand ────────────────────────────────────────────────────

public record ScheduleMeetingCommand(
    string Title,
    Guid? MeetingTypeId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? EndsAt,
    string? Location,
    Guid? RoomId,
    Guid? CampusId,
    Guid? OrganizerId,
    IReadOnlyList<ParticipantInput>? Participants) : IRequest<Guid>;

public record ParticipantInput(
    string ParticipantType,
    Guid? UserId,
    Guid? GuardianId,
    Guid? LeadId,
    string? ExternalName,
    string Attendance = "invited");

public class ScheduleMeetingCommandValidator : AbstractValidator<ScheduleMeetingCommand>
{
    private static readonly string[] ValidTypes =
        ["user", "guardian", "lead", "external"];
    private static readonly string[] ValidAttendances =
        ["invited", "attended", "absent", "tentative"];

    public ScheduleMeetingCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EndsAt)
            .Must((cmd, endsAt) => endsAt == null || cmd.ScheduledAt == null || endsAt > cmd.ScheduledAt)
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("EndsAt"), Msg.FieldArg("ScheduledAt"));
        RuleForEach(x => x.Participants).ChildRules(p =>
        {
            p.RuleFor(x => x.ParticipantType)
                .Must(t => ValidTypes.Contains(t))
                .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("ParticipantType"), EnumSets.ParticipantType);
            p.RuleFor(x => x.Attendance)
                .Must(a => ValidAttendances.Contains(a))
                .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Attendance"), EnumSets.MeetingAttendance);
        });
    }
}

public sealed class ScheduleMeetingCommandHandler : IRequestHandler<ScheduleMeetingCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public ScheduleMeetingCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(ScheduleMeetingCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var meeting = Meeting.Create(
            corporationId, req.Title, req.MeetingTypeId,
            req.ScheduledAt, req.EndsAt, req.Location,
            req.RoomId, req.CampusId, req.OrganizerId, _currentUser.UserId);

        _db.Meetings.Add(meeting);

        if (req.Participants is { Count: > 0 })
        {
            foreach (var p in req.Participants)
            {
                _db.MeetingParticipants.Add(MeetingParticipant.Create(
                    corporationId, meeting.Id,
                    p.ParticipantType, p.UserId, p.GuardianId,
                    p.LeadId, p.ExternalName, p.Attendance));
            }
        }

        await _db.SaveChangesAsync(ct);
        return meeting.Id;
    }
}

// ── UpdateMeetingCommand ──────────────────────────────────────────────────────

public record UpdateMeetingCommand(
    Guid Id,
    string Title,
    Guid? MeetingTypeId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? EndsAt,
    string? Location,
    Guid? RoomId,
    Guid? CampusId,
    Guid? OrganizerId,
    int RowVersion) : IRequest;

public class UpdateMeetingCommandValidator : AbstractValidator<UpdateMeetingCommand>
{
    public UpdateMeetingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EndsAt)
            .Must((cmd, endsAt) => endsAt == null || cmd.ScheduledAt == null || endsAt > cmd.ScheduledAt)
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("EndsAt"), Msg.FieldArg("ScheduledAt"));
    }
}

public sealed class UpdateMeetingCommandHandler : IRequestHandler<UpdateMeetingCommand>
{
    private readonly IAppDbContext _db;

    public UpdateMeetingCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateMeetingCommand req, CancellationToken ct)
    {
        var meeting = await _db.Meetings
            .FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Meeting {req.Id} not found.");

        meeting.Update(req.Title, req.MeetingTypeId, req.ScheduledAt, req.EndsAt,
            req.Location, req.RoomId, req.CampusId, req.OrganizerId);

        await _db.SaveChangesAsync(ct);
    }
}

// ── CancelMeetingCommand ──────────────────────────────────────────────────────

public record CancelMeetingCommand(Guid Id) : IRequest;

public sealed class CancelMeetingCommandHandler : IRequestHandler<CancelMeetingCommand>
{
    private readonly IAppDbContext _db;

    public CancelMeetingCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelMeetingCommand req, CancellationToken ct)
    {
        var meeting = await _db.Meetings
            .FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Meeting {req.Id} not found.");

        meeting.Cancel();
        await _db.SaveChangesAsync(ct);
    }
}

// ── CompleteMeetingCommand ────────────────────────────────────────────────────

public record CompleteMeetingCommand(Guid Id) : IRequest;

public sealed class CompleteMeetingCommandHandler : IRequestHandler<CompleteMeetingCommand>
{
    private readonly IAppDbContext _db;

    public CompleteMeetingCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CompleteMeetingCommand req, CancellationToken ct)
    {
        var meeting = await _db.Meetings
            .FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Meeting {req.Id} not found.");

        meeting.Complete();
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteMeetingCommand ──────────────────────────────────────────────────────

public record DeleteMeetingCommand(Guid Id) : IRequest;

public sealed class DeleteMeetingCommandHandler : IRequestHandler<DeleteMeetingCommand>
{
    private readonly IAppDbContext _db;

    public DeleteMeetingCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteMeetingCommand req, CancellationToken ct)
    {
        var meeting = await _db.Meetings
            .FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Meeting {req.Id} not found.");

        meeting.SoftDelete();
        await _db.SaveChangesAsync(ct);
    }
}

// ── AddMeetingParticipantCommand ──────────────────────────────────────────────

/// <summary>CorporationId is taken from the request context (JWT), never from the client.</summary>
public record AddMeetingParticipantCommand(
    Guid MeetingId,
    string ParticipantType,
    Guid? UserId,
    Guid? GuardianId,
    Guid? LeadId,
    string? ExternalName,
    string Attendance = "invited") : IRequest<MeetingParticipantDto>;

public class AddMeetingParticipantCommandValidator
    : AbstractValidator<AddMeetingParticipantCommand>
{
    private static readonly string[] ValidTypes =
        ["user", "guardian", "lead", "external"];
    private static readonly string[] ValidAttendances =
        ["invited", "attended", "absent", "tentative"];

    public AddMeetingParticipantCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEmpty();
        RuleFor(x => x.ParticipantType)
            .Must(t => ValidTypes.Contains(t))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("ParticipantType"), EnumSets.ParticipantType);
        RuleFor(x => x.Attendance)
            .Must(a => ValidAttendances.Contains(a))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Attendance"), EnumSets.MeetingAttendance);
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || x.GuardianId.HasValue
                       || x.LeadId.HasValue || !string.IsNullOrWhiteSpace(x.ExternalName))
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("UserId", "GuardianId", "LeadId", "ExternalName"));
    }
}

public sealed class AddMeetingParticipantCommandHandler
    : IRequestHandler<AddMeetingParticipantCommand, MeetingParticipantDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AddMeetingParticipantCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MeetingParticipantDto> Handle(
        AddMeetingParticipantCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var meetingExists = await _db.Meetings
            .AnyAsync(m => m.Id == req.MeetingId && m.CorporationId == corporationId, ct);

        if (!meetingExists)
            throw new KeyNotFoundException($"Meeting {req.MeetingId} not found.");

        var participant = MeetingParticipant.Create(
            corporationId, req.MeetingId, req.ParticipantType,
            req.UserId, req.GuardianId, req.LeadId,
            req.ExternalName, req.Attendance);

        _db.MeetingParticipants.Add(participant);
        await _db.SaveChangesAsync(ct);

        return new MeetingParticipantDto(
            participant.Id, participant.MeetingId, participant.CorporationId,
            participant.ParticipantType, participant.UserId, participant.GuardianId,
            participant.LeadId, participant.ExternalName, participant.Attendance);
    }
}

// ── UpdateAttendanceCommand ───────────────────────────────────────────────────

public record UpdateAttendanceCommand(Guid ParticipantId, string Attendance) : IRequest;

public class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    private static readonly string[] ValidAttendances =
        ["invited", "attended", "absent", "tentative"];

    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.Attendance)
            .Must(a => ValidAttendances.Contains(a))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Attendance"), EnumSets.MeetingAttendance);
    }
}

public sealed class UpdateAttendanceCommandHandler : IRequestHandler<UpdateAttendanceCommand>
{
    private readonly IAppDbContext _db;

    public UpdateAttendanceCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateAttendanceCommand req, CancellationToken ct)
    {
        var participant = await _db.MeetingParticipants
            .FirstOrDefaultAsync(p => p.Id == req.ParticipantId, ct)
            ?? throw new KeyNotFoundException($"MeetingParticipant {req.ParticipantId} not found.");

        participant.UpdateAttendance(req.Attendance);
        await _db.SaveChangesAsync(ct);
    }
}

// ── RemoveMeetingParticipantCommand ───────────────────────────────────────────

public record RemoveMeetingParticipantCommand(Guid ParticipantId) : IRequest;

public sealed class RemoveMeetingParticipantCommandHandler
    : IRequestHandler<RemoveMeetingParticipantCommand>
{
    private readonly IAppDbContext _db;

    public RemoveMeetingParticipantCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveMeetingParticipantCommand req, CancellationToken ct)
    {
        var participant = await _db.MeetingParticipants
            .FirstOrDefaultAsync(p => p.Id == req.ParticipantId, ct)
            ?? throw new KeyNotFoundException($"MeetingParticipant {req.ParticipantId} not found.");

        _db.MeetingParticipants.Remove(participant);
        await _db.SaveChangesAsync(ct);
    }
}

// ── RecordMeetingOutcomeCommand ───────────────────────────────────────────────

/// <summary>
/// CorporationId and CreatedBy are taken from the request context (JWT), never from the client.
/// </summary>
public record RecordMeetingOutcomeCommand(
    Guid MeetingId,
    string? Summary,
    string? Decisions) : IRequest<MeetingOutcomeDto>;

public class RecordMeetingOutcomeCommandValidator
    : AbstractValidator<RecordMeetingOutcomeCommand>
{
    public RecordMeetingOutcomeCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEmpty();
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.Decisions))
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("Summary", "Decisions"));
    }
}

public sealed class RecordMeetingOutcomeCommandHandler
    : IRequestHandler<RecordMeetingOutcomeCommand, MeetingOutcomeDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public RecordMeetingOutcomeCommandHandler(
        IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<MeetingOutcomeDto> Handle(
        RecordMeetingOutcomeCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var meetingExists = await _db.Meetings
            .AnyAsync(m => m.Id == req.MeetingId && m.CorporationId == corporationId, ct);

        if (!meetingExists)
            throw new KeyNotFoundException($"Meeting {req.MeetingId} not found.");

        var outcome = MeetingOutcome.Create(
            corporationId, req.MeetingId,
            req.Summary, req.Decisions, _currentUser.UserId);

        _db.MeetingOutcomes.Add(outcome);
        await _db.SaveChangesAsync(ct);

        return new MeetingOutcomeDto(
            outcome.Id, outcome.MeetingId,
            outcome.Summary, outcome.Decisions,
            outcome.CreatedAt, outcome.CreatedBy);
    }
}

// ── UpdateMeetingOutcomeCommand ───────────────────────────────────────────────

public record UpdateMeetingOutcomeCommand(
    Guid Id,
    string? Summary,
    string? Decisions) : IRequest;

public class UpdateMeetingOutcomeCommandValidator
    : AbstractValidator<UpdateMeetingOutcomeCommand>
{
    public UpdateMeetingOutcomeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.Decisions))
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("Summary", "Decisions"));
    }
}

public sealed class UpdateMeetingOutcomeCommandHandler
    : IRequestHandler<UpdateMeetingOutcomeCommand>
{
    private readonly IAppDbContext _db;

    public UpdateMeetingOutcomeCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateMeetingOutcomeCommand req, CancellationToken ct)
    {
        var outcome = await _db.MeetingOutcomes
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MeetingOutcome {req.Id} not found.");

        outcome.Update(req.Summary, req.Decisions);
        await _db.SaveChangesAsync(ct);
    }
}

// ── AddMeetingFollowUpCommand ─────────────────────────────────────────────────

/// <summary>
/// CorporationId is taken from the request context (JWT). AssigneeId is the responsible
/// person chosen by the client (not the actor) and stays in the payload.
/// </summary>
public record AddMeetingFollowUpCommand(
    Guid MeetingId,
    string Action,
    Guid? AssigneeId,
    DateOnly? DueDate) : IRequest<MeetingFollowUpDto>;

public class AddMeetingFollowUpCommandValidator
    : AbstractValidator<AddMeetingFollowUpCommand>
{
    public AddMeetingFollowUpCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEmpty();
        RuleFor(x => x.Action).NotEmpty().MaximumLength(1000);
    }
}

public sealed class AddMeetingFollowUpCommandHandler
    : IRequestHandler<AddMeetingFollowUpCommand, MeetingFollowUpDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AddMeetingFollowUpCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MeetingFollowUpDto> Handle(
        AddMeetingFollowUpCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var meetingExists = await _db.Meetings
            .AnyAsync(m => m.Id == req.MeetingId && m.CorporationId == corporationId, ct);

        if (!meetingExists)
            throw new KeyNotFoundException($"Meeting {req.MeetingId} not found.");

        var followUp = MeetingFollowUp.Create(
            corporationId, req.MeetingId,
            req.Action, req.AssigneeId, req.DueDate);

        _db.MeetingFollowUps.Add(followUp);
        await _db.SaveChangesAsync(ct);

        return new MeetingFollowUpDto(
            followUp.Id, followUp.MeetingId, followUp.Action,
            followUp.AssigneeId, followUp.DueDate, followUp.Status, followUp.CreatedAt);
    }
}

// ── UpdateMeetingFollowUpCommand ──────────────────────────────────────────────

public record UpdateMeetingFollowUpCommand(
    Guid Id,
    string Action,
    Guid? AssigneeId,
    DateOnly? DueDate) : IRequest;

public class UpdateMeetingFollowUpCommandValidator
    : AbstractValidator<UpdateMeetingFollowUpCommand>
{
    public UpdateMeetingFollowUpCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Action).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UpdateMeetingFollowUpCommandHandler
    : IRequestHandler<UpdateMeetingFollowUpCommand>
{
    private readonly IAppDbContext _db;

    public UpdateMeetingFollowUpCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateMeetingFollowUpCommand req, CancellationToken ct)
    {
        var followUp = await _db.MeetingFollowUps
            .FirstOrDefaultAsync(f => f.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MeetingFollowUp {req.Id} not found.");

        followUp.Update(req.Action, req.AssigneeId, req.DueDate);
        await _db.SaveChangesAsync(ct);
    }
}

// ── UpdateFollowUpStatusCommand ───────────────────────────────────────────────

public record UpdateFollowUpStatusCommand(Guid Id, string Status) : IRequest;

public class UpdateFollowUpStatusCommandValidator
    : AbstractValidator<UpdateFollowUpStatusCommand>
{
    private static readonly string[] ValidStatuses =
        ["open", "in_progress", "done", "cancelled"];

    public UpdateFollowUpStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Status"), EnumSets.ActionItemStatus);
    }
}

public sealed class UpdateFollowUpStatusCommandHandler
    : IRequestHandler<UpdateFollowUpStatusCommand>
{
    private readonly IAppDbContext _db;

    public UpdateFollowUpStatusCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateFollowUpStatusCommand req, CancellationToken ct)
    {
        var followUp = await _db.MeetingFollowUps
            .FirstOrDefaultAsync(f => f.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MeetingFollowUp {req.Id} not found.");

        followUp.UpdateStatus(req.Status);
        await _db.SaveChangesAsync(ct);
    }
}
