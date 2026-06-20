namespace Aynesil.Shared.Constants;

/// <summary>
/// Platform permission codes. Convention: 'resource:action'.
/// These are seeded into iam.permission. Never authorize by role name — always use these codes.
/// New permissions are added here when new features are built and seeded via a migration.
/// </summary>
public static class Permissions
{
    // ── Corporation (admin) ──────────────────────────────────────────────
    public static class Corporation
    {
        public const string Read = "corporation:read";
        public const string Create = "corporation:create";
        public const string Update = "corporation:update";
        public const string Delete = "corporation:delete";
    }

    // ── Campus ──────────────────────────────────────────────────────────
    public static class Campus
    {
        public const string Read = "campus:read";
        public const string Create = "campus:create";
        public const string Update = "campus:update";
        public const string Delete = "campus:delete";
    }

    // ── IAM ─────────────────────────────────────────────────────────────
    public static class Users
    {
        public const string Read = "user:read";
        public const string Create = "user:create";
        public const string Update = "user:update";
        public const string Delete = "user:delete";
        public const string ResetPassword = "user:reset_password";
    }

    public static class Roles
    {
        public const string Read = "role:read";
        public const string Create = "role:create";
        public const string Update = "role:update";
        public const string Delete = "role:delete";
        public const string AssignPermission = "role:assign_permission";
    }

    // ── Reference Data ───────────────────────────────────────────────────
    public static class RefData
    {
        public const string Read = "ref_data:read";
        public const string Manage = "ref_data:manage";
    }

    // ── Settings ────────────────────────────────────────────────────────
    public static class Settings
    {
        public const string Read = "settings:read";
        public const string Manage = "settings:manage";
    }

    // ── Menu ────────────────────────────────────────────────────────────
    public static class Menu
    {
        public const string Read = "menu:read";
        public const string Manage = "menu:manage";
    }

    // ── Notifications ────────────────────────────────────────────────────
    public static class Notifications
    {
        public const string Read = "notification:read";
        public const string Send = "notification:send";
    }

    // ── Files ────────────────────────────────────────────────────────────
    public static class Files
    {
        public const string Read = "file:read";
        public const string Upload = "file:upload";
        public const string Delete = "file:delete";
    }

    // ── Reports ──────────────────────────────────────────────────────────
    public static class Reports
    {
        public const string Read = "report:read";
        public const string Run = "report:run";
        public const string Manage = "report:manage";
        public const string Export = "report:export";
    }

    // ── Audit ────────────────────────────────────────────────────────────
    public static class Audit
    {
        public const string Read = "audit:read";
    }

    // ── Integrations ─────────────────────────────────────────────────────
    public static class Integrations
    {
        public const string Read = "integration:read";
        public const string Manage = "integration:manage";
    }
}
