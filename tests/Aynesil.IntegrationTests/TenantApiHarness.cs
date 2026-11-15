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
/// Gerçek HTTP hattı + en az yetkili aynesil_app bağlantısı (RLS etkin) + devops/new_tenant.py ile açılan kurumlar.
/// Kurum yöneticisi new_tenant.py'nin "admin" rolüyle açılır (platform izinleri hariç her şey).
/// Ortam değişkenleri süreç geneli olduğundan testler aynı koleksiyonda (sıralı) çalışır; <see cref="Dispose"/> temizler.
/// </summary>
public sealed class TenantApiHarness : IDisposable
{
    /// <summary>new_tenant.py --dry-run masks the password in its SQL; that literal becomes the admin password.</summary>
    public const string AdminPassword = "********";
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly MigratedDatabaseFixture _db;

    public TenantApiHarness(MigratedDatabaseFixture db)
    {
        _db = db;
        var appConnection = new NpgsqlConnectionStringBuilder(db.ConnectionString) { Username = "aynesil_app", Password = "app_it" };
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", appConnection.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
    }

    public WebApplicationFactory<Program> Factory { get; }

    public void Dispose()
    {
        Factory.Dispose();
        foreach (var key in EnvKeys) Environment.SetEnvironmentVariable(key, null);
    }

    public async Task<T> ScalarAsync<T>(string sql, params (string, object)[] args)
    {
        await using var conn = await _db.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task ExecAsync(string sql, params (string, object)[] args)
    {
        await using var conn = await _db.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Opens a tenant with devops/new_tenant.py (dry-run SQL executed as owner); returns the corporation id.</summary>
    public async Task<Guid> NewTenantAsync(string code, string adminUsername)
    {
        var psi = new ProcessStartInfo("python3")
        {
            ArgumentList =
            {
                Path.Combine(RepoRoot(), "devops", "new_tenant.py"), "--dry-run", "--code", code, "--name", $"Kurum {code}",
                "--admin-username", adminUsername, "--admin-name", "Yönetici", "--admin-password", "Gecici#Sifre1",
            },
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        using var process = Process.Start(psi)!;
        var sql = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, await process.StandardError.ReadToEndAsync());

        var statements = string.Join('\n', sql.Split('\n').Where(l => !l.TrimStart().StartsWith('\\')));
        await ExecAsync(statements);
        return await ScalarAsync<Guid>("select id from core.corporation where code = @c", ("c", code));
    }

    public async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(
        HttpMethod method, string url, string? token = null, object? body = null, string lang = "tr")
    {
        var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Accept-Language", lang);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    /// <summary>Raw login; the caller inspects status and body.</summary>
    public Task<(HttpStatusCode Status, JsonElement Body)> TryLoginAsync(string username, string password, string lang = "tr")
        => SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password }, lang: lang);

    /// <summary>
    /// Logs in and returns the login data (accessToken, refreshToken, …). Accounts that must change their password
    /// (V50) change it to <c>password + "-x9"</c> first, then log in again.
    /// </summary>
    public async Task<JsonElement> LoginAsync(string username, string password)
    {
        var (status, body) = await TryLoginAsync(username, password);
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        var data = body.GetProperty("data");
        if (!data.GetProperty("mustChangePassword").GetBoolean()) return data;

        var changed = password + "-x9";
        (await SendAsync(HttpMethod.Post, "/api/auth/change-password", data.GetProperty("accessToken").GetString(),
                new { currentPassword = password, newPassword = changed }))
            .Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (status, body) = await TryLoginAsync(username, changed);
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        return body.GetProperty("data");
    }

    public static string Token(JsonElement loginData) => loginData.GetProperty("accessToken").GetString()!;
    public static string Refresh(JsonElement loginData) => loginData.GetProperty("refreshToken").GetString()!;

    /// <summary>Creates a user through the API (admin token) and returns its id.</summary>
    public async Task<Guid> CreateUserAsync(string adminToken, string username, string password)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/users", adminToken,
            new { username, fullName = "Test Kullanıcı", email = (string?)null, phone = (string?)null, password, preferredLocale = "tr", primaryCampusId = (Guid?)null });
        status.Should().Be(HttpStatusCode.Created, body.ToString());
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Aynesil.sln"))) return dir.FullName;
        throw new InvalidOperationException("Repository root (Aynesil.sln) not found.");
    }
}
