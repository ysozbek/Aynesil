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

    // ── CRM / Leads ───────────────────────────────────────────────────────────
    public static class Leads
    {
        public const string Read = "lead:read";
        public const string Create = "lead:create";
        public const string Update = "lead:update";
        public const string Delete = "lead:delete";
        public const string Convert = "lead:convert";
        public const string Assign = "lead:assign";
    }

    public static class LeadActivities
    {
        public const string Read = "lead_activity:read";
        public const string Create = "lead_activity:create";
    }

    public static class Interviews
    {
        public const string Read = "interview:read";
        public const string Create = "interview:create";
        public const string Update = "interview:update";
        public const string Manage = "interview:manage";
    }

    // ── Assessment & Evaluation ───────────────────────────────────────────────

    public static class AssessmentTemplates
    {
        public const string Read       = "assessment_template:read";
        public const string Create     = "assessment_template:create";
        public const string Update     = "assessment_template:update";
        public const string Delete     = "assessment_template:delete";
        public const string Publish    = "assessment_template:publish";
        public const string Version    = "assessment_template:version";
    }

    public static class AssessmentSessions
    {
        public const string Read       = "assessment_session:read";
        public const string Create     = "assessment_session:create";
        public const string Update     = "assessment_session:update";
        public const string Delete     = "assessment_session:delete";
        public const string Start      = "assessment_session:start";
        public const string Complete   = "assessment_session:complete";
        public const string Cancel     = "assessment_session:cancel";
        public const string SubmitResponses = "assessment_session:submit_responses";
    }

    public static class AssessmentReports
    {
        public const string Read       = "assessment_report:read";
        public const string Create     = "assessment_report:create";
        public const string Update     = "assessment_report:update";
        public const string Finalize   = "assessment_report:finalize";
    }

    public static class ProgramRecommendations
    {
        public const string Read       = "program_recommendation:read";
        public const string Create     = "program_recommendation:create";
        public const string Update     = "program_recommendation:update";
    }

    // ── Students (Layer 2 — SPED) ─────────────────────────────────────────────
    // DB seed: student:read + student:write already in V6.
    // New granular codes below require migration V10__students_permissions_and_menu.sql.

    public static class Students
    {
        public const string Read         = "student:read";
        public const string Write        = "student:write";
        public const string Create       = "student:create";
        public const string Update       = "student:update";
        public const string Delete       = "student:delete";
        public const string ChangeStatus = "student:change_status";
    }

    public static class Guardians
    {
        public const string Read         = "guardian:read";
        public const string Create       = "guardian:create";
        public const string Update       = "guardian:update";
        public const string Delete       = "guardian:delete";
        public const string ManagePortal = "guardian:manage_portal";
    }

    public static class CaseNotes
    {
        public const string Read             = "case_note:read";
        public const string Create           = "case_note:create";
        public const string Update           = "case_note:update";
        public const string Delete           = "case_note:delete";
        public const string ReadConfidential = "case_note:read_confidential";
    }
}
