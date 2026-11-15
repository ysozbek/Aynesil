using System.Net;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Yöneticinin kullanıcı şifresini sıfırlaması (POST /api/users/{id}/reset-password, user:reset_password):
/// yeni geçici şifre bir kez döner, eski şifre ve açık oturumlar geçersiz olur, kullanıcı ilk girişte şifresini
/// değiştirmek zorundadır. Başka kurumun kullanıcısı 404, kendi hesabı 422, izni olmayan 403.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserPasswordResetTests : IDisposable
{
    private const string UserPassword = "Ilk#Sifre1234";
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public UserPasswordResetTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    private async Task<(string AdminToken, Guid AdminId, Guid UserId, string Username)> SetupAsync()
    {
        await _h.NewTenantAsync($"rp_{_suffix}", $"admin-rp-{_suffix}@example.test");
        var admin = await _h.LoginAsync($"admin-rp-{_suffix}@example.test", TenantApiHarness.AdminPassword);
        var adminToken = TenantApiHarness.Token(admin);
        var username = $"user-rp-{_suffix}@example.test";
        var userId = await _h.CreateUserAsync(adminToken, username, UserPassword);
        return (adminToken, admin.GetProperty("userId").GetGuid(), userId, username);
    }

    [Fact]
    public async Task Reset_returns_a_one_time_password_revokes_sessions_and_forces_a_password_change()
    {
        var (adminToken, _, userId, username) = await SetupAsync();
        var before = await _h.LoginAsync(username, UserPassword);   // ilk girişte şifre değiştirir: UserPassword-x9
        var currentPassword = UserPassword + "-x9";

        var (status, body) = await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/reset-password", adminToken);
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        var temporary = body.GetProperty("data").GetProperty("temporaryPassword").GetString()!;
        temporary.Should().HaveLength(14);
        body.GetProperty("data").GetProperty("username").GetString().Should().Be(username);

        // Eski oturum: yenileme reddedilir.
        (await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh", body: new { refreshToken = TenantApiHarness.Refresh(before) }))
            .Status.Should().Be(HttpStatusCode.Unauthorized);
        // Eski şifre geçmez, geçici şifre geçer ve şifre değişimi zorunludur.
        (await _h.TryLoginAsync(username, currentPassword)).Status.Should().Be(HttpStatusCode.Unauthorized);
        var (okStatus, okBody) = await _h.TryLoginAsync(username, temporary);
        okStatus.Should().Be(HttpStatusCode.OK, okBody.ToString());
        okBody.GetProperty("data").GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();

        (await _h.ScalarAsync<bool>("select must_change_password from iam.user_account where id = @id", ("id", userId)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Reset_unlocks_an_account_locked_by_failed_attempts()
    {
        var (adminToken, _, userId, username) = await SetupAsync();
        // 5 hatalı deneme → 15 dk kilit (PasswordService). Doğru şifre de kilitliyken reddedilir.
        for (var i = 0; i < 5; i++)
            (await _h.TryLoginAsync(username, "yanlis-sifre")).Status.Should().Be(HttpStatusCode.Unauthorized);
        var (lockedStatus, lockedBody) = await _h.TryLoginAsync(username, UserPassword);
        lockedStatus.Should().Be(HttpStatusCode.Unauthorized);
        lockedBody.GetProperty("code").GetString().Should().Be("Auth.AccountLocked");

        var (status, body) = await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/reset-password", adminToken);
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        var temporary = body.GetProperty("data").GetProperty("temporaryPassword").GetString()!;

        (await _h.TryLoginAsync(username, temporary)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User_of_another_corporation_is_not_found()
    {
        var (adminToken, _, _, _) = await SetupAsync();
        var otherCorp = await _h.NewTenantAsync($"rpo_{_suffix}", $"admin-rpo-{_suffix}@example.test");
        var otherUser = await _h.ScalarAsync<Guid>("select id from iam.user_account where corporation_id = @c", ("c", otherCorp));

        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{otherUser}/reset-password", adminToken))
            .Status.Should().Be(HttpStatusCode.NotFound);
        (await _h.ScalarAsync<bool>("select must_change_password from iam.user_account where id = @id", ("id", otherUser)))
            .Should().BeTrue("new_tenant.py yöneticisi zaten bayraklı açılır; şifresi değişmemiş olmalı");
        (await _h.TryLoginAsync($"admin-rpo-{_suffix}@example.test", TenantApiHarness.AdminPassword))
            .Status.Should().Be(HttpStatusCode.OK, "başka kurumun şifresi değişmemeli");
    }

    [Fact]
    public async Task Admin_cannot_reset_own_password_here()
    {
        var (adminToken, adminId, _, _) = await SetupAsync();
        var (status, body) = await _h.SendAsync(HttpMethod.Post, $"/api/users/{adminId}/reset-password", adminToken);
        status.Should().Be(HttpStatusCode.UnprocessableEntity, body.ToString());
    }

    [Fact]
    public async Task Without_the_permission_the_endpoint_is_forbidden()
    {
        var (_, _, userId, username) = await SetupAsync();
        var plain = await _h.LoginAsync(username, UserPassword);   // rolsüz kullanıcı
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/reset-password", TenantApiHarness.Token(plain)))
            .Status.Should().Be(HttpStatusCode.Forbidden);
    }
}
