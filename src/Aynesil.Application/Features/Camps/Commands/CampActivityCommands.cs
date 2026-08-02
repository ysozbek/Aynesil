using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── CreateCampActivityCommand ─────────────────────────────────────────────────

public record CreateCampActivityCommand(
    Guid CorporationId,
    Guid CampPeriodId,
    string Name,
    Guid? ActivityTypeId,
    string? Description,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location,
    int? Capacity,
    Guid? SessionId = null,
    Guid? CreatedBy = null) : IRequest<CampActivityDto>;

public class CreateCampActivityCommandValidator : AbstractValidator<CreateCampActivityCommand>
{
    public CreateCampActivityCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampPeriodId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt)
            .Must((cmd, ends) => ends == null || cmd.StartsAt == null || ends > cmd.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateCampActivityCommandHandler
    : IRequestHandler<CreateCampActivityCommand, CampActivityDto>
{
    private readonly IAppDbContext _db;

    public CreateCampActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampActivityDto> Handle(CreateCampActivityCommand req, CancellationToken ct)
    {
        var periodExists = await _db.CampPeriods
            .AnyAsync(p => p.Id == req.CampPeriodId
                        && p.CorporationId == req.CorporationId, ct);
        if (!periodExists)
            throw new KeyNotFoundException($"Camp period {req.CampPeriodId} not found.");

        var activity = CampActivity.Create(
            req.CorporationId, req.CampPeriodId, req.Name,
            req.ActivityTypeId, req.Description,
            req.StartsAt, req.EndsAt, req.Location,
            req.Capacity, req.SessionId, req.CreatedBy);

        _db.CampActivities.Add(activity);
        await _db.SaveChangesAsync(ct);

        var typeCode = req.ActivityTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.ActivityTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return ToDto(activity, typeCode);
    }

    internal static CampActivityDto ToDto(CampActivity a, string? typeCode) =>
        new(a.Id, a.CorporationId, a.CampPeriodId,
            a.ActivityTypeId, typeCode,
            a.Name, a.Description, a.StartsAt, a.EndsAt,
            a.Location, a.Capacity, a.SessionId, a.IsActive,
            a.CreatedAt, a.UpdatedAt, a.RowVersion);
}

// ── UpdateCampActivityCommand ─────────────────────────────────────────────────

public record UpdateCampActivityCommand(
    Guid Id,
    string Name,
    Guid? ActivityTypeId,
    string? Description,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location,
    int? Capacity,
    Guid? SessionId,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateCampActivityCommandValidator : AbstractValidator<UpdateCampActivityCommand>
{
    public UpdateCampActivityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt)
            .Must((cmd, ends) => ends == null || cmd.StartsAt == null || ends > cmd.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateCampActivityCommandHandler : IRequestHandler<UpdateCampActivityCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCampActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCampActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.CampActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp activity {req.Id} not found.");

        activity.Update(req.Name, req.ActivityTypeId, req.Description,
            req.StartsAt, req.EndsAt, req.Location, req.Capacity,
            req.SessionId, req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCampActivityCommand ─────────────────────────────────────────────────

public record DeleteCampActivityCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteCampActivityCommandHandler : IRequestHandler<DeleteCampActivityCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCampActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCampActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.CampActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp activity {req.Id} not found.");

        activity.SoftDelete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
