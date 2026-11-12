using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Y7: anonim auth uçlarında hız sınırı gerçek HTTP hattında. Sınır aşılınca 429 + Retry-After ve
/// standart ApiResponse zarfı (kod Error.TooManyRequests, mesaj Accept-Language'a göre tr/en) döner.
/// Her test kendi host'unu açar; sayaçlar host'a özgüdür.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthRateLimitTests : IDisposable
{
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public AuthRateLimitTests(MigratedDatabaseFixture db)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", db.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var key in EnvKeys) Environment.SetEnvironmentVariable(key, null);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body, string? RetryAfter)> PostAsync(
        string url, object body, string lang = "en")
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("Accept-Language", lang);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var json = text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (response.StatusCode, json, response.Headers.TryGetValues("Retry-After", out var v) ? v.Single() : null);
    }

    [Fact]
    public async Task Login_is_limited_to_10_attempts_per_minute_and_answers_429_in_the_api_envelope()
    {
        var username = $"nobody-{_suffix}";
        for (var i = 0; i < 10; i++)
        {
            var (status, _, _) = await PostAsync("/api/auth/login", new { username, password = "x" });
            status.Should().Be(HttpStatusCode.Unauthorized, $"deneme {i + 1} sınırın içinde");
        }

        var (en, enBody, retryAfter) = await PostAsync("/api/auth/login", new { username, password = "x" });
        var (tr, trBody, _) = await PostAsync("/api/auth/login", new { username, password = "x" }, "tr");

        en.Should().Be(HttpStatusCode.TooManyRequests);
        tr.Should().Be(HttpStatusCode.TooManyRequests);
        enBody.GetProperty("success").GetBoolean().Should().BeFalse();
        enBody.GetProperty("code").GetString().Should().Be("Error.TooManyRequests");
        trBody.GetProperty("code").GetString().Should().Be("Error.TooManyRequests");
        enBody.GetProperty("message").GetString().Should().Be("Too many attempts. Please wait a moment and try again.");
        trBody.GetProperty("message").GetString().Should().StartWith("Çok fazla deneme");
        enBody.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        int.Parse(retryAfter!).Should().BeInRange(1, 60);
    }

    [Fact]
    public async Task Login_attempts_against_many_accounts_from_one_client_are_limited()
    {
        for (var i = 0; i < 10; i++)
            (await PostAsync("/api/auth/login", new { username = $"spray{i}-{_suffix}", password = "Winter2026!" }))
                .Status.Should().Be(HttpStatusCode.Unauthorized);

        (await PostAsync("/api/auth/login", new { username = $"spray-new-{_suffix}", password = "Winter2026!" }))
            .Status.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Password_reset_request_is_limited_to_3_per_hour()
    {
        var email = $"target-{_suffix}@example.test";
        for (var i = 0; i < 3; i++)
            (await PostAsync("/api/auth/request-password-reset", new { email })).Status
                .Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var (status, body, retryAfter) = await PostAsync("/api/auth/request-password-reset", new { email });
        status.Should().Be(HttpStatusCode.TooManyRequests);
        body.GetProperty("code").GetString().Should().Be("Error.TooManyRequests");
        int.Parse(retryAfter!).Should().BeGreaterThan(60, "pencere bir saat");
    }

    [Fact]
    public async Task Password_reset_confirmation_is_limited_to_10_per_minute()
    {
        for (var i = 0; i < 10; i++)
            (await PostAsync("/api/auth/confirm-password-reset", new { token = $"bad-{i}-{_suffix}", newPassword = "N3w#Password!" }))
                .Status.Should().NotBe(HttpStatusCode.TooManyRequests);

        (await PostAsync("/api/auth/confirm-password-reset", new { token = $"bad-x-{_suffix}", newPassword = "N3w#Password!" }))
            .Status.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
