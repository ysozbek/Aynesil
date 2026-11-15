using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Gerçek istemci IP'si (KURULUM_KILAVUZU A.6): istek ön yüz nginx'inden geçtiğinde bağlantının kaynağı nginx'tir.
/// API, X-Forwarded-For'u yalnızca güvenilen ağdan (compose iç ağı) kabul eder; böylece
///  - farklı istemciler aynı vekilden gelse de hız sınırında ayrı sayaçlara düşer,
///  - güvenilmeyen bir kaynağın uydurduğu X-Forwarded-For sınırı aşmaya yaramaz,
///  - giriş kaydı (iam.auth_session.ip_address) gerçek istemci adresini taşır.
/// TestServer'da bağlantı adresi olmadığı için <see cref="RemoteIpStartupFilter"/> kaynağı test başlığından kurar
/// (UseForwardedHeaders'tan önce çalışır, gerçek soketteki RemoteIpAddress'in yerini tutar).
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ForwardedClientIpTests : IAsyncLifetime, IDisposable
{
    private const string TrustedProxy = "172.30.0.5";       // compose ağındaki ön yüz nginx'i
    private const string TrustedNetwork = "172.30.0.0/24";
    private const string Password = "Proxy#Pass1234";
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret", "ForwardedHeaders__KnownNetworks__0"];

    private readonly MigratedDatabaseFixture _db;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    private string Username => $"proxy-{_suffix}";

    public ForwardedClientIpTests(MigratedDatabaseFixture db)
    {
        _db = db;
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", db.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        Environment.SetEnvironmentVariable("ForwardedHeaders__KnownNetworks__0", TrustedNetwork);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureServices(s => s.AddSingleton<IStartupFilter, RemoteIpStartupFilter>());
        });
    }

    public async Task InitializeAsync()
    {
        await using var conn = await _db.OpenAsync();
        var corp = Guid.NewGuid();
        await Exec(conn, "insert into core.corporation(id, code, legal_name, display_name) values (@id, @code, 'P', 'P')",
            ("id", corp), ("code", $"p-{_suffix}"));
        await Exec(conn,
            "insert into iam.user_account(corporation_id, username, email, full_name, password_hash, status) values (@c, @u, @e, @u, @h, 'active')",
            ("c", corp), ("u", Username), ("e", $"{Username}@example.test"), ("h", BCrypt.Net.BCrypt.HashPassword(Password, 4)));
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

    private async Task<HttpStatusCode> LoginAsync(string remoteIp, string? forwardedFor, string username, string password = "wrong")
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username, password }),
        };
        request.Headers.TryAddWithoutValidation(RemoteIpStartupFilter.Header, remoteIp);
        if (forwardedFor is not null) request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task Two_clients_behind_the_same_trusted_proxy_get_separate_rate_limit_counters()
    {
        // İstemci A sınırı doldurur (her denemede farklı kullanıcı adı: kimlik sayacı değil IP sayacı dolar).
        for (var i = 0; i < 10; i++)
            (await LoginAsync(TrustedProxy, "203.0.113.10", $"a{i}-{_suffix}")).Should().Be(HttpStatusCode.Unauthorized);
        (await LoginAsync(TrustedProxy, "203.0.113.10", $"a-x-{_suffix}")).Should().Be(HttpStatusCode.TooManyRequests);

        // Aynı vekilden gelen istemci B etkilenmez.
        (await LoginAsync(TrustedProxy, "203.0.113.20", $"b-{_suffix}")).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forged_forwarded_for_from_an_untrusted_source_does_not_bypass_the_limit()
    {
        const string attacker = "198.51.100.7";   // compose ağı dışında: başlığı yok sayılır
        for (var i = 0; i < 10; i++)
            (await LoginAsync(attacker, $"203.0.113.{100 + i}", $"f{i}-{_suffix}")).Should().Be(HttpStatusCode.Unauthorized);
        (await LoginAsync(attacker, "203.0.113.200", $"f-x-{_suffix}")).Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Successful_login_records_the_real_client_ip()
    {
        (await LoginAsync(TrustedProxy, "203.0.113.30", Username, Password)).Should().Be(HttpStatusCode.OK);

        await using var conn = await _db.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            select s.ip_address::text from iam.auth_session s join iam.user_account u on u.id = s.user_id
             where u.username = @u order by s.issued_at desc limit 1
            """, conn);
        cmd.Parameters.AddWithValue("u", Username);
        ((string?)await cmd.ExecuteScalarAsync()).Should().Be("203.0.113.30");
    }

    /// <summary>Test başlığındaki adresi bağlantının kaynağı yapar (ForwardedHeaders'tan önce).</summary>
    private sealed class RemoteIpStartupFilter : IStartupFilter
    {
        public const string Header = "X-Test-Remote-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((ctx, nextMiddleware) =>
            {
                if (ctx.Request.Headers.TryGetValue(Header, out var ip))
                    ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                return nextMiddleware(ctx);
            });
            next(app);
        };
    }
}
