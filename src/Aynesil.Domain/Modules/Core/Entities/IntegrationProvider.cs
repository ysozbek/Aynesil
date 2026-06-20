namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.integration_provider.
/// Platform catalog of available external providers (email, SMS, payment, streaming, etc.).
/// Not tenant-scoped — providers are registered by the platform team.
/// Tenants create IntegrationConnection records to activate a provider for their corporation.
/// Follows the Adapter pattern: each provider has a corresponding implementation class.
/// </summary>
public class IntegrationProvider : BaseEntity
{
    /// <summary>Stable machine code, e.g. 'sendgrid', 'twilio', 'iyzico', 'zoom'.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>FK to ref_value(integration_kind): 'email', 'sms', 'payment', 'streaming', 'idp'.</summary>
    public Guid? KindId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<IntegrationConnection> Connections { get; set; } = [];
}
