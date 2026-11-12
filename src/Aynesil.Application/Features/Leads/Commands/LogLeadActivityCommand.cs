using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Domain.Modules.Crm.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record LogLeadActivityCommand(
    Guid LeadId,
    Guid? ActivityTypeId,
    string? Subject,
    string? Body,
    string? Direction,
    DateTimeOffset? OccurredAt,
    DateTimeOffset? FollowUpAt,
    Guid? PerformedBy) : IRequest<LeadActivityDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class LogLeadActivityCommandValidator : AbstractValidator<LogLeadActivityCommand>
{
    private static readonly string[] ValidDirections = ["inbound", "outbound"];

    public LogLeadActivityCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.Subject).MaximumLength(500).When(x => x.Subject is not null);
        RuleFor(x => x.Direction)
            .Must(d => d is null || ValidDirections.Contains(d))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Direction"), EnumSets.Direction);
        RuleFor(x => x.FollowUpAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .When(x => x.FollowUpAt.HasValue)
            .WithKey(MsgKeys.Validation.MustBeInFuture, Msg.FieldArg("FollowUpDate"));
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class LogLeadActivityCommandHandler : IRequestHandler<LogLeadActivityCommand, LeadActivityDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public LogLeadActivityCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<LeadActivityDto> Handle(LogLeadActivityCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var lead = await _db.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == req.LeadId && l.CorporationId == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Lead"));

        // PerformedBy is chosen by the client (the activity may have been done by a colleague),
        // but it must be a user of this corporation.
        await TenantUserGuard.EnsureBelongsToTenantAsync(_db, corporationId, req.PerformedBy, nameof(req.PerformedBy), ct);

        var activity = LeadActivity.Create(
            lead.CorporationId,
            req.LeadId,
            req.ActivityTypeId,
            req.Subject,
            req.Body,
            req.Direction,
            req.OccurredAt,
            req.FollowUpAt,
            req.PerformedBy ?? _currentUser.UserId);

        _db.LeadActivities.Add(activity);
        await _db.SaveChangesAsync(ct);

        var activityTypeCode = activity.ActivityTypeId.HasValue
            ? await _db.RefValues
                .Where(rv => rv.Id == activity.ActivityTypeId.Value)
                .Select(rv => rv.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        var performedByName = activity.PerformedBy.HasValue
            ? await _db.UserAccounts
                .Where(u => u.Id == activity.PerformedBy.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct)
            : null;

        var dto = activity.ToDto(activityTypeCode, performedByName);
        return (await LeadLabels.ApplyAsync(_db, [dto], _tenant.Locale, ct))[0];
    }
}
