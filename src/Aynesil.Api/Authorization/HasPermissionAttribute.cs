using Microsoft.AspNetCore.Authorization;

namespace Aynesil.Api.Authorization;

/// <summary>
/// Convenience attribute for permission-based authorization.
/// Usage: [HasPermission(Permissions.Students.Read)]
/// This generates an AuthorizationPolicy with a PermissionRequirement on the fly.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
        : base(permissionCode)
    {
    }
}
