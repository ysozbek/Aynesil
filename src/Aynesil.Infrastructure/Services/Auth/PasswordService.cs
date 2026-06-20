using Aynesil.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Aynesil.Infrastructure.Services.Auth;

/// <summary>
/// BCrypt password hashing with work factor 12.
/// Account lockout state is stored in distributed cache (Redis) to survive pod restarts
/// in multi-replica deployments. Key: "lockout:{userId}" TTL = LockoutDurationMinutes.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;

    private readonly IDistributedCache _cache;

    public PasswordService(IDistributedCache cache) => _cache = cache;

    public string Hash(string plaintext) =>
        BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);

    public bool Verify(string plaintext, string hash) =>
        BCrypt.Net.BCrypt.Verify(plaintext, hash);

    public async Task<bool> RecordFailedAttemptAsync(Guid userId, CancellationToken ct = default)
    {
        var key = FailedKey(userId);
        var countStr = await _cache.GetStringAsync(key, ct);
        var count = int.TryParse(countStr, out var c) ? c + 1 : 1;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LockoutDurationMinutes)
        };
        await _cache.SetStringAsync(key, count.ToString(), options, ct);

        if (count >= MaxFailedAttempts)
        {
            await _cache.SetStringAsync(LockoutKey(userId), "1", options, ct);
            return true;
        }
        return false;
    }

    public async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken ct = default) =>
        await _cache.GetStringAsync(LockoutKey(userId), ct) is not null;

    public async Task ResetFailedAttemptsAsync(Guid userId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(FailedKey(userId), ct);
        await _cache.RemoveAsync(LockoutKey(userId), ct);
    }

    private static string FailedKey(Guid userId) => $"auth:failed:{userId}";
    private static string LockoutKey(Guid userId) => $"auth:lockout:{userId}";
}
