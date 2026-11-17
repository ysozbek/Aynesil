using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aynesil.Infrastructure.Services.Auth;

/// <inheritdoc cref="ISessionRevocation"/>
/// <remarks>
/// Önbellek süreç belleğindedir (StaleTenantMiddleware ve hız sınırı sayaçlarıyla aynı kısıt): API birden çok
/// örnekte çalışırsa diğer örnekler iptali en geç önbellek süresi (1 dk) sonra görür.
/// </remarks>
public sealed class SessionRevocation : ISessionRevocation
{
    private readonly IAppDbContext _db;
    private readonly IMemoryCache _cache;

    public SessionRevocation(IAppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public void Forget(Guid userId) => _cache.Remove(CacheKeys.UserSessionState(userId));

    public async Task RevokeAccessTokensAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return;
        var now = DateTimeOffset.UtcNow;
        var ids = userIds.Distinct().ToList();
        // RLS: yalnızca oturumdaki kurumun kullanıcıları güncellenir.
        await _db.UserAccounts
            .Where(u => ids.Contains(u.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SessionsRevokedAt, now), ct);
        foreach (var id in ids) Forget(id);
    }

    public async Task RevokeRoleMembersAsync(Guid roleId, CancellationToken ct)
    {
        var userIds = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);
        await RevokeAccessTokensAsync(userIds, ct);
    }
}
