using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// must_change_password (V50): bayrak açıkken giriş "mcp" claim'li token verir, API yalnızca parola değiştirme /
/// çıkış / token yenileme uçlarına izin verir; parola değişince bayrak kapanır ve yeni girişte kısıt kalkar.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MustChangePasswordTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Temp#Pass1234";
    private const string NewPassword = "Brand#New5678";
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly MigratedDatabaseFixture _db;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    private string Flagged => $"mcp-{_suffix}";
    private string Normal => $"ok-{_suffix}";

    public MustChangePasswordTests(MigratedDatabaseFixture db)
    {
        _db = db;
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", db.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
    }

    public async Task InitializeAsync()
    {
        await using var conn = await _db.OpenAsync();
        var corp = Guid.NewGuid();
        await Exec(conn, "insert into core.corporation(id, code, legal_name, display_name) values (@id, @code, 'T', 'T')",
            ("id", corp), ("code", $"t-{_suffix}"));

        var hash = BCrypt.Net.BCrypt.HashPassword(Password, 4);
        foreach (var (name, must) in new[] { (Flagged, true), (Normal, false) })
            await Exec(conn,
                "insert into iam.user_account(corporation_id, username, email, full_name, password_hash, status, must_change_password) values (@c, @u, @e, @u, @h, 'active', @m)",
                ("c", corp), ("u", name), ("e", $"{name}@example.test"), ("h", hash), ("m", must));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var key in EnvKeys) Environment.SetEnvironmentVariable(key, null);
    }

    private static async Task Exec(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<JsonElement> LoginDataAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data").Clone();
    }

    private HttpClient Authed(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task Flagged_user_is_limited_to_password_change_and_logout()
    {
        var data = await LoginDataAsync(Flagged, Password);
        data.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();
        var client = Authed(data.GetProperty("accessToken").GetString()!);

        using var blocked = await client.GetAsync("/api/menus/me");
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("code").GetString().Should().Be("Auth.PasswordChangeRequired");

        using var logout = await client.PostAsJsonAsync("/api/auth/logout",
            new { refreshToken = data.GetProperty("refreshToken").GetString() });
        logout.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Changing_the_password_clears_the_flag_and_lifts_the_restriction()
    {
        var data = await LoginDataAsync(Flagged, Password);
        var client = Authed(data.GetProperty("accessToken").GetString()!);

        using var change = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = Password, newPassword = NewPassword });
        change.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        await using var conn = await _db.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "select must_change_password from iam.user_account where username = @u", conn);
        cmd.Parameters.AddWithValue("u", Flagged);
        ((bool)(await cmd.ExecuteScalarAsync())!).Should().BeFalse();

        var again = await LoginDataAsync(Flagged, NewPassword);
        again.GetProperty("mustChangePassword").GetBoolean().Should().BeFalse();
        using var ok = await Authed(again.GetProperty("accessToken").GetString()!).GetAsync("/api/menus/me");
        ok.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Arayüzdeki akışın aynısı: ilk giriş → şifre değiştir → çıkış (ön yüz auth uçlarına erişim token'ı eklemez)
    /// → yeni şifreyle giriş. Çıkış anonim olarak 200 dönmeli (eskiden [Authorize] yüzünden 401 dönüyor ve ön yüz
    /// /login?redirect=/account/password'e atıyordu); yeni girişte mcp yok, kısıt kalkmış.
    /// </summary>
    [Fact]
    public async Task First_login_change_password_logout_and_login_again_has_no_mcp()
    {
        var first = await LoginDataAsync(Flagged, Password);
        first.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();
        ClaimOf(first.GetProperty("accessToken").GetString()!, "mcp").Should().Be("true");

        using (var change = await Authed(first.GetProperty("accessToken").GetString()!)
                   .PostAsJsonAsync("/api/auth/change-password", new { currentPassword = Password, newPassword = NewPassword }))
            change.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshToken = first.GetProperty("refreshToken").GetString();
        using (var logout = await _factory.CreateClient().PostAsJsonAsync("/api/auth/logout", new { refreshToken }))
            logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var again = await LoginDataAsync(Flagged, NewPassword);
        again.GetProperty("mustChangePassword").GetBoolean().Should().BeFalse();
        ClaimOf(again.GetProperty("accessToken").GetString()!, "mcp").Should().BeNull();
        using var menus = await Authed(again.GetProperty("accessToken").GetString()!).GetAsync("/api/menus/me");
        menus.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Anonim çıkış oturumu gerçekten kapatır: aynı yenileme jetonu bir daha kullanılamaz; ikinci çıkış da 200.</summary>
    [Fact]
    public async Task Anonymous_logout_revokes_the_refresh_token_and_is_idempotent()
    {
        var data = await LoginDataAsync(Normal, Password);
        var refreshToken = data.GetProperty("refreshToken").GetString();
        var anonymous = _factory.CreateClient();

        using (var logout = await anonymous.PostAsJsonAsync("/api/auth/logout", new { refreshToken }))
            logout.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var refresh = await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken }))
            refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using (var again = await anonymous.PostAsJsonAsync("/api/auth/logout", new { refreshToken }))
            again.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var unknown = await anonymous.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "bilinmeyen-jeton" }))
            unknown.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string? ClaimOf(string jwt, string claim)
        => new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(jwt)
            .Claims.FirstOrDefault(c => c.Type == claim)?.Value;

    [Fact]
    public async Task Refresh_keeps_the_restriction_while_the_flag_is_on()
    {
        var data = await LoginDataAsync(Flagged, Password);
        using var refresh = await _factory.CreateClient().PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = data.GetProperty("refreshToken").GetString() });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        refreshed.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();

        using var blocked = await Authed(refreshed.GetProperty("accessToken").GetString()!).GetAsync("/api/menus/me");
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_stale_flagged_token_does_not_block_anonymous_login()
    {
        var data = await LoginDataAsync(Flagged, Password);
        var client = Authed(data.GetProperty("accessToken").GetString()!);

        // Giriş anonim uçtur: başlıkta eski bir "mcp" token'ı olsa bile engellenmemeli.
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { username = Normal, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unflagged_user_is_not_restricted()
    {
        var data = await LoginDataAsync(Normal, Password);
        data.GetProperty("mustChangePassword").GetBoolean().Should().BeFalse();
        using var ok = await Authed(data.GetProperty("accessToken").GetString()!).GetAsync("/api/menus/me");
        ok.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
