namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the currently authenticated user.
/// Resolved from JWT claims in the HTTP context.
/// Used by the AuditSaveChangesInterceptor to set CreatedBy/UpdatedBy.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>User account ID from iam.user_account.id. Null when unauthenticated.</summary>
    Guid? UserId { get; }

    /// <summary>Display name for logging purposes.</summary>
    string? UserName { get; }

    /// <summary>True when the request is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>All claims carried by the current user's token.</summary>
    IReadOnlyDictionary<string, string> Claims { get; }

    /// <summary>Returns true if the user has the given permission code.</summary>
    bool HasPermission(string permissionCode);
}
