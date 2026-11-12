namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Helpers that resolve server-trusted identifiers (tenant, acting user) for command handlers.
/// Commands must never accept CorporationId or actor ids (CreatedBy, GrantedBy, ...) from the
/// client; handlers read them from the JWT-backed request context instead.
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>Returns the current corporation id or throws (mapped to 401) when there is no tenant.</summary>
    public static Guid RequireCorporationId(this ITenantContext tenant) =>
        tenant.CorporationId
        ?? throw new UnauthorizedAccessException("Tenant context is required.");

    /// <summary>Returns the current user id or throws (mapped to 401) when unauthenticated.</summary>
    public static Guid RequireUserId(this ICurrentUserService currentUser) =>
        currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user is required.");
}
