namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.integration_connection.
/// A tenant's activation of an integration provider with their credentials.
/// Config contains non-secret values (webhook URLs, sender addresses, etc.).
/// SecretRef is a reference into the secret manager (AWS Secrets Manager, Vault) —
/// never the raw secret itself.
/// </summary>
public class IntegrationConnection : TenantEntity
{
    public Guid ProviderId { get; set; }

    /// <summary>Non-secret configuration as JSON (API endpoints, sender email, etc.).</summary>
    public string Config { get; set; } = "{}";

    /// <summary>
    /// Reference path into the secret manager, e.g. 'arn:aws:secretsmanager:eu-west-1:...:secret:corp/akran/sendgrid'.
    /// The Infrastructure layer resolves this at runtime; the value is never logged or serialized.
    /// </summary>
    public string? SecretRef { get; set; }

    public bool IsActive { get; set; } = true;

    public IntegrationProvider? Provider { get; set; }
}
