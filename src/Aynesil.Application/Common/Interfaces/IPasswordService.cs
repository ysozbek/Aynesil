namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// BCrypt-based password hashing service.
/// Work factor defaults to 12 (suitable for 2024+ hardware).
/// Includes account lockout tracking: MaxFailedAttempts = 5, LockoutDuration = 15min.
/// </summary>
public interface IPasswordService
{
    string Hash(string plaintext);

    bool Verify(string plaintext, string hash);

    /// <summary>Records a failed login attempt. Returns true when account is now locked.</summary>
    Task<bool> RecordFailedAttemptAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns true if the account is currently locked out.</summary>
    Task<bool> IsLockedOutAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Resets the failed attempt counter after a successful login.</summary>
    Task ResetFailedAttemptsAsync(Guid userId, CancellationToken ct = default);
}
