using Aynesil.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aynesil.Infrastructure.Services.Notifications;

/// <summary>
/// Geçici <see cref="INotificationService"/>: gerçek e-posta/SMS sağlayıcısı bağlanana kadar bildirimi
/// göndermek yerine yalnızca log'a yazar. Şablon kodu ve alıcı kimliği loglanır; değişken DEĞERLERİ
/// (parola sıfırlama/doğrulama token'ları gibi) güvenlik için asla loglanmaz, yalnızca adları yazılır.
/// Gerçek sağlayıcı (SMTP/outbox + Hangfire işçisi) eklendiğinde bu kayıt onunla değiştirilir.
/// </summary>
public sealed class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger) => _logger = logger;

    public Task SendAsync(SendNotificationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Notification NOT delivered (no provider configured): template={TemplateCode} recipient={RecipientUserId} variables=[{VariableNames}] channels=[{Channels}]",
            request.TemplateCode,
            request.RecipientUserId,
            string.Join(", ", request.Variables?.Keys ?? []),
            string.Join(", ", request.ForceChannels ?? []));
        return Task.CompletedTask;
    }

    public Task MarkReadAsync(Guid notificationId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(0);
}
