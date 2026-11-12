using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Gerçek HTTP hattı (RequestCulture → ExceptionMiddleware → MediatR ValidationBehavior): aynı geçersiz istek
/// Accept-Language'a göre farklı dilde mesaj döner; hata kodu ve errors/errorDetails yapısı dilden bağımsızdır.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class LocalizedErrorResponseTests : IDisposable
{
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly WebApplicationFactory<Program> _factory;

    public LocalizedErrorResponseTests(MigratedDatabaseFixture db)
    {
        // Program.cs yapılandırmayı build sırasında okuduğu için ortam değişkenleri en güvenilir yoldur.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", db.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");   // bellek içi önbellek
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        // Production: Development'taki ValidateOnBuild, kaydı olmayan INotificationService'i (Register/PasswordReset
        // handler'ları) başlangıçta hata yapar; bu test o uçlara dokunmaz.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Production"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var key in EnvKeys) Environment.SetEnvironmentVariable(key, null);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> PostLoginAsync(string json, string? acceptLanguage)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
        };
        if (acceptLanguage is not null) request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, JsonDocument.Parse(text).RootElement.Clone());
    }

    private const string EmptyCredentials = """{ "username": "", "password": "" }""";

    [Fact]
    public async Task Same_invalid_request_returns_tr_and_en_messages_with_the_same_codes()
    {
        var (statusTr, tr) = await PostLoginAsync(EmptyCredentials, "tr");
        var (statusEn, en) = await PostLoginAsync(EmptyCredentials, "en");

        statusTr.Should().Be(HttpStatusCode.UnprocessableEntity);
        statusEn.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Üst düzey mesaj ve alan mesajları dile göre değişir.
        tr.GetProperty("message").GetString().Should().Be("Doğrulama hatası.");
        en.GetProperty("message").GetString().Should().Be("Validation failed.");

        var trUsername = tr.GetProperty("errors").GetProperty("username")[0].GetString()!;
        var enUsername = en.GetProperty("errors").GetProperty("username")[0].GetString()!;
        trUsername.Should().Contain("Kullanıcı adı").And.NotBe(enUsername);
        enUsername.Should().Contain("Username");

        // Kodlar ve yapı dilden bağımsız: errors (alan → string[]) korunur, errorDetails kod taşır.
        tr.GetProperty("code").GetString().Should().Be("Error.ValidationFailed").And.Be(en.GetProperty("code").GetString());
        var trDetail = tr.GetProperty("errorDetails").GetProperty("username")[0];
        var enDetail = en.GetProperty("errorDetails").GetProperty("username")[0];
        trDetail.GetProperty("code").GetString().Should().Be(enDetail.GetProperty("code").GetString());
        trDetail.GetProperty("message").GetString().Should().Be(trUsername);
        enDetail.GetProperty("message").GetString().Should().Be(enUsername);
        tr.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]                 // başlık yok → varsayılan tr
    [InlineData("de-DE,fr;q=0.8")]     // desteklenmeyen diller → tr
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8")]
    public async Task Unsupported_or_missing_Accept_Language_falls_back_to_turkish(string? header)
    {
        var (_, body) = await PostLoginAsync(EmptyCredentials, header);
        body.GetProperty("message").GetString().Should().Be("Doğrulama hatası.");
    }

    [Fact]
    public async Task Accept_Language_q_values_pick_the_preferred_supported_language()
    {
        var (_, body) = await PostLoginAsync(EmptyCredentials, "tr;q=0.3,en;q=0.9");
        body.GetProperty("message").GetString().Should().Be("Validation failed.");
    }

    [Fact]
    public async Task Model_binding_errors_are_localized_with_a_code()
    {
        const string malformed = """{ "username": 123 }""";   // string alana sayı → ModelState hatası (400)
        var (statusTr, tr) = await PostLoginAsync(malformed, "tr");
        var (statusEn, en) = await PostLoginAsync(malformed, "en");

        statusTr.Should().Be(HttpStatusCode.BadRequest);
        statusEn.Should().Be(HttpStatusCode.BadRequest);
        tr.GetProperty("message").GetString().Should().Be("Gönderilen veri geçersiz.");
        en.GetProperty("message").GetString().Should().Be("The submitted data is invalid.");
        tr.GetProperty("code").GetString().Should().Be("Error.InvalidRequest");
        en.GetProperty("code").GetString().Should().Be("Error.InvalidRequest");
        tr.TryGetProperty("errorDetails", out _).Should().BeTrue();
    }
}
