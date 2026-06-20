using Microsoft.AspNetCore.Authorization;

namespace Aynesil.Api.Authorization;

/// <summary>
/// Authorization requirement that checks for a specific permission code in the 'perm' JWT claims.
/// Usage: [Authorize(Policy = "student:read")]
/// Never use role names directly — always use permission codes.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }
    public PermissionRequirement(string permissionCode) => PermissionCode = permissionCode;
}
