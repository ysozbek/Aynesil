using Aynesil.Api;
using Aynesil.Application;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aynesil.Tests.Api;

/// <summary>
/// Development'taki varsayılan ayarlarla (ValidateOnBuild + ValidateScopes) tüm DI kayıtlarını doğrular:
/// eksik bir bağımlılık (ör. kaydı olmayan INotificationService) veya ömür uyumsuzluğu (singleton → scoped)
/// artık yalnızca çalışma anında, ilgili uç çağrıldığında değil, burada hemen görünür.
/// Host başlatılmaz (Hangfire/Redis/DB bağlantısı yok); yalnızca servis sağlayıcı kurulur.
/// </summary>
public class ServiceRegistrationTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]   // Production'da doğrulama varsayılan kapalıdır; burada zorla açılır
    public void All_service_registrations_resolve_with_ValidateOnBuild_and_ValidateScopes(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=u;Password=p",
            ["Jwt:Issuer"] = "https://test", ["Jwt:Audience"] = "test",
            ["Jwt:Secret"] = "unit-test-secret-unit-test-secret-0123456789",
            ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
        });
        builder.Host.UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; });
        builder.Services.AddAynesilApi(builder.Configuration);

        var act = () => builder.Build();

        act.Should().NotThrow("her kayıtlı servis ve MediatR handler'ı çözümlenebilmeli");
    }

    [Fact]
    public void Notification_service_is_registered()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=u;Password=p",
            ["Jwt:Secret"] = "unit-test-secret-unit-test-secret-0123456789",
        });
        builder.Services.AddAynesilApi(builder.Configuration);
        using var app = builder.Build();
        using var scope = app.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.INotificationService>();

        service.Should().BeOfType<Infrastructure.Services.Notifications.LoggingNotificationService>();
    }

    [Fact]
    public async Task Logging_notification_service_never_logs_variable_values()
    {
        var log = new CapturingLogger();
        var service = new Infrastructure.Services.Notifications.LoggingNotificationService(log);

        await service.SendAsync(new Application.Common.Interfaces.SendNotificationRequest(
            Guid.NewGuid(), "user.password_reset", new Dictionary<string, string> { ["token"] = "SECRET-RESET-TOKEN" }));

        log.Messages.Should().ContainSingle().Which.Should()
            .Contain("user.password_reset").And.Contain("token").And.NotContain("SECRET-RESET-TOKEN");
    }

    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<Infrastructure.Services.Notifications.LoggingNotificationService>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
