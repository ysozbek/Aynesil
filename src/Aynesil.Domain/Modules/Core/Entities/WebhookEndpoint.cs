namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.webhook_endpoint.
/// A tenant-configured URL that receives platform events via HTTP POST.
/// EventTypes is an array of event codes this endpoint subscribes to.
/// SecretRef is used to sign webhook payloads for receiver verification.
/// </summary>
public class WebhookEndpoint : TenantEntity
{
    public string Url { get; set; } = string.Empty;

    /// <summary>Array of subscribed event type codes, e.g. {"session.completed", "payment.received"}.</summary>
    public string[] EventTypes { get; set; } = [];

    /// <summary>Reference to the signing secret in the secret manager.</summary>
    public string? SecretRef { get; set; }

    public bool IsActive { get; set; } = true;
}
