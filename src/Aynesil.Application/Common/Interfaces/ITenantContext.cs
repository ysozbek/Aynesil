namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Provides the current request's tenant context resolved from the JWT claims.
/// Implementations read CorporationId and CampusId from the authenticated user's claims
/// and supply them to the DbContext interceptor that sets the PostgreSQL GUCs required
/// for RLS (app.current_corporation_id, app.current_user_id).
/// </summary>
public interface ITenantContext
{
    /// <summary>Current corporation (tenant) ID. Null for unauthenticated or system requests.</summary>
    Guid? CorporationId { get; }

    /// <summary>Active campus (branch) scope. Null means corporation-wide access.</summary>
    Guid? CampusId { get; }

    /// <summary>Current user's preferred locale (e.g. 'tr', 'en').</summary>
    string? Locale { get; }

    /// <summary>Database timezone for the current corporation (e.g. 'Europe/Istanbul').</summary>
    string Timezone { get; }
}
