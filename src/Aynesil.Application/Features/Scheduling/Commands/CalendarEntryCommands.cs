using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── CreateCalendarEntryCommand ────────────────────────────────────────────────

public record CreateCalendarEntryCommand(
    Guid CorporationId,
    string Title,
    string EntryType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAllDay,
    Guid? CampusId) : IRequest<CalendarEntryDto>;

public class CreateCalendarEntryCommandValidator : AbstractValidator<CreateCalendarEntryCommand>
{
    public CreateCalendarEntryCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EntryType)
            .Must(t => new[] { "holiday", "closure", "event", "term_break" }.Contains(t))
            .WithMessage("EntryType must be holiday, closure, event, or term_break.");
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
    }
}

public sealed class CreateCalendarEntryCommandHandler
    : IRequestHandler<CreateCalendarEntryCommand, CalendarEntryDto>
{
    private readonly IAppDbContext _db;

    public CreateCalendarEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CalendarEntryDto> Handle(CreateCalendarEntryCommand req, CancellationToken ct)
    {
        var entry = CalendarEntry.Create(
            req.CorporationId, req.Title, req.EntryType,
            req.StartsAt, req.EndsAt, req.IsAllDay, req.CampusId);

        _db.CalendarEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        return SchedulingProjection.ToCalendarEntryDto(entry);
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
            ?? throw new KeyNotFoundException($"CalendarEntry {req.Id} not found.");

        _db.CalendarEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);
    }
}
