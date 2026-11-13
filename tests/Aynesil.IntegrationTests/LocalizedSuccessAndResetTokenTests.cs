using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Y13 gerçek HTTP hattında: başarı mesajları MsgKeys ile kodlanır (message tr/en, code anahtar);
/// geçersiz sıfırlama token'ı 401 değil, 422 + Auth.InvalidResetToken döner.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class LocalizedSuccessAndResetTokenTests : IDisposable
{
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly WebApplicationFactory<Program> _factory;

    public LocalizedSuccessAndResetTokenTests(MigratedDatabaseFixture db)
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

    private async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(string url, object body, string lang)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("Accept-Language", lang);
        using var response = await _factory.CreateClient().SendAsync(request);
        return (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone());
    }

    [Fact]
    public async Task Invalid_reset_token_answers_422_with_a_code_in_both_languages_not_401()
    {
        var payload = new { token = "does-not-exist", newPassword = "Str0ng!Passw0rd#2026" };
        var (statusEn, en) = await PostAsync("/api/auth/confirm-password-reset", payload, "en");
        var (statusTr, tr) = await PostAsync("/api/auth/confirm-password-reset", payload, "tr");

        statusEn.Should().Be(HttpStatusCode.UnprocessableEntity);
        statusTr.Should().Be(HttpStatusCode.UnprocessableEntity);
        en.GetProperty("success").GetBoolean().Should().BeFalse();
        en.GetProperty("code").GetString().Should().Be("Auth.InvalidResetToken");
        tr.GetProperty("code").GetString().Should().Be("Auth.InvalidResetToken");
        en.GetProperty("message").GetString().Should().Contain("invalid or has expired");
        tr.GetProperty("message").GetString().Should().Contain("geçersiz").And.NotBe(en.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Success_message_is_localized_and_carries_its_code()
    {
        var payload = new { email = $"nobody-{Guid.NewGuid():N}@example.test" };
        var (statusEn, en) = await PostAsync("/api/auth/request-password-reset", payload, "en");
        var (statusTr, tr) = await PostAsync("/api/auth/request-password-reset", payload, "tr");

        statusEn.Should().Be(HttpStatusCode.OK);
        statusTr.Should().Be(HttpStatusCode.OK);
        en.GetProperty("success").GetBoolean().Should().BeTrue();
        en.GetProperty("code").GetString().Should().Be("Success.PasswordResetRequested");
        tr.GetProperty("code").GetString().Should().Be("Success.PasswordResetRequested");
        en.GetProperty("message").GetString().Should().StartWith("If an account");
        tr.GetProperty("message").GetString().Should().StartWith("Bu e-posta");
    }
}
