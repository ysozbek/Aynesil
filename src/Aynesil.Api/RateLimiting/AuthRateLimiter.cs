using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Aynesil.Api.RateLimiting;

/// <summary>Anonim kimlik doğrulama uçlarının hız sınırı politikaları.</summary>
public enum AuthRateLimitPolicy
{
    Login,
    PasswordResetRequest,
    PasswordResetConfirm,
}

/// <summary>Bir politika için sabit pencere: <see cref="PermitLimit"/> istek / <see cref="Window"/>.</summary>
public sealed class FixedWindowLimit
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
}

/// <summary>
/// "RateLimiting:Auth" yapılandırması. Her sınır hem istemci IP'sine hem hedef kimliğe (kullanıcı adı,
/// e-posta ya da sıfırlama token'ı) ayrı ayrı uygulanır.
/// </summary>
public sealed class AuthRateLimitOptions
{
    public const string Section = "RateLimiting:Auth";

    public FixedWindowLimit Login { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };
    public FixedWindowLimit PasswordResetRequest { get; set; } = new() { PermitLimit = 3, Window = TimeSpan.FromHours(1) };
    public FixedWindowLimit PasswordResetConfirm { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

    public FixedWindowLimit For(AuthRateLimitPolicy policy) => policy switch
    {
        AuthRateLimitPolicy.Login => Login,
        AuthRateLimitPolicy.PasswordResetRequest => PasswordResetRequest,
        AuthRateLimitPolicy.PasswordResetConfirm => PasswordResetConfirm,
        _ => throw new System.Diagnostics.UnreachableException($"Unknown auth rate limit policy {policy}."),
    };
}

/// <summary>
/// Y7: login / parola sıfırlama isteği / sıfırlama onayı için IP başına ve hedef kimlik başına sabit
/// pencereli hız sınırı (System.Threading.RateLimiting). Bir istek, iki sınırın ikisinden de izin
/// alırsa geçer; böylece hem tek IP'den çok hesaba deneme (password spraying) hem de çok IP'den tek
/// hesaba deneme / tek adrese toplu e-posta sınırlanır.
///
/// Kimlik değeri gövdeden gelir; bu yüzden sınır, model bağlamadan sonra çalışan
/// <see cref="AuthRateLimitAttribute"/> filtresiyle uygulanır (ara katmanda gövde henüz okunmamıştır).
/// Sayaçlar süreç belleğindedir: birden fazla API örneğinde sınır örnek başınadır.
/// </summary>
public sealed class AuthRateLimiter : IDisposable
{
    private readonly Dictionary<AuthRateLimitPolicy, PartitionedRateLimiter<string>> _byIp = [];
    private readonly Dictionary<AuthRateLimitPolicy, PartitionedRateLimiter<string>> _byIdentity = [];

    public AuthRateLimiter(IOptions<AuthRateLimitOptions> options)
    {
        foreach (var policy in Enum.GetValues<AuthRateLimitPolicy>())
        {
            var limit = options.Value.For(policy);
            _byIp[policy] = Create(limit);
            _byIdentity[policy] = Create(limit);
        }
    }

    /// <summary>
    /// İzin alınırsa true. Reddedilirse <paramref name="retryAfter"/> pencerenin yenilenmesine kalan süreyi
    /// (biliniyorsa) verir. Önce IP, sonra kimlik sınırı denenir.
    /// </summary>
    public bool TryAcquire(AuthRateLimitPolicy policy, string? clientIp, string? identity, out TimeSpan? retryAfter)
    {
        retryAfter = null;
        foreach (var (limiter, key) in new[]
                 {
                     (_byIp[policy], "ip:" + (string.IsNullOrWhiteSpace(clientIp) ? "unknown" : clientIp)),
                     (_byIdentity[policy], "id:" + NormalizeIdentity(identity)),
                 })
        {
            using var lease = limiter.AttemptAcquire(key);
            if (!lease.IsAcquired)
            {
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var wait)) retryAfter = wait;
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Büyük/küçük harf ve boşluk farkı sınırı aşmaya yaramasın; değer (ör. sıfırlama token'ı) bellekte
    /// açık tutulmasın diye özetlenir.
    /// </summary>
    private static string NormalizeIdentity(string? identity)
    {
        var normalized = (identity ?? string.Empty).Trim().ToLowerInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static PartitionedRateLimiter<string> Create(FixedWindowLimit limit) =>
        PartitionedRateLimiter.Create<string, string>(key => RateLimitPartition.GetFixedWindowLimiter(key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit.PermitLimit,
                Window = limit.Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    public void Dispose()
    {
        foreach (var limiter in _byIp.Values.Concat(_byIdentity.Values)) limiter.Dispose();
    }
}
