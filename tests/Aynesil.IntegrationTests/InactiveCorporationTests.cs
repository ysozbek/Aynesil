using System.Net;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Pasif kurum (V57): status 'active' değilse (suspended/closed) giriş ve token yenileme reddedilir; daha önce
/// verilmiş erişim token'ı da (StaleTenantMiddleware) 401 alır. Mesaj istek dilinde (tr/en), kod Auth.CorporationInactive.
/// Kurum yeniden etkinleşince giriş çalışır. API, RLS'ye tabi aynesil_app rolüyle bağlıdır.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class InactiveCorporationTests : IDisposable
{
    private const string Code = "Auth.CorporationInactive";
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public InactiveCorporationTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    private string Admin => $"admin-ic-{_suffix}@example.test";
    private string Password => TenantApiHarness.AdminPassword + "-x9";   // ilk girişte değiştirilen şifre

    private Task SetStatusAsync(Guid corp, string status) =>
        _h.ExecAsync("update core.corporation set status = @s where id = @id", ("s", status), ("id", corp));

    [Theory]
    [InlineData("suspended")]
    [InlineData("closed")]
    public async Task Login_is_refused_with_a_localized_message(string status)
    {
        var corp = await _h.NewTenantAsync($"ic_{status[..3]}_{_suffix}", Admin);
        await _h.LoginAsync(Admin, TenantApiHarness.AdminPassword);   // şifre değişimi (V50) tamam
        await SetStatusAsync(corp, status);

        var (tr, trBody) = await _h.TryLoginAsync(Admin, Password, "tr");
        var (en, enBody) = await _h.TryLoginAsync(Admin, Password, "en");

        tr.Should().Be(HttpStatusCode.Unauthorized);
        en.Should().Be(HttpStatusCode.Unauthorized);
        trBody.GetProperty("code").GetString().Should().Be(Code);
        trBody.GetProperty("message").GetString().Should().StartWith("Kurumunuzun hesabı etkin değil");
        enBody.GetProperty("message").GetString().Should().StartWith("Your organization's account is not active");
    }

    [Fact]
    public async Task Wrong_password_on_a_suspended_corporation_still_answers_invalid_credentials()
    {
        // Kurum durumu yalnızca parola doğruysa söylenir (kullanıcı adı/kurum varlığı sızmaz).
        var corp = await _h.NewTenantAsync($"ic_wp_{_suffix}", Admin);
        await SetStatusAsync(corp, "suspended");
        var (status, body) = await _h.TryLoginAsync(Admin, "yanlis-sifre");
        status.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("code").GetString().Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Refresh_is_refused_and_the_session_cannot_be_retried()
    {
        var corp = await _h.NewTenantAsync($"ic_rf_{_suffix}", Admin);
        var data = await _h.LoginAsync(Admin, TenantApiHarness.AdminPassword);
        await SetStatusAsync(corp, "suspended");

        var refresh = new { refreshToken = TenantApiHarness.Refresh(data) };
        var (status, body) = await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh", body: refresh);
        status.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("code").GetString().Should().Be(Code);

        // Kurum geri açılsa bile aynı yenileme jetonu kullanılamaz (oturum iptal edildi); yeni giriş gerekir.
        await SetStatusAsync(corp, "active");
        (await _h.SendAsync(HttpMethod.Post, "/api/auth/refresh", body: refresh)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await _h.TryLoginAsync(Admin, Password)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_already_issued_access_token_is_refused_by_the_middleware()
    {
        var corp = await _h.NewTenantAsync($"ic_at_{_suffix}", Admin);
        var token = TenantApiHarness.Token(await _h.LoginAsync(Admin, TenantApiHarness.AdminPassword));
        await SetStatusAsync(corp, "suspended");   // bu token'la henüz istek yapılmadı: durum önbellekte değil

        var (status, body) = await _h.SendAsync(HttpMethod.Get, "/api/menus/me", token);
        status.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("code").GetString().Should().Be(Code);
    }

    [Fact]
    public async Task Reactivated_corporation_can_log_in_again()
    {
        var corp = await _h.NewTenantAsync($"ic_re_{_suffix}", Admin);
        await _h.LoginAsync(Admin, TenantApiHarness.AdminPassword);
        await SetStatusAsync(corp, "suspended");
        (await _h.TryLoginAsync(Admin, Password)).Status.Should().Be(HttpStatusCode.Unauthorized);

        await SetStatusAsync(corp, "active");
        var (status, body) = await _h.TryLoginAsync(Admin, Password);
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        (await _h.SendAsync(HttpMethod.Get, "/api/menus/me", TenantApiHarness.Token(body.GetProperty("data"))))
            .Status.Should().Be(HttpStatusCode.OK);
    }
}
