using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Aynesil.Api.Authorization;

/// <summary>
/// Custom IAuthorizationPolicyProvider that dynamically creates policies for permission codes.
///
/// Problem: [HasPermission("campus:read")] sets Policy = "campus:read" on the Authorize attribute.
/// ASP.NET Core's DefaultAuthorizationPolicyProvider looks for a *registered* named policy
/// called "campus:read" and throws InvalidOperationException when not found.
///
/// Solution: This provider intercepts policy lookups for strings containing ':' (all our
/// permission codes use code:action format) and builds an AuthorizationPolicy on the fly
/// with the matching PermissionRequirement. PermissionAuthorizationHandler then evaluates
/// that requirement against the JWT 'perm' claims.
///
/// All other policy names (including the default policy) fall through to the default provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Permission codes always use "resource:action" format (e.g. "campus:read").
        // Build a transient policy with PermissionRequirement instead of a named policy lookup.
        if (policyName.Contains(':'))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
