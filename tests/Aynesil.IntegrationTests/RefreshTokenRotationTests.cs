using System.Net;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Yenileme jetonu tek kullanımlıktır: yenileme eski oturumu iptal eder, aynı jeton ikinci kez kullanılamaz.
/// API, RLS'ye tabi aynesil_app rolüyle bağlıyken (yenilemede kurum bağlamı yoktur; iptal aynı bağlantıda
/// kurum GUC'u kurularak yapılmalıdır).
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RefreshTokenRotationTests : IDisposable
{
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public RefreshTokenRotationTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task A_refresh_token_can_be_used_only_once()
    {
        var admin = $"admin-rt-{_suffix}@example.test";
        await _h.NewTenantAsync($"rt_{_suffix}", admin);
        var data = await _h.LoginAsync(admin, TenantApiHarness.AdminPassword);
        var first = new { refreshToken = TenantApiHarness.Refresh(data) };

        var (ok, okBody) = await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh", body: first);
        ok.Should().Be(HttpStatusCode.OK, okBody.ToString());

        (await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh", body: first)).Status
            .Should().Be(HttpStatusCode.Unauthorized, "eski oturum yenilemede iptal edilmiş olmalı");

        var rotated = new { refreshToken = okBody.GetProperty("data").GetProperty("refreshToken").GetString() };
        (await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh", body: rotated)).Status.Should().Be(HttpStatusCode.OK);
    }
}
