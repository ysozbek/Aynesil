using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Kurum bazında modül görünürlüğü (V53) gerçek HTTP hattında (aynesil_app, RLS etkin):
///   - new_tenant.py ile açılan kurumda 6 isteğe bağlı modül kapalıdır: uçları 403 Error.ModuleDisabled,
///     menüde görünmez; modül dışı uçlar etkilenmez;
///   - platform (sahip rolü) modülü açınca uçlar ve menü gelir, kapatınca tekrar gider;
///   - kurum yöneticisi ayar güncellemesiyle modül açamaz (gönderilen `modules` yok sayılır).
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ModuleVisibilityTests : IDisposable
{
    private const string AdminPassword = "Gecici#Sifre1";
    /// <summary>new_tenant.py --dry-run masks the password in its SQL; that literal becomes the admin password.</summary>
    private const string DryRunPassword = "********";
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly MigratedDatabaseFixture _db;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public ModuleVisibilityTests(MigratedDatabaseFixture db)
    {
        _db = db;
        var appConnection = new NpgsqlConnectionStringBuilder(db.ConnectionString) { Username = "aynesil_app", Password = "app_it" };
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", appConnection.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var key in EnvKeys) Environment.SetEnvironmentVariable(key, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>Opens a tenant with devops/new_tenant.py (dry-run SQL executed as owner); returns the corporation id.</summary>
    private async Task<Guid> NewTenantAsync(string code, string adminUsername)
    {
        var root = RepoRoot();
        var psi = new ProcessStartInfo("python3")
        {
            ArgumentList =
            {
                Path.Combine(root, "devops", "new_tenant.py"), "--dry-run", "--code", code, "--name", $"Kurum {code}",
                "--admin-username", adminUsername, "--admin-name", "Yönetici", "--admin-password", AdminPassword,
            },
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        using var process = Process.Start(psi)!;
        var sql = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, await process.StandardError.ReadToEndAsync());

        var statements = string.Join('\n', sql.Split('\n').Where(l => !l.TrimStart().StartsWith('\\')));
        await using var owner = await _db.OpenAsync();
        await using (var cmd = new NpgsqlCommand(statements, owner)) await cmd.ExecuteNonQueryAsync();
        return await ScalarAsync<Guid>(owner, "select id from core.corporation where code = @c", ("c", code));
    }

    private async Task<Guid> RoleIdAsync(Guid corporationId, string code)
    {
        await using var owner = await _db.OpenAsync();
        return await ScalarAsync<Guid>(owner,
            "select id from iam.role where corporation_id = @c and code = @r", ("c", corporationId), ("r", code));
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(
        HttpMethod method, string url, string? token = null, object? body = null)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Accept-Language", "tr");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password });
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        var token = body.GetProperty("data").GetProperty("accessToken").GetString()!;
        if (!body.GetProperty("data").GetProperty("mustChangePassword").GetBoolean()) return token;

        // Accounts opened by an admin (new_tenant.py, user-account create) start with must_change_password (V50).
        var changed = password + "-x9";
        (await SendAsync(HttpMethod.Post, "/api/auth/change-password", token, new { currentPassword = password, newPassword = changed }))
            .Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (status, body) = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password = changed });
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    /// <summary>Every string value in the body, unescaped (raw JSON escapes non-ASCII characters).</summary>
    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        _ => [],
    };

    private static Guid Id(JsonElement body) => body.GetProperty("data").GetProperty("id").GetGuid();

    private async Task<Guid> CreateEducatorAsync(string token, string firstName)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/educators", token, new { firstName, lastName = "Eğitmen" });
        status.Should().Be(HttpStatusCode.Created, body.ToString());
        return Id(body);
    }

    private async Task<Guid> CreateUserAsync(string token, string username)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/users", token,
            new { username, fullName = "Bağlanacak Kullanıcı", password = "Kullanici#Sifre1", activateImmediately = true });
        status.Should().Be(HttpStatusCode.Created, body.ToString());
        return Id(body);
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Aynesil.sln"))) return dir.FullName;
        throw new InvalidOperationException("Repository root (Aynesil.sln) not found.");
    }

    private static readonly (string Module, string Url, string MenuCode)[] Gated =
    [
        ("camps", "/api/camps", "camps"),
        ("consultancy", "/api/consultancy/institutions", "consultancy"),
        ("camera", "/api/cameras", "cameras"),
        ("surveys", "/api/surveys", "students-surveys"),
        ("care_team", "/api/care-team/my-students", "students-care-team"),
        ("kpi", "/api/performance-kpi/definitions", "performance"),
    ];

    private static IEnumerable<string> MenuCodes(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Array)
            foreach (var n in node.EnumerateArray())
            {
                yield return n.GetProperty("code").GetString()!;
                if (n.TryGetProperty("children", out var c))
                    foreach (var code in MenuCodes(c)) yield return code;
            }
    }

    private async Task SetModulesAsync(Guid corporationId, bool enabled)
    {
        var patch = string.Join(",", Gated.Select(g => $"\"{g.Module}\": {(enabled ? "true" : "false")}"));
        await using var owner = await _db.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "update core.corporation set settings = jsonb_set(settings, '{modules}', @m::jsonb, true) where id = @c", owner);
        cmd.Parameters.AddWithValue("m", "{" + patch + "}");
        cmd.Parameters.AddWithValue("c", corporationId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool IsModuleDisabled(HttpStatusCode status, JsonElement body) =>
        status == HttpStatusCode.Forbidden
        && body.ValueKind == JsonValueKind.Object
        && body.TryGetProperty("code", out var code)
        && code.GetString() == "Error.ModuleDisabled";

    [Fact]
    public async Task A_new_tenant_starts_with_the_optional_modules_closed_endpoints_403_and_menu_hidden()
    {
        var admin = $"admin-mv-{_suffix}@t.test";
        await NewTenantAsync($"mv_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        foreach (var (module, url, _) in Gated)
        {
            var (status, body) = await SendAsync(HttpMethod.Get, url, token);
            IsModuleDisabled(status, body).Should().BeTrue($"{module} kapalı başlamalı ({url} → {(int)status} {body})");
        }

        var (menuStatus, menu) = await SendAsync(HttpMethod.Get, "/api/menus/me", token);
        menuStatus.Should().Be(HttpStatusCode.OK);
        var codes = MenuCodes(menu.GetProperty("data")).ToHashSet();
        codes.Should().NotContain(Gated.Select(g => g.MenuCode), "kapalı modülün menüsü görünmez");
        codes.Should().Contain("students", "modül dışı menü etkilenmez");

        var (studentsStatus, _) = await SendAsync(HttpMethod.Get, "/api/students", token);
        studentsStatus.Should().Be(HttpStatusCode.OK, "modül dışı uçlar etkilenmez");
    }

    [Fact]
    public async Task Enabling_and_disabling_modules_takes_effect_immediately_without_a_restart()
    {
        var admin = $"admin-mt-{_suffix}@t.test";
        var corp = await NewTenantAsync($"mt_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        await SetModulesAsync(corp, enabled: true);
        foreach (var (module, url, _) in Gated)
        {
            var (status, body) = await SendAsync(HttpMethod.Get, url, token);
            IsModuleDisabled(status, body).Should().BeFalse($"{module} açıkken engellenmemeli");
            ((int)status).Should().BeLessThan(500, $"{url}: {body}");
        }
        var (_, menu) = await SendAsync(HttpMethod.Get, "/api/menus/me", token);
        MenuCodes(menu.GetProperty("data")).Should().Contain(["camps", "consultancy", "cameras", "performance"]);

        await SetModulesAsync(corp, enabled: false);
        var (closedStatus, closedBody) = await SendAsync(HttpMethod.Get, "/api/camps", token);
        IsModuleDisabled(closedStatus, closedBody).Should().BeTrue();
        var (_, menu2) = await SendAsync(HttpMethod.Get, "/api/menus/me", token);
        MenuCodes(menu2.GetProperty("data")).Should().NotContain(["camps", "consultancy", "cameras", "performance"]);
    }

    [Fact]
    public async Task Tenant_admin_cannot_open_a_module_through_the_settings_update()
    {
        var admin = $"admin-ms-{_suffix}@t.test";
        var corp = await NewTenantAsync($"ms_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        var (_, current) = await SendAsync(HttpMethod.Get, $"/api/corporations/{corp}/settings", token);
        var data = current.GetProperty("data");
        data.GetProperty("modules").GetProperty("camps").GetBoolean().Should().BeFalse();
        var (_, corpBody) = await SendAsync(HttpMethod.Get, $"/api/corporations/{corp}", token);

        var (putStatus, put) = await SendAsync(HttpMethod.Put, $"/api/corporations/{corp}/settings", token, new
        {
            defaultLocale = data.GetProperty("defaultLocale").GetString(),
            defaultCurrency = data.GetProperty("defaultCurrency").GetString(),
            timezone = data.GetProperty("timezone").GetString(),
            settings = "{\"modules\":{\"camps\":true,\"surveys\":true},\"tema\":\"koyu\"}",
            rowVersion = corpBody.GetProperty("data").GetProperty("rowVersion").GetInt32(),
        });
        putStatus.Should().Be(HttpStatusCode.OK, put.ToString());

        var after = put.GetProperty("data");
        after.GetProperty("modules").GetProperty("camps").GetBoolean().Should().BeFalse("modules platform işidir");
        after.GetProperty("modules").GetProperty("surveys").GetBoolean().Should().BeFalse();
        after.GetProperty("settings").GetString().Should().Contain("tema", "diğer ayarlar kaydedilir");

        var (status, body) = await SendAsync(HttpMethod.Get, "/api/camps", token);
        IsModuleDisabled(status, body).Should().BeTrue();
    }
}
