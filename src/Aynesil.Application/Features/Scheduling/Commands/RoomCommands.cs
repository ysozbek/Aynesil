using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── CreateRoomCommand ─────────────────────────────────────────────────────────

public record CreateRoomCommand(
    Guid CorporationId,
    string Code,
    string Name,
    int Capacity,
    bool IsVirtual,
    Guid? CampusId,
    Guid? RoomTypeId,
    string? MeetingUrl) : IRequest<RoomDto>;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MeetingUrl).MaximumLength(2000).When(x => x.MeetingUrl != null);
        RuleFor(x => x.CampusId).Null()
            .When(x => x.IsVirtual)
            .WithMessage("Virtual rooms must not be assigned to a campus.");
    }
}

public sealed class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, RoomDto>
{
    private readonly IAppDbContext _db;

    public CreateRoomCommandHandler(IAppDbContext db) => _db = db;

    public async Task<RoomDto> Handle(CreateRoomCommand req, CancellationToken ct)
    {
        var duplicate = await _db.Rooms.AnyAsync(
            r => r.CorporationId == req.CorporationId
              && r.CampusId == req.CampusId
              && r.Code == req.Code, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"Room with code '{req.Code}' already exists in this campus.");

        var room = Room.Create(
            req.CorporationId, req.Code, req.Name, req.Capacity,
            req.IsVirtual, req.CampusId, req.RoomTypeId, req.MeetingUrl);

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);

        return SchedulingProjection.ToRoomDto(room);
    }
}

// ── UpdateRoomCommand ─────────────────────────────────────────────────────────

public record UpdateRoomCommand(
    Guid Id,
    string Code,
    string Name,
    int Capacity,
    Guid? RoomTypeId,
    string? MeetingUrl,
    int RowVersion) : IRequest<RoomDto>;

public class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MeetingUrl).MaximumLength(2000).When(x => x.MeetingUrl != null);
    }
}

public sealed class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand, RoomDto>
{
    private readonly IAppDbContext _db;

    public UpdateRoomCommandHandler(IAppDbContext db) => _db = db;

    public async Task<RoomDto> Handle(UpdateRoomCommand req, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Room {req.Id} not found.");

        if (room.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Room was modified by another user. Please refresh and retry.");

        room.UpdateDetails(req.Code, req.Name, req.Capacity, req.RoomTypeId, req.MeetingUrl);

        await _db.SaveChangesAsync(ct);
        return SchedulingProjection.ToRoomDto(room);
    }
}

// ── DeactivateRoomCommand ─────────────────────────────────────────────────────

public record DeactivateRoomCommand(Guid Id) : IRequest;

public sealed class DeactivateRoomCommandHandler : IRequestHandler<DeactivateRoomCommand>
{
    private readonly IAppDbContext _db;

    public DeactivateRoomCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeactivateRoomCommand req, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Room {req.Id} not found.");

        room.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteRoomCommand ─────────────────────────────────────────────────────────

public record DeleteRoomCommand(Guid Id) : IRequest;

public sealed class DeleteRoomCommandHandler : IRequestHandler<DeleteRoomCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteRoomCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteRoomCommand req, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Room {req.Id} not found.");

        var hasFutureSessions = await _db.Sessions.AnyAsync(
            s => s.RoomId == req.Id
              && s.Status == "scheduled"
              && s.StartsAt > DateTimeOffset.UtcNow, ct);

        if (hasFutureSessions)
            throw new InvalidOperationException(
                "Cannot delete a room that has upcoming scheduled sessions. Deactivate it instead.");

        room.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
