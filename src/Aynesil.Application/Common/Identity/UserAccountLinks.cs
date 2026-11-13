using System.Security.Cryptography;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Features.Users;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Common.Identity;

/// <summary>Login account linked to a Layer-2 person record (educator, guardian).</summary>
public sealed record LinkedUserAccountDto(
    Guid Id,
    string Username,
    string FullName,
    string? Email,
    string Status,
    DateTimeOffset? LastLoginAt);

/// <summary>
/// Result of "create a user and link it". <see cref="TemporaryPassword"/> is returned exactly once,
/// to the admin who created the account; it is never stored in plain text nor logged.
/// </summary>
public sealed record CreatedLinkedUserAccountDto(LinkedUserAccountDto User, string TemporaryPassword);

/// <summary>Data for a new account opened from a person record. The role is mandatory.</summary>
public sealed record NewLinkedUserAccount(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    Guid RoleId,
    Guid? CampusId);

/// <summary>Same field rules as <c>CreateUserCommandValidator</c>; the password is generated, never supplied.</summary>
public sealed class NewLinkedUserAccountValidator : AbstractValidator<NewLinkedUserAccount>
{
    public NewLinkedUserAccountValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9][a-z0-9._@+-]*$")
            .WithKey(MsgKeys.Validation.UsernameFormat);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

/// <summary>
/// Shared rules for linking iam.user_account to educators.educator / students.guardian (V48).
/// Tenant integrity is enforced twice: here (clear 404 before the write) and by the composite FKs
/// fk_educator_user_id_tnt / fk_guardian_user_id_tnt (V39–V40).
/// </summary>
public static class UserAccountLinks
{
    public static async Task<LinkedUserAccountDto?> LoadAsync(IAppDbContext db, Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return null;
        return await db.UserAccounts.AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => new LinkedUserAccountDto(u.Id, u.Username, u.FullName, u.Email, u.Status, u.LastLoginAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>404 unless the user is a live account of <paramref name="corporationId"/>.</summary>
    public static Task EnsureUserInTenantAsync(IAppDbContext db, Guid corporationId, Guid userId, CancellationToken ct)
        => TenantUserGuard.EnsureBelongsToTenantAsync(db, corporationId, userId, "userId", ct);

    /// <summary>
    /// Creates an active user with a random temporary password and grants <see cref="NewLinkedUserAccount.RoleId"/>.
    /// The caller links the returned user to its record and saves. When <paramref name="requiredPermission"/>
    /// is given, the role must carry it (a parent account without portal:access could not use the portal).
    /// </summary>
    public static async Task<(UserAccount User, string TemporaryPassword)> CreateWithTemporaryPasswordAsync(
        IAppDbContext db,
        IPasswordService passwords,
        Guid corporationId,
        NewLinkedUserAccount req,
        Guid? createdBy,
        string? requiredPermission,
        CancellationToken ct)
    {
        var username = req.Username.Trim().ToLowerInvariant();
        if (await db.UsernameExistsAsync(username, ct))
            throw new ValidationException([ValidationFailures.For("username", MsgKeys.Rule.UsernameTaken, username)]);

        var email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim().ToLowerInvariant();
        if (email is not null
            && await db.UserAccounts.AnyAsync(u => u.CorporationId == corporationId && u.Email == email, ct))
            throw new ValidationException([ValidationFailures.For("email", MsgKeys.Rule.EmailRegistered, email)]);

        var roleExists = await db.Roles.AnyAsync(
            r => r.Id == req.RoleId && (r.CorporationId == null || r.CorporationId == corporationId), ct);
        if (!roleExists) throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Role"));

        if (requiredPermission is not null)
        {
            var hasPermission = await db.RolePermissions
                .AnyAsync(rp => rp.RoleId == req.RoleId && rp.Permission!.Code == requiredPermission, ct);
            if (!hasPermission)
                throw new ValidationException([ValidationFailures.For("roleId", MsgKeys.Rule.RoleLacksPermission, requiredPermission)]);
        }

        if (req.CampusId.HasValue
            && !await db.Campuses.AnyAsync(c => c.Id == req.CampusId.Value && c.CorporationId == corporationId, ct))
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Campus"));

        var temporaryPassword = GenerateTemporaryPassword();
        var user = UserAccount.Create(
            corporationId,
            username,
            req.FullName.Trim(),
            email,
            string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
            passwords.Hash(temporaryPassword),
            preferredLocale: null,
            primaryCampusId: req.CampusId,
            createdBy: createdBy,
            activateImmediately: true);
        db.UserAccounts.Add(user);

        db.UserRoles.Add(new UserRole
        {
            CorporationId = corporationId,
            UserId = user.Id,
            RoleId = req.RoleId,
            CampusId = null,
            CreatedBy = createdBy
        });

        return (user, temporaryPassword);
    }

    /// <summary>
    /// Creates the account (see <see cref="CreateWithTemporaryPasswordAsync"/>), then lets
    /// <paramref name="link"/> point the record at it — in one transaction, so a failed link leaves
    /// no orphan account. Two saves because EF has no navigation between the record and the user
    /// and would not order the INSERT before the UPDATE on its own.
    /// </summary>
    public static async Task<CreatedLinkedUserAccountDto> CreateAndLinkAsync(
        IAppDbContext db,
        IPasswordService passwords,
        Guid corporationId,
        NewLinkedUserAccount req,
        Guid? createdBy,
        string? requiredPermission,
        Action<Guid> link,
        CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var (user, temporaryPassword) = await CreateWithTemporaryPasswordAsync(
                db, passwords, corporationId, req, createdBy, requiredPermission, ct);
            await db.SaveChangesAsync(ct);
            link(user.Id);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new CreatedLinkedUserAccountDto(ToDto(user), temporaryPassword);
        });
    }

    public static LinkedUserAccountDto ToDto(UserAccount u)
        => new(u.Id, u.Username, u.FullName, u.Email, u.Status, u.LastLoginAt);

    // No look-alike characters (0/O, 1/l/I) — the admin reads it out or writes it down.
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*?";

    /// <summary>14 characters from a CSPRNG, at least one of each class (meets the 8+ password rule).</summary>
    public static string GenerateTemporaryPassword()
    {
        const string all = Lower + Upper + Digits + Symbols;
        var chars = new char[14];
        chars[0] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        chars[1] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        chars[3] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}
