using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Identity;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// Guardian ↔ parent-portal login (students.guardian.user_id). The portal resolves the signed-in parent's
// guardian record from this link (PortalAccessHelper); per-student visibility stays in guardian_portal_access
// ("Portal Erişimi → Erişim Aç"). Rules: same corporation (also a composite FK), a user backs at most one
// guardian (uq_guardian_user, V48), a linked guardian must be unlinked before linking another user, and a
// new parent account's role must carry portal:access.

/// <summary>Short guardian reference shown on the user detail screen.</summary>
public sealed record LinkedGuardianDto(Guid Id, string FullName);

internal static class GuardianUserLinkRules
{
    public static async Task<Guardian> LoadTrackedAsync(IAppDbContext db, Guid guardianId, CancellationToken ct)
        => await db.Guardians.FirstOrDefaultAsync(e => e.Id == guardianId, ct)
           ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Guardian"));

    public static async Task EnsureLinkableAsync(IAppDbContext db, Guardian guardian, Guid userId, CancellationToken ct)
    {
        if (guardian.UserId.HasValue && guardian.UserId != userId)
        {
            var current = await db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == guardian.UserId.Value).Select(u => u.Username).FirstOrDefaultAsync(ct);
            throw new ConflictException(MsgKeys.Rule.RecordAlreadyLinkedToUser, current ?? guardian.UserId.Value.ToString());
        }

        var other = await db.Guardians.AsNoTracking()
            .Where(e => e.UserId == userId && e.Id != guardian.Id)
            .Select(e => new { e.FirstName, e.LastName })
            .FirstOrDefaultAsync(ct);
        if (other is not null)
        {
            var username = await db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefaultAsync(ct);
            throw new ConflictException(MsgKeys.Rule.UserAlreadyLinkedToGuardian,
                username ?? userId.ToString(), $"{other.FirstName} {other.LastName}".Trim());
        }
    }
}

// ── GetGuardianUserAccountQuery ───────────────────────────────────────────────

public record GetGuardianUserAccountQuery(Guid GuardianId) : IRequest<LinkedUserAccountDto?>;

public sealed class GetGuardianUserAccountQueryHandler : IRequestHandler<GetGuardianUserAccountQuery, LinkedUserAccountDto?>
{
    private readonly IAppDbContext _db;
    public GetGuardianUserAccountQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LinkedUserAccountDto?> Handle(GetGuardianUserAccountQuery req, CancellationToken ct)
    {
        var guardian = await _db.Guardians.AsNoTracking()
            .Where(e => e.Id == req.GuardianId)
            .Select(e => new { e.UserId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Guardian"));
        return await UserAccountLinks.LoadAsync(_db, guardian.UserId, ct);
    }
}

// ── GetGuardianByUserQuery ────────────────────────────────────────────────────

public record GetGuardianByUserQuery(Guid UserId) : IRequest<LinkedGuardianDto?>;

public sealed class GetGuardianByUserQueryHandler : IRequestHandler<GetGuardianByUserQuery, LinkedGuardianDto?>
{
    private readonly IAppDbContext _db;
    public GetGuardianByUserQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LinkedGuardianDto?> Handle(GetGuardianByUserQuery req, CancellationToken ct)
        => await _db.Guardians.AsNoTracking()
            .Where(e => e.UserId == req.UserId)
            .Select(e => new LinkedGuardianDto(e.Id, (e.FirstName + " " + e.LastName).Trim()))
            .FirstOrDefaultAsync(ct);
}

// ── LinkGuardianUserAccountCommand ────────────────────────────────────────────

public record LinkGuardianUserAccountCommand(Guid GuardianId, Guid UserId) : IRequest<LinkedUserAccountDto>;

public class LinkGuardianUserAccountCommandValidator : AbstractValidator<LinkGuardianUserAccountCommand>
{
    public LinkGuardianUserAccountCommandValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class LinkGuardianUserAccountCommandHandler
    : IRequestHandler<LinkGuardianUserAccountCommand, LinkedUserAccountDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LinkGuardianUserAccountCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LinkedUserAccountDto> Handle(LinkGuardianUserAccountCommand req, CancellationToken ct)
    {
        var guardian = await GuardianUserLinkRules.LoadTrackedAsync(_db, req.GuardianId, ct);
        await UserAccountLinks.EnsureUserInTenantAsync(_db, guardian.CorporationId, req.UserId, ct);
        await GuardianUserLinkRules.EnsureLinkableAsync(_db, guardian, req.UserId, ct);

        if (guardian.UserId != req.UserId)
        {
            guardian.LinkPortalAccount(req.UserId, _currentUser.UserId);
            await _db.SaveChangesAsync(ct);
        }

        return (await UserAccountLinks.LoadAsync(_db, req.UserId, ct))!;
    }
}

// ── UnlinkGuardianUserAccountCommand ──────────────────────────────────────────

/// <summary>Removes the link only; the user account itself (and its roles) stays.</summary>
public record UnlinkGuardianUserAccountCommand(Guid GuardianId) : IRequest;

public sealed class UnlinkGuardianUserAccountCommandHandler : IRequestHandler<UnlinkGuardianUserAccountCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UnlinkGuardianUserAccountCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UnlinkGuardianUserAccountCommand req, CancellationToken ct)
    {
        var guardian = await GuardianUserLinkRules.LoadTrackedAsync(_db, req.GuardianId, ct);
        if (!guardian.UserId.HasValue) return;
        guardian.UnlinkPortalAccount(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CreateGuardianUserAccountCommand ──────────────────────────────────────────

/// <summary>Opens a parent-portal login with the chosen role (must carry portal:access) and links it; returns a one-time temporary password.</summary>
public record CreateGuardianUserAccountCommand(Guid GuardianId, NewLinkedUserAccount Account)
    : IRequest<CreatedLinkedUserAccountDto>;

public class CreateGuardianUserAccountCommandValidator : AbstractValidator<CreateGuardianUserAccountCommand>
{
    public CreateGuardianUserAccountCommandValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty();
        RuleFor(x => x.Account).NotNull().SetValidator(new NewLinkedUserAccountValidator());
    }
}

public sealed class CreateGuardianUserAccountCommandHandler
    : IRequestHandler<CreateGuardianUserAccountCommand, CreatedLinkedUserAccountDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ICurrentUserService _currentUser;

    public CreateGuardianUserAccountCommandHandler(IAppDbContext db, IPasswordService passwords, ICurrentUserService currentUser)
    {
        _db = db;
        _passwords = passwords;
        _currentUser = currentUser;
    }

    public async Task<CreatedLinkedUserAccountDto> Handle(CreateGuardianUserAccountCommand req, CancellationToken ct)
    {
        var guardian = await GuardianUserLinkRules.LoadTrackedAsync(_db, req.GuardianId, ct);
        if (guardian.UserId.HasValue)
        {
            var current = await _db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == guardian.UserId.Value).Select(u => u.Username).FirstOrDefaultAsync(ct);
            throw new ConflictException(MsgKeys.Rule.RecordAlreadyLinkedToUser, current ?? guardian.UserId.Value.ToString());
        }

        return await UserAccountLinks.CreateAndLinkAsync(
            _db, _passwords, guardian.CorporationId, req.Account, _currentUser.UserId,
            requiredPermission: Aynesil.Shared.Constants.Permissions.Portal.Access,
            link: userId => guardian.LinkPortalAccount(userId, _currentUser.UserId),
            ct);
    }
}
