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

// ── CreateCampPeriodCommand ───────────────────────────────────────────────────

/// <summary>CorporationId is taken from the request context (JWT), never from the client.</summary>
public record CreateCampPeriodCommand(
    Guid CampId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity) : IRequest<CampPeriodDto>;

public class CreateCampPeriodCommandValidator : AbstractValidator<CreateCampPeriodCommand>
{
    public CreateCampPeriodCommandValidator()
    {
        RuleFor(x => x.CampId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate)
            .Must((cmd, end) => end >= cmd.StartDate)
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("EndDate"), Msg.FieldArg("StartDate"));
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateCampPeriodCommandHandler
    : IRequestHandler<CreateCampPeriodCommand, CampPeriodDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateCampPeriodCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampPeriodDto> Handle(CreateCampPeriodCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var campExists = await _db.Camps
            .AnyAsync(c => c.Id == req.CampId
                        && c.CorporationId == corporationId
                        && c.DeletedAt == null, ct);

        if (!campExists)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Camp"));

        var period = CampPeriod.Create(
            corporationId, req.CampId,
            req.Name, req.StartDate, req.EndDate, req.Capacity);

        _db.CampPeriods.Add(period);
        await _db.SaveChangesAsync(ct);

        return new CampPeriodDto(
            period.Id, period.CampId, period.CorporationId,
            period.Name, period.StartDate, period.EndDate,
            period.Capacity, 0, 0);
    }
}

// ── UpdateCampPeriodCommand ───────────────────────────────────────────────────

public record UpdateCampPeriodCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity) : IRequest;

public class UpdateCampPeriodCommandValidator : AbstractValidator<UpdateCampPeriodCommand>
{
    public UpdateCampPeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate)
            .Must((cmd, end) => end >= cmd.StartDate)
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("EndDate"), Msg.FieldArg("StartDate"));
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class UpdateCampPeriodCommandHandler : IRequestHandler<UpdateCampPeriodCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCampPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCampPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.CampPeriods
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampPeriod"));

        period.Update(req.Name, req.StartDate, req.EndDate, req.Capacity);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCampPeriodCommand ───────────────────────────────────────────────────

public record DeleteCampPeriodCommand(Guid Id) : IRequest;

public sealed class DeleteCampPeriodCommandHandler : IRequestHandler<DeleteCampPeriodCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCampPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCampPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.CampPeriods
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampPeriod"));

        var hasEnrollments = await _db.CampEnrollments
            .AnyAsync(e => e.CampPeriodId == req.Id, ct);

        if (hasEnrollments)
            throw new BusinessRuleViolationException(MsgKeys.Rule.CampPeriodHasEnrollments);

        _db.CampPeriods.Remove(period);
        await _db.SaveChangesAsync(ct);
    }
}
