using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Identity;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Educators.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Commands;

// Educator ↔ login account (educators.educator.user_id). ActingEducator resolves "the educator who is
// signed in" from this link, so without it a user cannot write session notes or approve plans.
// Rules: the user belongs to the educator's corporation (also a composite FK), a user backs at most one
// educator (uq_educator_user, V48), and a linked educator must be unlinked before linking another user.

/// <summary>Short educator reference shown on the user detail screen.</summary>
public sealed record LinkedEducatorDto(Guid Id, string FullName, bool IsActive);

internal static class EducatorUserLinkRules
{
    public static async Task<Educator> LoadTrackedAsync(IAppDbContext db, Guid educatorId, CancellationToken ct)
        => await db.Educators.FirstOrDefaultAsync(e => e.Id == educatorId, ct)
           ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Educator"));

    public static async Task EnsureLinkableAsync(IAppDbContext db, Educator educator, Guid userId, CancellationToken ct)
    {
        if (educator.UserId.HasValue && educator.UserId != userId)
        {
            var current = await db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == educator.UserId.Value).Select(u => u.Username).FirstOrDefaultAsync(ct);
            throw new ConflictException(MsgKeys.Rule.RecordAlreadyLinkedToUser, current ?? educator.UserId.Value.ToString());
        }

        var other = await db.Educators.AsNoTracking()
            .Where(e => e.UserId == userId && e.Id != educator.Id)
            .Select(e => new { e.FirstName, e.LastName })
            .FirstOrDefaultAsync(ct);
        if (other is not null)
        {
            var username = await db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefaultAsync(ct);
            throw new ConflictException(MsgKeys.Rule.UserAlreadyLinkedToEducator,
                username ?? userId.ToString(), $"{other.FirstName} {other.LastName}".Trim());
        }
    }
}

// ── GetEducatorUserAccountQuery ───────────────────────────────────────────────

public record GetEducatorUserAccountQuery(Guid EducatorId) : IRequest<LinkedUserAccountDto?>;

public sealed class GetEducatorUserAccountQueryHandler : IRequestHandler<GetEducatorUserAccountQuery, LinkedUserAccountDto?>
{
    private readonly IAppDbContext _db;
    public GetEducatorUserAccountQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LinkedUserAccountDto?> Handle(GetEducatorUserAccountQuery req, CancellationToken ct)
    {
        var educator = await _db.Educators.AsNoTracking()
            .Where(e => e.Id == req.EducatorId)
            .Select(e => new { e.UserId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Educator"));
        return await UserAccountLinks.LoadAsync(_db, educator.UserId, ct);
    }
}

// ── GetEducatorByUserQuery ────────────────────────────────────────────────────

public record GetEducatorByUserQuery(Guid UserId) : IRequest<LinkedEducatorDto?>;

public sealed class GetEducatorByUserQueryHandler : IRequestHandler<GetEducatorByUserQuery, LinkedEducatorDto?>
{
    private readonly IAppDbContext _db;
    public GetEducatorByUserQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LinkedEducatorDto?> Handle(GetEducatorByUserQuery req, CancellationToken ct)
        => await _db.Educators.AsNoTracking()
            .Where(e => e.UserId == req.UserId)
            .Select(e => new LinkedEducatorDto(e.Id, (e.FirstName + " " + e.LastName).Trim(), e.IsActive))
            .FirstOrDefaultAsync(ct);
}

// ── LinkEducatorUserAccountCommand ────────────────────────────────────────────

public record LinkEducatorUserAccountCommand(Guid EducatorId, Guid UserId) : IRequest<LinkedUserAccountDto>;

public class LinkEducatorUserAccountCommandValidator : AbstractValidator<LinkEducatorUserAccountCommand>
{
    public LinkEducatorUserAccountCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class LinkEducatorUserAccountCommandHandler
    : IRequestHandler<LinkEducatorUserAccountCommand, LinkedUserAccountDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LinkEducatorUserAccountCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LinkedUserAccountDto> Handle(LinkEducatorUserAccountCommand req, CancellationToken ct)
    {
        var educator = await EducatorUserLinkRules.LoadTrackedAsync(_db, req.EducatorId, ct);
        await UserAccountLinks.EnsureUserInTenantAsync(_db, educator.CorporationId, req.UserId, ct);
        await EducatorUserLinkRules.EnsureLinkableAsync(_db, educator, req.UserId, ct);

        if (educator.UserId != req.UserId)
        {
            educator.LinkUserAccount(req.UserId, _currentUser.UserId);
            await _db.SaveChangesAsync(ct);
        }

        return (await UserAccountLinks.LoadAsync(_db, req.UserId, ct))!;
    }
}

// ── UnlinkEducatorUserAccountCommand ──────────────────────────────────────────

/// <summary>Removes the link only; the user account itself (and its roles) stays.</summary>
public record UnlinkEducatorUserAccountCommand(Guid EducatorId) : IRequest;

public sealed class UnlinkEducatorUserAccountCommandHandler : IRequestHandler<UnlinkEducatorUserAccountCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UnlinkEducatorUserAccountCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UnlinkEducatorUserAccountCommand req, CancellationToken ct)
    {
        var educator = await EducatorUserLinkRules.LoadTrackedAsync(_db, req.EducatorId, ct);
        if (!educator.UserId.HasValue) return;
        educator.UnlinkUserAccount(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CreateEducatorUserAccountCommand ──────────────────────────────────────────

/// <summary>Opens a new login for the educator with the chosen role and links it; returns a one-time temporary password.</summary>
public record CreateEducatorUserAccountCommand(Guid EducatorId, NewLinkedUserAccount Account)
    : IRequest<CreatedLinkedUserAccountDto>;

public class CreateEducatorUserAccountCommandValidator : AbstractValidator<CreateEducatorUserAccountCommand>
{
    public CreateEducatorUserAccountCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.Account).NotNull().SetValidator(new NewLinkedUserAccountValidator());
    }
}

public sealed class CreateEducatorUserAccountCommandHandler
    : IRequestHandler<CreateEducatorUserAccountCommand, CreatedLinkedUserAccountDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ICurrentUserService _currentUser;

    public CreateEducatorUserAccountCommandHandler(IAppDbContext db, IPasswordService passwords, ICurrentUserService currentUser)
    {
        _db = db;
        _passwords = passwords;
        _currentUser = currentUser;
    }

    public async Task<CreatedLinkedUserAccountDto> Handle(CreateEducatorUserAccountCommand req, CancellationToken ct)
    {
        var educator = await EducatorUserLinkRules.LoadTrackedAsync(_db, req.EducatorId, ct);
        if (educator.UserId.HasValue)
        {
            var current = await _db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == educator.UserId.Value).Select(u => u.Username).FirstOrDefaultAsync(ct);
            throw new ConflictException(MsgKeys.Rule.RecordAlreadyLinkedToUser, current ?? educator.UserId.Value.ToString());
        }

        return await UserAccountLinks.CreateAndLinkAsync(
            _db, _passwords, educator.CorporationId, req.Account, _currentUser.UserId,
            requiredPermission: null,
            link: userId => educator.LinkUserAccount(userId, _currentUser.UserId),
            ct);
    }
}
