using Microsoft.AspNetCore.Authorization;

namespace Aynesil.Api.Authorization;

/// <summary>
/// Evaluates PermissionRequirement by checking the 'perm' claims in the JWT.
/// The JWT token includes all permission codes assigned to the user via their roles.
/// Cache-warm: permissions are embedded in the token so no DB/Redis lookup per request.
/// On role/permission change: issue new tokens (or use shorter access token expiry).
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permClaims = context.User.FindAll("perm").Select(c => c.Value);

        if (permClaims.Contains(requirement.PermissionCode))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
