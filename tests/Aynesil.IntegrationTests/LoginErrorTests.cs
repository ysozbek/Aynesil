using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Giriş hataları gerçek veritabanı ve HTTP hattı üzerinden: kodlu 401 yanıtları, tr/en mesajlar ve
/// kullanıcı varlığının sızmaması (kilitli/devre dışı bilgisi yalnızca parola doğruysa gösterilir).
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class LoginErrorTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Correct#Pass1";
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly MigratedDatabaseFixture _db;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    private string Active => $"active-{_suffix}";
    private string Disabled => $"disabled-{_suffix}";
    private string Locked => $"locked-{_suffix}";

    public LoginErrorTests(MigratedDatabaseFixture db)
    {
        _db = db;
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", db.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        // Development: DI doğrulaması açık; eksik kayıt varsa host burada açılmaz.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
    }

    public async Task InitializeAsync()
    {
        await using var conn = await _db.OpenAsync();
        var corp = Guid.NewGuid();
        await Exec(conn, "insert into core.corporation(id, code, legal_name, display_name) values (@id, @code, 'T', 'T')",
            ("id", corp), ("code", $"t-{_suffix}"));

        var hash = BCrypt.Net.BCrypt.HashPassword(Password, 4);
        foreach (var (name, status) in new[] { (Active, "active"), (Disabled, "disabled"), (Locked, "active") })
            await Exec(conn,
                "insert into iam.user_account(corporation_id, username, full_name, password_hash, status) values (@c, @u, @u, @h, @s)",
                ("c", corp), ("u", name), ("h", hash), ("s", status));
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

    private async Task<(HttpStatusCode Status, JsonElement Body)> LoginAsync(string username, string password, string lang = "en")
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username, password }),
        };
        request.Headers.TryAddWithoutValidation("Accept-Language", lang);
        using var response = await client.SendAsync(request);
        return (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone());
    }

    private static string Code(JsonElement body) => body.GetProperty("code").GetString()!;
    private static string Message(JsonElement body) => body.GetProperty("message").GetString()!;

    [Fact]
    public async Task Correct_credentials_succeed()
    {
        var (status, body) = await LoginAsync(Active, Password);
        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Unknown_user_and_wrong_password_are_indistinguishable()
    {
        var (s1, unknown) = await LoginAsync($"nobody-{_suffix}", "whatever");
        var (s2, wrong) = await LoginAsync(Active, "wrong-password");

        s1.Should().Be(HttpStatusCode.Unauthorized);
        s2.Should().Be(HttpStatusCode.Unauthorized);
        Code(unknown).Should().Be("Auth.InvalidCredentials").And.Be(Code(wrong));
        Message(unknown).Should().Be(Message(wrong)).And.Be("Invalid username or password.");
    }

    [Fact]
    public async Task Messages_follow_Accept_Language()
    {
        var (_, tr) = await LoginAsync(Active, "wrong-password", "tr");
        var (_, en) = await LoginAsync(Active, "wrong-password", "en");

        Message(tr).Should().Be("Kullanıcı adı veya parola hatalı.");
        Message(en).Should().Be("Invalid username or password.");
        Code(tr).Should().Be(Code(en));
    }

    [Fact]
    public async Task Disabled_status_is_revealed_only_when_the_password_is_correct()
    {
        var (_, wrongPassword) = await LoginAsync(Disabled, "wrong-password");
        var (status, correctPassword) = await LoginAsync(Disabled, Password);
        var (_, tr) = await LoginAsync(Disabled, Password, "tr");

        Code(wrongPassword).Should().Be("Auth.InvalidCredentials");   // devre dışı olduğu sızmaz
        status.Should().Be(HttpStatusCode.Unauthorized);
        Code(correctPassword).Should().Be("Auth.AccountDisabled");
        Message(correctPassword).Should().Contain("disabled");
        Message(tr).Should().Contain("devre dışı");
    }

    [Fact]
    public async Task Lockout_is_revealed_only_when_the_password_is_correct()
    {
        for (var i = 0; i < 5; i++)
        {
            var (_, attempt) = await LoginAsync(Locked, "wrong-password");
            Code(attempt).Should().Be("Auth.InvalidCredentials", "kilitleyen deneme dahil hepsi aynı yanıtı verir");
        }

        var (_, wrongWhileLocked) = await LoginAsync(Locked, "wrong-password");
        var (status, correctWhileLocked) = await LoginAsync(Locked, Password);

        Code(wrongWhileLocked).Should().Be("Auth.InvalidCredentials");
        status.Should().Be(HttpStatusCode.Unauthorized);
        Code(correctWhileLocked).Should().Be("Auth.AccountLocked");
        Message(correctWhileLocked).Should().Contain("locked");
    }
}
