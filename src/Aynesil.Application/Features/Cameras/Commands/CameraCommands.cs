using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Media.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Cameras.Commands;

// ── RegisterCameraCommand ─────────────────────────────────────────────────────

public record RegisterCameraCommand(
    string Code,
    string Name,
    Guid? CampusId,
    Guid? CameraTypeId,
    Guid? StreamProviderId,
    string? StreamRef) : IRequest<Guid>;

public class RegisterCameraCommandValidator : AbstractValidator<RegisterCameraCommand>
{
    public RegisterCameraCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StreamRef).MaximumLength(500).When(x => x.StreamRef != null);
    }
}

public sealed class RegisterCameraCommandHandler : IRequestHandler<RegisterCameraCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public RegisterCameraCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(RegisterCameraCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var duplicate = await _db.Cameras
            .AnyAsync(c => c.CorporationId == corporationId && c.Code == req.Code, ct);

        if (duplicate)
            throw new BusinessRuleViolationException(
                $"A camera with code '{req.Code}' already exists in this corporation.");

        var camera = Camera.Register(
            corporationId, req.Code, req.Name,
            req.CampusId, req.CameraTypeId,
            req.StreamProviderId, req.StreamRef,
            _currentUser.UserId);

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
    int RowVersion) : IRequest;

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
    private readonly ICurrentUserService _currentUser;

    public UpdateCameraCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCameraCommand req, CancellationToken ct)
    {
        var camera = await _db.Cameras
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        camera.Update(req.Name, req.CampusId, req.CameraTypeId,
            req.StreamProviderId, req.StreamRef, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCameraCommand ───────────────────────────────────────────────────────

public record DeleteCameraCommand(Guid Id) : IRequest;

public sealed class DeleteCameraCommandHandler : IRequestHandler<DeleteCameraCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCameraCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCameraCommand req, CancellationToken ct)
    {
        var camera = await _db.Cameras
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        camera.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SetCameraActiveCommand ────────────────────────────────────────────────────

public record SetCameraActiveCommand(Guid Id, bool IsActive) : IRequest;

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
    private readonly ICurrentUserService _currentUser;

    public SetCameraActiveCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SetCameraActiveCommand req, CancellationToken ct)
    {
        var camera = await _db.Cameras
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camera {req.Id} not found.");

        if (req.IsActive)
            camera.Activate(_currentUser.UserId);
        else
            camera.Deactivate(_currentUser.UserId);

        await _db.SaveChangesAsync(ct);
    }
}
