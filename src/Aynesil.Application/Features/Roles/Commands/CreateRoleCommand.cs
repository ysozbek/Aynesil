using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Roles.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Roles.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record CreateRoleCommand(
    string Code,
    string Name,
    string? Description = null) : IRequest<RoleListItemDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(50)
            .Matches(@"^[a-z0-9][a-z0-9_-]*$")
            .WithMessage("Code must start with a letter or digit and contain only lowercase letters, digits, hyphens, or underscores.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleListItemDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateRoleCommandHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<RoleListItemDto> Handle(CreateRoleCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var codeLower = req.Code.ToLowerInvariant();

        var taken = await _db.Roles.AnyAsync(
            r => r.CorporationId == corporationId && r.Code == codeLower, ct);
        if (taken)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.Code), $"Role code '{codeLower}' already exists in this organization.")]);

        var role = Role.Create(corporationId, codeLower, req.Name, req.Description, isSystem: false);

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        return role.ToListItemDto();
    }
}
