using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── CreateCalendarEntryCommand ────────────────────────────────────────────────

public record CreateCalendarEntryCommand(
    string Title,
    string EntryType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAllDay,
    Guid? CampusId,
    bool CancelPlannedSessions = false) : IRequest<CalendarEntryDto>;

/// <summary>Bir takvim kaydının kapsadığı aralıkta henüz yapılmamış (planlı / ertelenmiş) seanslar.</summary>
internal static class ClosureImpact
{
    public static bool Blocks(string entryType) => entryType is "holiday" or "closure" or "term_break";

    public static IQueryable<Session> PlannedSessions(
        IAppDbContext db, DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? campusId)
    {
        var q = db.Sessions.Where(s => (s.Status == "scheduled" || s.Status == "rescheduled")
                                    && s.StartsAt < endsAt && s.EndsAt > startsAt);
        if (campusId.HasValue) q = q.Where(s => s.CampusId == campusId.Value);
        return q;
    }
}

// ── GetCalendarEntryImpactQuery ───────────────────────────────────────────────

/// <summary>Kayıt eklenmeden önce: o aralıktaki planlı seans sayısı.</summary>
public record GetCalendarEntryImpactQuery(
    DateTimeOffset From, DateTimeOffset To, Guid? CampusId) : IRequest<CalendarEntryImpactDto>;

public record CalendarEntryImpactDto(int PlannedSessionCount);

public sealed class GetCalendarEntryImpactQueryHandler
    : IRequestHandler<GetCalendarEntryImpactQuery, CalendarEntryImpactDto>
{
    private readonly IAppDbContext _db;
    public GetCalendarEntryImpactQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CalendarEntryImpactDto> Handle(GetCalendarEntryImpactQuery req, CancellationToken ct)
        => new(await ClosureImpact.PlannedSessions(_db, req.From, req.To, req.CampusId).CountAsync(ct));
}

public class CreateCalendarEntryCommandValidator : AbstractValidator<CreateCalendarEntryCommand>
{
    public CreateCalendarEntryCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EntryType)
            .Must(t => new[] { "holiday", "closure", "event", "term_break" }.Contains(t))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("EntryType"), EnumSets.EntryType);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("EndsAt"), Msg.FieldArg("StartsAt"));
    }
}

public sealed class CreateCalendarEntryCommandHandler
    : IRequestHandler<CreateCalendarEntryCommand, CalendarEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateCalendarEntryCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<CalendarEntryDto> Handle(CreateCalendarEntryCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var entry = CalendarEntry.Create(
            corporationId, req.Title, req.EntryType,
            req.StartsAt, req.EndsAt, req.IsAllDay, req.CampusId);

        _db.CalendarEntries.Add(entry);

        // İsteğe bağlı: aralıktaki planlı seansları iptal et (gerekçe = kaydın adı). Seçilmezse seanslar kalır.
        var cancelled = 0;
        if (req.CancelPlannedSessions && ClosureImpact.Blocks(req.EntryType))
        {
            var sessions = await ClosureImpact.PlannedSessions(_db, req.StartsAt, req.EndsAt, req.CampusId).ToListAsync(ct);
            foreach (var s in sessions) s.Cancel(req.Title, _currentUser.UserId);
            cancelled = sessions.Count;
        }

        await _db.SaveChangesAsync(ct);

        return SchedulingProjection.ToCalendarEntryDto(entry) with { CancelledSessionCount = cancelled };
    }
}

// ── DeleteCalendarEntryCommand ────────────────────────────────────────────────

public record DeleteCalendarEntryCommand(Guid Id) : IRequest;

public sealed class DeleteCalendarEntryCommandHandler : IRequestHandler<DeleteCalendarEntryCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCalendarEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCalendarEntryCommand req, CancellationToken ct)
    {
        var entry = await _db.CalendarEntries.FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CalendarEntry"));

        _db.CalendarEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);
    }
}
