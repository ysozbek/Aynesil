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
/// Y1 against PostgreSQL 17 with all migrations (V43 included):
///   - core.corporation RLS: a tenant (aynesil_app) sees/changes only its own row;
///   - V43 removed the platform-only permissions from every role;
///   - devops/new_tenant.py still opens a tenant (owner role), its admin logs in and refreshes the
///     token through the API connected as the least-privilege app role (RLS active), gets
///     corporation:read/update but not corporation:create/delete, lists only its own corporation and
///     gets 404 for another corporation (GET and PUT).
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CorporationIsolationTests : IDisposable
{
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly MigratedDatabaseFixture _db;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public CorporationIsolationTests(MigratedDatabaseFixture db)
    {
        _db = db;
        // The API connects as aynesil_app, as in production: RLS is in force for every request.
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

    private static async Task<int> ExecAsync(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        return await cmd.ExecuteNonQueryAsync();
    }

    private async Task<Guid> NewCorporationAsync(NpgsqlConnection owner, string code) =>
        await ScalarAsync<Guid>(owner,
            "insert into core.corporation(code, legal_name, display_name) values (@c, @c, @c) returning id",
            ("c", code));

    /// <summary>Runs devops/new_tenant.py --dry-run and executes its SQL as the owner (what the script does via psql).</summary>
    private async Task<string> RunNewTenantScriptAsync(string code, string username)
    {
        var root = RepoRoot();
        var psi = new ProcessStartInfo("python3")
        {
            ArgumentList =
            {
                Path.Combine(root, "devops", "new_tenant.py"), "--dry-run", "--code", code, "--name", $"Kurum {code}",
                "--admin-username", username, "--admin-name", "Yönetici", "--admin-password", "Gecici#Sifre1",
            },
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        using var process = Process.Start(psi)!;
        var sql = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, await process.StandardError.ReadToEndAsync());

        // psql meta-commands (\set …) are not SQL; the dry-run masks the password as '********'.
        var statements = string.Join('\n', sql.Split('\n').Where(l => !l.TrimStart().StartsWith('\\')));
        await using var owner = await _db.OpenAsync();
        await ExecAsync(owner, statements);
        return "********";
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(
        HttpMethod method, string url, string? token = null, object? body = null)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private static string[] PermissionClaims(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(payload)).RootElement;
        return json.TryGetProperty("perm", out var perm)
            ? perm.ValueKind == JsonValueKind.Array ? perm.EnumerateArray().Select(p => p.GetString()!).ToArray() : [perm.GetString()!]
            : [];
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Aynesil.sln"))) return dir.FullName;
        throw new InvalidOperationException("Repository root (Aynesil.sln) not found.");
    }

    // ── core.corporation RLS (aynesil_app) ────────────────────────────────────

    [Fact]
    public async Task App_role_sees_and_changes_only_its_own_corporation()
    {
        await using var owner = await _db.OpenAsync();
        var a = await NewCorporationAsync(owner, $"rls_a_{_suffix}");
        var b = await NewCorporationAsync(owner, $"rls_b_{_suffix}");

        await using var app = await _db.OpenAsAppAsync(a);
        (await ScalarAsync<long>(app, "select count(*) from core.corporation")).Should().Be(1);
        (await ScalarAsync<Guid>(app, "select id from core.corporation")).Should().Be(a);
        (await ExecAsync(app, "update core.corporation set display_name = 'x' where id = @b", ("b", b))).Should().Be(0);
        (await ExecAsync(app, "update core.corporation set status = 'suspended' where id = @b", ("b", b))).Should().Be(0);
        (await ExecAsync(app, "update core.corporation set display_name = 'Kendi' where id = @a", ("a", a))).Should().Be(1);

        var insert = () => ExecAsync(app, "insert into core.corporation(code, legal_name, display_name) values (@c, 'x', 'x')",
            ("c", $"rls_new_{_suffix}"));
        (await insert.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        (await ScalarAsync<string>(owner, "select display_name from core.corporation where id = @b", ("b", b))).Should().Be($"rls_b_{_suffix}");
    }

    [Fact]
    public async Task App_role_without_tenant_sees_no_corporation()
    {
        await using var owner = await _db.OpenAsync();
        await NewCorporationAsync(owner, $"rls_n_{_suffix}");

        var builder = new NpgsqlConnectionStringBuilder(_db.ConnectionString) { Username = "aynesil_app", Password = "app_it" };
        await using var app = new NpgsqlConnection(builder.ConnectionString);
        await app.OpenAsync();

        (await ScalarAsync<long>(app, "select count(*) from core.corporation")).Should().Be(0);
    }

    [Fact]
    public async Task No_role_holds_a_platform_only_permission_after_V43()
    {
        await using var owner = await _db.OpenAsync();
        (await ScalarAsync<long>(owner, """
            select count(*) from iam.role_permission rp join iam.permission p on p.id = rp.permission_id
             where p.code in ('corporation:create', 'corporation:delete')
            """)).Should().Be(0);
    }

    // ── End to end: new_tenant.py → login → refresh → corporation endpoints ───

    [Fact]
    public async Task New_tenant_admin_logs_in_refreshes_and_is_limited_to_its_own_corporation()
    {
        var username = $"admin-{_suffix}@example.test";
        var password = await RunNewTenantScriptAsync($"nt_{_suffix}", username);
        await using var owner = await _db.OpenAsync();
        var own = await ScalarAsync<Guid>(owner, "select id from core.corporation where code = @c", ("c", $"nt_{_suffix}"));
        var other = await NewCorporationAsync(owner, $"nt_other_{_suffix}");

        // Login (SECURITY DEFINER lookup + permission load) works under RLS.
        var (loginStatus, login) = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password });
        loginStatus.Should().Be(HttpStatusCode.OK, login.ToString());
        var token = login.GetProperty("data").GetProperty("accessToken").GetString()!;
        login.GetProperty("data").GetProperty("corporationId").GetGuid().Should().Be(own);

        // The script marks the admin must_change_password (V50): other endpoints stay closed until the password changes.
        login.GetProperty("data").GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();
        (await SendAsync(HttpMethod.Get, "/api/corporations?page=1&pageSize=50", token)).Status.Should().Be(HttpStatusCode.Forbidden);
        var changed = password + "-x9";
        (await SendAsync(HttpMethod.Post, "/api/auth/change-password", token, new { currentPassword = password, newPassword = changed }))
            .Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (loginStatus, login) = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password = changed });
        loginStatus.Should().Be(HttpStatusCode.OK, login.ToString());
        token = login.GetProperty("data").GetProperty("accessToken").GetString()!;

        var perms = PermissionClaims(token);
        perms.Should().Contain(["corporation:read", "corporation:update"]);
        perms.Should().NotContain(["corporation:create", "corporation:delete"]);

        // Token refresh works under RLS.
        var refreshToken = login.GetProperty("data").GetProperty("refreshToken").GetString()!;
        var (refreshStatus, refreshed) = await SendAsync(HttpMethod.Post, "/api/auth/refresh", body: new { refreshToken });
        refreshStatus.Should().Be(HttpStatusCode.OK, refreshed.ToString());
        token = refreshed.GetProperty("data").GetProperty("accessToken").GetString()!;

        // List: only its own corporation.
        var (listStatus, list) = await SendAsync(HttpMethod.Get, "/api/corporations?page=1&pageSize=50", token);
        listStatus.Should().Be(HttpStatusCode.OK);
        list.GetProperty("data").GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("id").GetGuid()).Should().Equal(own);

        // Another corporation: 404 for read, settings and update.
        (await SendAsync(HttpMethod.Get, $"/api/corporations/{other}", token)).Status.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Get, $"/api/corporations/{other}/settings", token)).Status.Should().Be(HttpStatusCode.NotFound);
        var update = new
        {
            legalName = "Ele geçirildi", displayName = "Ele geçirildi", defaultLocale = "tr", defaultCurrency = "TRY",
            timezone = "Europe/Istanbul", taxOffice = (string?)null, taxNumber = (string?)null, rowVersion = 1,
        };
        (await SendAsync(HttpMethod.Put, $"/api/corporations/{other}", token, update)).Status.Should().Be(HttpStatusCode.NotFound);

        // Activate/deactivate and create are platform operations: 403 for the tenant admin.
        (await SendAsync(HttpMethod.Post, $"/api/corporations/{other}/deactivate", token)).Status.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Post, $"/api/corporations/{own}/deactivate", token)).Status.Should().Be(HttpStatusCode.Forbidden);

        (await ScalarAsync<string>(owner, "select display_name from core.corporation where id = @o", ("o", other)))
            .Should().Be($"nt_other_{_suffix}");
        (await ScalarAsync<string>(owner, "select status from core.corporation where id = @o", ("o", other))).Should().Be("active");

        // Its own corporation is readable and updatable.
        var (getOwn, ownBody) = await SendAsync(HttpMethod.Get, $"/api/corporations/{own}", token);
        getOwn.Should().Be(HttpStatusCode.OK);
        var ownUpdate = new
        {
            legalName = "Kendi Ünvanı", displayName = "Kendi Adı", defaultLocale = "tr", defaultCurrency = "TRY",
            timezone = "Europe/Istanbul", taxOffice = (string?)null, taxNumber = (string?)null,
            rowVersion = ownBody.GetProperty("data").GetProperty("rowVersion").GetInt32(),
        };
        var (putOwn, putBody) = await SendAsync(HttpMethod.Put, $"/api/corporations/{own}", token, ownUpdate);
        putOwn.Should().Be(HttpStatusCode.OK, putBody.ToString());
    }
}
