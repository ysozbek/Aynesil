using System.Security.Claims;
using Aynesil.Application.Common.Interfaces;

namespace Aynesil.Api.Services;

/// <summary>
/// Reads the current user's identity from the JWT claims in the HTTP context.
/// Resolved per-request as a scoped service.
/// Used by the AuditSaveChangesInterceptor (to set CreatedBy/UpdatedBy)
/// and the PerformanceBehavior pipeline.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub"), out var id)
            ? id : null;

    public string? UserName => User?.FindFirstValue("full_name");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public IReadOnlyDictionary<string, string> Claims =>
        User?.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value)
        ?? new Dictionary<string, string>();

    public bool HasPermission(string permissionCode) =>
        User?.FindAll("perm").Any(c => c.Value == permissionCode) == true;
}
