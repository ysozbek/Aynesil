namespace Aynesil.Shared.Constants;

/// <summary>
/// Centralized cache key templates. All tenant-scoped keys are prefixed with corporation_id
/// to prevent cross-tenant reads. Use CacheKeys.ForTenant() to build scoped keys.
/// </summary>
public static class CacheKeys
{
    private const string Sep = ":";

    public static string ForTenant(Guid corporationId, string key) =>
        $"corp{Sep}{corporationId}{Sep}{key}";

    public static string ForUser(Guid userId, string key) =>
        $"user{Sep}{userId}{Sep}{key}";

    // ── Permissions ──────────────────────────────────────────────────────
    public static string UserPermissions(Guid corporationId, Guid userId) =>
        ForTenant(corporationId, $"perms{Sep}{userId}");

    // ── Menus ────────────────────────────────────────────────────────────
    public static string MenuTree(Guid corporationId, string locale) =>
        ForTenant(corporationId, $"menu{Sep}{locale}");

    // ── Settings ─────────────────────────────────────────────────────────
    public static string CorporationSettings(Guid corporationId) =>
        ForTenant(corporationId, "settings");

    public static string UserSettings(Guid corporationId, Guid userId) =>
        ForTenant(corporationId, $"settings{Sep}user{Sep}{userId}");

    // ── Reference Data ───────────────────────────────────────────────────
    public static string RefValues(Guid? corporationId, string typeCode) =>
        corporationId.HasValue
            ? ForTenant(corporationId.Value, $"ref{Sep}{typeCode}")
            : $"ref{Sep}global{Sep}{typeCode}";

    // ── Localization ─────────────────────────────────────────────────────
    public static string Translations(Guid? corporationId, string locale, string @namespace) =>
        corporationId.HasValue
            ? ForTenant(corporationId.Value, $"i18n{Sep}{locale}{Sep}{@namespace}")
            : $"i18n{Sep}global{Sep}{locale}{Sep}{@namespace}";

    // ── Tenant prefix for bulk invalidation ──────────────────────────────
    public static string TenantPrefix(Guid corporationId) => $"corp{Sep}{corporationId}";

    // ── Auth one-time tokens (TTL-backed, stored as token-hash → payload) ─
    public static string EmailVerificationToken(string tokenHash) =>
        $"auth{Sep}email-verify{Sep}{tokenHash}";

    public static string PasswordResetToken(string tokenHash) =>
        $"auth{Sep}pwd-reset{Sep}{tokenHash}";

    public static string AccountLockout(Guid userId) =>
        ForUser(userId, "lockout");
}
