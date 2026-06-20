using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Auth.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Authenticated user changes their own password.
/// All existing refresh token sessions are revoked upon success (forced re-login).
/// </summary>
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from the current password.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(
        IAppDbContext db,
        IPasswordService passwords,
        ITokenService tokens,
        ICurrentUserService currentUser)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordCommand req, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated to change password.");

        var user = await _db.UserAccounts.FindAsync([userId], ct)
            ?? throw new NotFoundException("User", userId);

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !_passwords.Verify(req.CurrentPassword, user.PasswordHash))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.CurrentPassword), "Current password is incorrect.")]);

        user.ChangePassword(_passwords.Hash(req.NewPassword), userId);
        await _db.SaveChangesAsync(ct);

        // Revoke all sessions — user must re-login with new password
        await _tokens.RevokeAllAsync(userId, ct);
    }
}
