using Aynesil.Application.Common.Exceptions;
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

// ── CreateCampActivityCommand ─────────────────────────────────────────────────

public record CreateCampActivityCommand(
    Guid CampPeriodId,
    string Name,
    Guid? ActivityTypeId,
    string? Description,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location,
    int? Capacity,
    Guid? SessionId = null) : IRequest<CampActivityDto>;

public class CreateCampActivityCommandValidator : AbstractValidator<CreateCampActivityCommand>
{
    public CreateCampActivityCommandValidator()
    {
        RuleFor(x => x.CampPeriodId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt)
            .Must((cmd, ends) => ends == null || cmd.StartsAt == null || ends > cmd.StartsAt)
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("EndsAt"), Msg.FieldArg("StartsAt"));
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateCampActivityCommandHandler
    : IRequestHandler<CreateCampActivityCommand, CampActivityDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public CreateCampActivityCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampActivityDto> Handle(CreateCampActivityCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var periodExists = await _db.CampPeriods
            .AnyAsync(p => p.Id == req.CampPeriodId
                        && p.CorporationId == corporationId, ct);
        if (!periodExists)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampPeriod"));

        var activity = CampActivity.Create(
            corporationId, req.CampPeriodId, req.Name,
            req.ActivityTypeId, req.Description,
            req.StartsAt, req.EndsAt, req.Location,
            req.Capacity, req.SessionId, _currentUser.UserId);

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
    int RowVersion) : IRequest;

public class UpdateCampActivityCommandValidator : AbstractValidator<UpdateCampActivityCommand>
{
    public UpdateCampActivityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt)
            .Must((cmd, ends) => ends == null || cmd.StartsAt == null || ends > cmd.StartsAt)
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("EndsAt"), Msg.FieldArg("StartsAt"));
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateCampActivityCommandHandler : IRequestHandler<UpdateCampActivityCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCampActivityCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCampActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.CampActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampActivity"));

        activity.Update(req.Name, req.ActivityTypeId, req.Description,
            req.StartsAt, req.EndsAt, req.Location, req.Capacity,
            req.SessionId, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCampActivityCommand ─────────────────────────────────────────────────

public record DeleteCampActivityCommand(Guid Id) : IRequest;

public sealed class DeleteCampActivityCommandHandler : IRequestHandler<DeleteCampActivityCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCampActivityCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCampActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.CampActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampActivity"));

        activity.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
