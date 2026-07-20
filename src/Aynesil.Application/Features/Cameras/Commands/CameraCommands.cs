using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Media.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Cameras.Commands;

// ── RegisterCameraCommand ─────────────────────────────────────────────────────

public record RegisterCameraCommand(
    Guid CorporationId,
    string Code,
    string Name,
    Guid? CampusId,
    Guid? CameraTypeId,
    Guid? StreamProviderId,
    string? StreamRef,
    Guid? CreatedBy = null) : IRequest<Guid>;

public class RegisterCameraCommandValidator : AbstractValidator<RegisterCameraCommand>
{
    public RegisterCameraCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StreamRef).MaximumLength(500).When(x => x.StreamRef != null);
    }
}

public sealed class RegisterCameraCommandHandler : IRequestHandler<RegisterCameraCommand, Guid>
{
    private readonly IAppDbContext _db;

    public RegisterCameraCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(RegisterCameraCommand req, CancellationToken ct)
    {
        var duplicate = await _db.Cameras
            .AnyAsync(c => c.CorporationId == req.CorporationId && c.Code == req.Code, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"A camera with code '{req.Code}' already exists in this corporation.");

        var camera = Camera.Register(
            req.CorporationId, req.Code, req.Name,
            req.CampusId, req.CameraTypeId,
            req.StreamProviderId, req.StreamRef,
            req.CreatedBy);

        _db.Cameras.Add(camera);
        await _db.SaveChangesAsync(ct);
        return camera.Id;
    }
}

// ── UpdateCameraCommand ───────────────────────────────────────────────────────

public record UpdateCameraCommand(
    Guid Id,
    string Name,
    Guid? CampusId,
    Guid? CameraTypeId,
    Guid? StreamProviderId,
    string? StreamRef,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateCameraCommandValidator : AbstractValidator<UpdateCameraCommand>
{
    public UpdateCameraCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StreamRef).MaximumLength(500).When(x => x.StreamRef != null);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateCameraCommandHandler : IRequestHandler<UpdateCameraCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCameraCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCameraCommand req, CancellationToken ct)
    {
        var camera = await _db.Cameras
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        camera.Update(req.Name, req.CampusId, req.CameraTypeId,
            req.StreamProviderId, req.StreamRef, req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCameraCommand ───────────────────────────────────────────────────────

public record DeleteCameraCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteCameraCommandHandler : IRequestHandler<DeleteCameraCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCameraCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCameraCommand req, CancellationToken ct)
    {
        var camera = await _db.Cameras
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        camera.SoftDelete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SetCameraActiveCommand ────────────────────────────────────────────────────

public record SetCameraActiveCommand(Guid Id, bool IsActive, Guid? UpdatedBy = null) : IRequest;

public class SetCameraActiveCommandValidator : AbstractValidator<SetCameraActiveCommand>
{
    public SetCameraActiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class SetCameraActiveCommandHandler : IRequestHandler<SetCameraActiveCommand>
{
    private readonly IAppDbContext _db;

    public SetCameraActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SetCameraActiveCommand req, CancellationToken ct)
    {
        var camera = await _db.Cameras
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        if (req.IsActive)
            camera.Activate(req.UpdatedBy);
        else
            camera.Deactivate(req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}
