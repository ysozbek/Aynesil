using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Users.Commands;

// ── Assign Role ───────────────────────────────────────────────────────────────

public record AssignUserRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid? CampusId = null,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null) : IRequest<UserRoleDto>;

public class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithMessage("ValidTo must be after ValidFrom.");
    }
}

public sealed class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, UserRoleDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public AssignUserRoleCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<UserRoleDto> Handle(AssignUserRoleCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var userExists = await _db.UserAccounts.AnyAsync(u => u.Id == req.UserId, ct);
        if (!userExists) throw new NotFoundException("User", req.UserId);

        // Verify the role belongs to this tenant or is a system role
        var roleExists = await _db.Roles.AnyAsync(
            r => r.Id == req.RoleId && (r.CorporationId == null || r.CorporationId == corporationId), ct);
        if (!roleExists) throw new NotFoundException("Role", req.RoleId);

        // Prevent duplicate grant for the same user/role/campus combination
        var duplicate = await _db.UserRoles.AnyAsync(
            ur => ur.UserId == req.UserId && ur.RoleId == req.RoleId && ur.CampusId == req.CampusId, ct);
        if (duplicate)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RoleId), "This role is already assigned to the user for the specified scope.")]);

        var grant = new UserRole
        {
            CorporationId = corporationId,
            UserId = req.UserId,
            RoleId = req.RoleId,
            CampusId = req.CampusId,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            CreatedBy = _currentUser.UserId
        };

        _db.UserRoles.Add(grant);
        await _db.SaveChangesAsync(ct);

        // Reload with Role navigation for DTO mapping
        await _db.UserRoles.Entry(grant).Reference(g => g.Role).LoadAsync(ct);

        return grant.ToDto();
    }
}

// ── Remove Role ───────────────────────────────────────────────────────────────

public record RemoveUserRoleCommand(Guid UserRoleId) : IRequest;

public class RemoveUserRoleCommandValidator : AbstractValidator<RemoveUserRoleCommand>
{
    public RemoveUserRoleCommandValidator() => RuleFor(x => x.UserRoleId).NotEmpty();
}

public sealed class RemoveUserRoleCommandHandler : IRequestHandler<RemoveUserRoleCommand>
{
    private readonly IAppDbContext _db;

    public RemoveUserRoleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveUserRoleCommand req, CancellationToken ct)
    {
        var grant = await _db.UserRoles.FindAsync([req.UserRoleId], ct)
            ?? throw new NotFoundException("UserRole", req.UserRoleId);

        _db.UserRoles.Remove(grant);
        await _db.SaveChangesAsync(ct);
    }
}
