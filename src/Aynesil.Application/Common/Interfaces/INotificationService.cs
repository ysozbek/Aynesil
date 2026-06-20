namespace Aynesil.Application.Common.Interfaces;

public record SendNotificationRequest(
    Guid RecipientUserId,
    string TemplateCode,
    IDictionary<string, string>? Variables = null,
    string[]? ForceChannels = null);

/// <summary>
/// Sends notifications via the configured channels (in-app, email, SMS, push).
/// Resolves the template and localized content; creates core.notification and
/// core.notification_delivery records within the same transaction.
/// Actual dispatch is performed by the notification job processor (Hangfire).
/// </summary>
public interface INotificationService
{
    Task SendAsync(SendNotificationRequest request, CancellationToken ct = default);
    Task MarkReadAsync(Guid notificationId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}
