using System.IdentityModel.Tokens.Jwt;
using System.Net;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// V63 (docs/raporlar/2026-11-17_dogrulama.md D1): erişim jetonu süresi dolmadan iptal edilir. Şifre sıfırlama,
/// askıya alma, silme, şifre değişimi ve rol/izin değişikliğinden sonra eski jeton bir sonraki istekte 401
/// <c>Auth.SessionRevoked</c> alır (önceden süresi dolana kadar 200 dönüyordu). Rol/izin değişikliğinde yenileme
/// jetonu geçerli kalır: istemci yeniler ve güncel izinlerle devam eder. İptalden hemen sonraki yeni giriş reddedilmez.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SessionRevocationTests : IDisposable
{
    private const string UserPassword = "Ilk#Sifre1234";
    private const string Probe = "/api/modules/me";   // yalnızca oturum ister (izin gerektirmez)
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public SessionRevocationTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    private async Task<(string AdminToken, Guid UserId, string Username, System.Text.Json.JsonElement UserLogin)> SetupAsync()
    {
        await _h.NewTenantAsync($"sr_{_suffix}", $"admin-sr-{_suffix}@example.test");
        var admin = await _h.LoginAsync($"admin-sr-{_suffix}@example.test", TenantApiHarness.AdminPassword);
        var username = $"user-sr-{_suffix}@example.test";
        var userId = await _h.CreateUserAsync(TenantApiHarness.Token(admin), username, UserPassword);
        var login = await _h.LoginAsync(username, UserPassword);   // ilk girişte şifre değişir → UserPassword-x9
        (await _h.SendAsync(HttpMethod.Get, Probe, TenantApiHarness.Token(login))).Status.Should().Be(HttpStatusCode.OK);
        return (TenantApiHarness.Token(admin), userId, username, login);
    }

    private async Task ShouldBeRevokedAsync(string accessToken)
    {
        var (status, body) = await _h.SendAsync(HttpMethod.Get, Probe, accessToken);
        status.Should().Be(HttpStatusCode.Unauthorized, body.ToString());
        body.GetProperty("code").GetString().Should().Be("Auth.SessionRevoked");
    }

    [Fact]
    public async Task Access_tokens_carry_iat()
    {
        var (_, _, _, login) = await SetupAsync();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(TenantApiHarness.Token(login));
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public async Task Admin_password_reset_rejects_the_old_access_token()
    {
        var (adminToken, userId, _, login) = await SetupAsync();
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/reset-password", adminToken)).Status.Should().Be(HttpStatusCode.OK);
        await ShouldBeRevokedAsync(TenantApiHarness.Token(login));
    }

    [Fact]
    public async Task Suspension_rejects_the_old_token_and_activation_lets_a_new_login_in_at_once()
    {
        var (adminToken, userId, username, login) = await SetupAsync();
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/suspend", adminToken, new { })).Status.Should().Be(HttpStatusCode.OK);
        await ShouldBeRevokedAsync(TenantApiHarness.Token(login));

        // Etkinleştirme önbelleği temizler: yeni giriş 1 dk beklemeden çalışır.
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/activate", adminToken, new { })).Status.Should().Be(HttpStatusCode.OK);
        var again = await _h.LoginAsync(username, UserPassword + "-x9");
        (await _h.SendAsync(HttpMethod.Get, Probe, TenantApiHarness.Token(again))).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deletion_rejects_the_old_access_token()
    {
        var (adminToken, userId, _, login) = await SetupAsync();
        var rowVersion = await _h.ScalarAsync<int>("select row_version from iam.user_account where id = @id", ("id", userId));
        (await _h.SendAsync(HttpMethod.Delete, $"/api/users/{userId}?rowVersion={rowVersion}", adminToken)).Status.Should().Be(HttpStatusCode.OK);
        await ShouldBeRevokedAsync(TenantApiHarness.Token(login));
    }

    [Fact]
    public async Task Own_password_change_rejects_other_sessions_but_not_the_immediate_new_login()
    {
        var (_, _, username, login) = await SetupAsync();
        var otherSession = await _h.LoginAsync(username, UserPassword + "-x9");
        (await _h.SendAsync(HttpMethod.Post, "/api/auth/change-password", TenantApiHarness.Token(login),
            new { currentPassword = UserPassword + "-x9", newPassword = "Yeni#Sifre5678" })).Status.Should().Be(HttpStatusCode.OK);

        await ShouldBeRevokedAsync(TenantApiHarness.Token(otherSession));
        var fresh = await _h.LoginAsync(username, "Yeni#Sifre5678");   // aynı saniye içinde olabilir: reddedilmemeli
        (await _h.SendAsync(HttpMethod.Get, Probe, TenantApiHarness.Token(fresh))).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Role_and_permission_changes_force_a_refresh_that_carries_the_new_permissions()
    {
        var (adminToken, userId, _, login) = await SetupAsync();
        var roleId = await _h.ScalarAsync<Guid>(
            "select r.id from iam.role r join iam.user_account u on u.corporation_id = r.corporation_id where u.id = @u and r.code = 'guardian'",
            ("u", userId));

        // 1) Rol atama: eski jeton 401, yenileme geçerli (oturumdan atılmaz).
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/roles", adminToken,
            new { roleId, campusId = (Guid?)null, validFrom = (DateTimeOffset?)null, validTo = (DateTimeOffset?)null }))
            .Status.Should().Be(HttpStatusCode.Created);
        await ShouldBeRevokedAsync(TenantApiHarness.Token(login));
        var (refreshStatus, refreshBody) = await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh",
            body: new { refreshToken = TenantApiHarness.Refresh(login) });
        refreshStatus.Should().Be(HttpStatusCode.OK, refreshBody.ToString());
        var refreshed = refreshBody.GetProperty("data");
        (await _h.SendAsync(HttpMethod.Get, Probe, TenantApiHarness.Token(refreshed))).Status.Should().Be(HttpStatusCode.OK);
        Perms(TenantApiHarness.Token(refreshed)).Should().Contain("portal:access");

        // 2) Rolden izin kaldırma: rol üyesinin jetonu 401; yenilenen jetonda izin yok.
        var permissionId = await _h.ScalarAsync<Guid>("select id from iam.permission where code = 'portal:access'");
        (await _h.SendAsync(HttpMethod.Delete, $"/api/roles/{roleId}/permissions/{permissionId}", adminToken))
            .Status.Should().Be(HttpStatusCode.OK);
        await ShouldBeRevokedAsync(TenantApiHarness.Token(refreshed));
        var (again, againBody) = await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh",
            body: new { refreshToken = TenantApiHarness.Refresh(refreshed) });
        again.Should().Be(HttpStatusCode.OK, againBody.ToString());
        Perms(TenantApiHarness.Token(againBody.GetProperty("data"))).Should().NotContain("portal:access");
    }

    private static IEnumerable<string> Perms(string jwt)
        => new JwtSecurityTokenHandler().ReadJwtToken(jwt).Claims.Where(c => c.Type == "perm").Select(c => c.Value);
}
