using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Media.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Cameras.Commands;

// ── AssignCameraToRoomCommand ─────────────────────────────────────────────────

public record AssignCameraToRoomCommand(
    Guid CorporationId,
    Guid CameraId,
    Guid RoomId) : IRequest<Guid>;

public class AssignCameraToRoomCommandValidator : AbstractValidator<AssignCameraToRoomCommand>
{
    public AssignCameraToRoomCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
    }
}

public sealed class AssignCameraToRoomCommandHandler : IRequestHandler<AssignCameraToRoomCommand, Guid>
{
    private readonly IAppDbContext _db;

    public AssignCameraToRoomCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(AssignCameraToRoomCommand req, CancellationToken ct)
    {
        var cameraExists = await _db.Cameras
            .AnyAsync(c => c.Id == req.CameraId && c.CorporationId == req.CorporationId, ct);
        if (!cameraExists)
            throw new KeyNotFoundException($"Camera {req.CameraId} not found.");

        var roomExists = await _db.Rooms
            .AnyAsync(r => r.Id == req.RoomId && r.CorporationId == req.CorporationId, ct);
        if (!roomExists)
            throw new KeyNotFoundException($"Room {req.RoomId} not found.");

        var duplicate = await _db.RoomCameras
            .AnyAsync(rc => rc.RoomId == req.RoomId && rc.CameraId == req.CameraId, ct);
        if (duplicate)
            throw new InvalidOperationException("Camera is already assigned to this room.");

        var assignment = RoomCamera.Assign(req.CorporationId, req.RoomId, req.CameraId);
        _db.RoomCameras.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return assignment.Id;
    }
}

// ── UnassignCameraFromRoomCommand ─────────────────────────────────────────────

public record UnassignCameraFromRoomCommand(Guid CameraId, Guid RoomId) : IRequest;

public class UnassignCameraFromRoomCommandValidator : AbstractValidator<UnassignCameraFromRoomCommand>
{
    public UnassignCameraFromRoomCommandValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
    }
}

public sealed class UnassignCameraFromRoomCommandHandler : IRequestHandler<UnassignCameraFromRoomCommand>
{
    private readonly IAppDbContext _db;

    public UnassignCameraFromRoomCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UnassignCameraFromRoomCommand req, CancellationToken ct)
    {
        var assignment = await _db.RoomCameras
            .FirstOrDefaultAsync(rc => rc.CameraId == req.CameraId && rc.RoomId == req.RoomId, ct)
            ?? throw new KeyNotFoundException(
                $"Camera {req.CameraId} is not assigned to room {req.RoomId}.");

        _db.RoomCameras.Remove(assignment);
        await _db.SaveChangesAsync(ct);
    }
}

// ── AssignCameraToSessionCommand ──────────────────────────────────────────────

public record AssignCameraToSessionCommand(
    Guid CorporationId,
    Guid CameraId,
    Guid SessionId) : IRequest<Guid>;

public class AssignCameraToSessionCommandValidator : AbstractValidator<AssignCameraToSessionCommand>
{
    public AssignCameraToSessionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public sealed class AssignCameraToSessionCommandHandler : IRequestHandler<AssignCameraToSessionCommand, Guid>
{
    private readonly IAppDbContext _db;

    public AssignCameraToSessionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(AssignCameraToSessionCommand req, CancellationToken ct)
    {
        var cameraExists = await _db.Cameras
            .AnyAsync(c => c.Id == req.CameraId && c.CorporationId == req.CorporationId, ct);
        if (!cameraExists)
            throw new KeyNotFoundException($"Camera {req.CameraId} not found.");

        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.Id == req.SessionId && s.CorporationId == req.CorporationId, ct);
        if (!sessionExists)
            throw new KeyNotFoundException($"Session {req.SessionId} not found.");

        var duplicate = await _db.SessionCameras
            .AnyAsync(sc => sc.SessionId == req.SessionId && sc.CameraId == req.CameraId, ct);
        if (duplicate)
            throw new InvalidOperationException("Camera is already assigned to this session.");

        var assignment = SessionCamera.Assign(req.CorporationId, req.SessionId, req.CameraId);
        _db.SessionCameras.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return assignment.Id;
    }
}

// ── UnassignCameraFromSessionCommand ──────────────────────────────────────────

public record UnassignCameraFromSessionCommand(Guid CameraId, Guid SessionId) : IRequest;

public class UnassignCameraFromSessionCommandValidator : AbstractValidator<UnassignCameraFromSessionCommand>
{
    public UnassignCameraFromSessionCommandValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public sealed class UnassignCameraFromSessionCommandHandler : IRequestHandler<UnassignCameraFromSessionCommand>
{
    private readonly IAppDbContext _db;

    public UnassignCameraFromSessionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UnassignCameraFromSessionCommand req, CancellationToken ct)
    {
        var assignment = await _db.SessionCameras
            .FirstOrDefaultAsync(sc => sc.CameraId == req.CameraId && sc.SessionId == req.SessionId, ct)
            ?? throw new KeyNotFoundException(
                $"Camera {req.CameraId} is not assigned to session {req.SessionId}.");

        _db.SessionCameras.Remove(assignment);
        await _db.SaveChangesAsync(ct);
    }
}
