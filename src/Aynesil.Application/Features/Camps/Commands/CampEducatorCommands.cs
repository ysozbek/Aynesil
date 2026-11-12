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

// ── AssignCampEducatorCommand ─────────────────────────────────────────────────

public record AssignCampEducatorCommand(
    Guid CampId,
    Guid EducatorId,
    string Role = "lead",
    Guid? CampPeriodId = null,
    Guid? CampActivityId = null) : IRequest<CampEducatorDto>;

public class AssignCampEducatorCommandValidator : AbstractValidator<AssignCampEducatorCommand>
{
    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    public AssignCampEducatorCommandValidator()
    {
        RuleFor(x => x.CampId).NotEmpty();
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Role"), EnumSets.EducatorRole);
    }
}

public sealed class AssignCampEducatorCommandHandler
    : IRequestHandler<AssignCampEducatorCommand, CampEducatorDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public AssignCampEducatorCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampEducatorDto> Handle(AssignCampEducatorCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var campExists = await _db.Camps
            .AnyAsync(c => c.Id == req.CampId
                        && c.CorporationId == corporationId
                        && c.DeletedAt == null, ct);
        if (!campExists)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Camp"));

        if (req.CampPeriodId.HasValue)
        {
            var periodOk = await _db.CampPeriods
                .AnyAsync(p => p.CorporationId == corporationId && p.Id == req.CampPeriodId.Value && p.CampId == req.CampId, ct);
            if (!periodOk)
                throw new NotFoundException(MsgKeys.Rule.CampPeriodNotInCamp, req.CampPeriodId);
        }

        if (req.CampActivityId.HasValue)
        {
            var activityOk = await _db.CampActivities
                .AnyAsync(a => a.CorporationId == corporationId && a.Id == req.CampActivityId.Value && a.DeletedAt == null, ct);
            if (!activityOk)
                throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampActivity"));
        }

        var duplicate = await _db.CampEducators.AnyAsync(e => e.CorporationId == corporationId && e.CampId == req.CampId
            && e.EducatorId == req.EducatorId
            && e.CampPeriodId == req.CampPeriodId
            && e.CampActivityId == req.CampActivityId, ct);
        if (duplicate)
            throw new BusinessRuleViolationException(MsgKeys.Rule.EducatorAlreadyAtScope);

        var assignment = CampEducator.Assign(
            corporationId, req.CampId, req.EducatorId, req.Role,
            req.CampPeriodId, req.CampActivityId, _currentUser.UserId);

        _db.CampEducators.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return new CampEducatorDto(
            assignment.Id, assignment.CorporationId, assignment.CampId,
            assignment.CampPeriodId, assignment.CampActivityId,
            assignment.EducatorId, assignment.Role,
            assignment.AssignedAt, assignment.AssignedBy);
    }
}

// ── UpdateCampEducatorRoleCommand ─────────────────────────────────────────────

public record UpdateCampEducatorRoleCommand(Guid Id, string Role) : IRequest;

public class UpdateCampEducatorRoleCommandValidator : AbstractValidator<UpdateCampEducatorRoleCommand>
{
    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    public UpdateCampEducatorRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Role"), EnumSets.EducatorRole);
    }
}

public sealed class UpdateCampEducatorRoleCommandHandler
    : IRequestHandler<UpdateCampEducatorRoleCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCampEducatorRoleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCampEducatorRoleCommand req, CancellationToken ct)
    {
        var assignment = await _db.CampEducators
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampEducatorAssignment"));

        assignment.UpdateRole(req.Role);
        await _db.SaveChangesAsync(ct);
    }
}

// ── RemoveCampEducatorCommand ─────────────────────────────────────────────────

public record RemoveCampEducatorCommand(Guid Id) : IRequest;

public sealed class RemoveCampEducatorCommandHandler : IRequestHandler<RemoveCampEducatorCommand>
{
    private readonly IAppDbContext _db;

    public RemoveCampEducatorCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveCampEducatorCommand req, CancellationToken ct)
    {
        var assignment = await _db.CampEducators
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampEducatorAssignment"));

        _db.CampEducators.Remove(assignment);
        await _db.SaveChangesAsync(ct);
    }
}
