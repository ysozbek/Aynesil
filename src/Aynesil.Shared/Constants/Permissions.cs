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

    // ── Educators ─────────────────────────────────────────────────────────────
    public static class Educators
    {
        public const string Read             = "educator:read";
        public const string Create           = "educator:create";
        public const string Update           = "educator:update";
        public const string Delete           = "educator:delete";
        public const string ManageSpecialties = "educator:manage_specialties";
        public const string ManageCampuses   = "educator:manage_campuses";
        public const string ManageCertifications = "educator:manage_certifications";
        public const string ManageHierarchy  = "educator:manage_hierarchy";
    }

    // ── Programs (Education) ──────────────────────────────────────────────────
    public static class Programs
    {
        public const string Read   = "program:read";
        public const string Create = "program:create";
        public const string Update = "program:update";
        public const string Delete = "program:delete";
    }

    // ── Enrollments ───────────────────────────────────────────────────────────
    public static class Enrollments
    {
        public const string Read          = "enrollment:read";
        public const string Create        = "enrollment:create";
        public const string Update        = "enrollment:update";
        public const string ManagePrograms = "enrollment:manage_programs";
    }

    // ── Goal Library ──────────────────────────────────────────────────────────
    public static class GoalLibraries
    {
        public const string Read   = "goal_library:read";
        public const string Create = "goal_library:create";
        public const string Update = "goal_library:update";
        public const string Delete = "goal_library:delete";
    }

    // ── Goal Templates ────────────────────────────────────────────────────────
    public static class GoalTemplates
    {
        public const string Read      = "goal_template:read";
        public const string Create    = "goal_template:create";
        public const string Update    = "goal_template:update";
        public const string Delete    = "goal_template:delete";
        public const string Translate = "goal_template:translate";
    }

    // ── Student Goals ─────────────────────────────────────────────────────────
    public static class StudentGoals
    {
        public const string Read         = "student_goal:read";
        public const string Create       = "student_goal:create";
        public const string Update       = "student_goal:update";
        public const string Delete       = "student_goal:delete";
        public const string ChangeStatus = "student_goal:change_status";
    }

    // ── Goal Progress ─────────────────────────────────────────────────────────
    public static class GoalProgress
    {
        public const string Read   = "goal_progress:read";
        public const string Record = "goal_progress:record";
    }

    // ── Academic Periods ──────────────────────────────────────────────────────
    public static class AcademicPeriods
    {
        public const string Read   = "academic_period:read";
        public const string Manage = "academic_period:manage";
    }

    // ── Education Plans (BEP/IEP) ─────────────────────────────────────────────
    public static class EducationPlans
    {
        public const string Read          = "education_plan:read";
        public const string Create        = "education_plan:create";
        public const string Update        = "education_plan:update";
        public const string Delete        = "education_plan:delete";
        public const string Submit        = "education_plan:submit";
        public const string Approve       = "education_plan:approve";
        public const string Revise        = "education_plan:revise";
        public const string ManageGoals   = "education_plan:manage_goals";
        public const string AddReview     = "education_plan:add_review";
        public const string GuardianView  = "education_plan:guardian_view";
    }

    // ── Goal Reports ──────────────────────────────────────────────────────────
    public static class GoalReports
    {
        public const string Read   = "goal_report:read";
        public const string Export = "goal_report:export";
    }

    // ── Rooms ─────────────────────────────────────────────────────────────────
    public static class Rooms
    {
        public const string Read   = "room:read";
        public const string Create = "room:create";
        public const string Update = "room:update";
        public const string Delete = "room:delete";
    }

    // ── Sessions ──────────────────────────────────────────────────────────────
    public static class Sessions
    {
        public const string Read               = "session:read";
        public const string Create             = "session:create";
        public const string Update             = "session:update";
        public const string Delete             = "session:delete";
        public const string Reschedule         = "session:reschedule";
        public const string Complete           = "session:complete";
        public const string Cancel             = "session:cancel";
        public const string ManageParticipants = "session:manage_participants";
        public const string ManageEducators    = "session:manage_educators";
        public const string ManageGoals        = "session:manage_goals";
        public const string ManageCalendar     = "session:manage_calendar";
        public const string BulkGenerate       = "session:bulk_generate";
        public const string BulkCancel         = "session:bulk_cancel";
        public const string BulkReassign       = "session:bulk_reassign";
    }

    // ── Session Notes ─────────────────────────────────────────────────────────
    public static class SessionNotes
    {
        public const string Write  = "session_note:write";
        public const string Delete = "session_note:delete";
    }

    // ── Attendance ────────────────────────────────────────────────────────────
    public static class Attendance
    {
        public const string Read   = "attendance:read";
        public const string Record = "attendance:record";
    }

    // ── Makeup Requests ───────────────────────────────────────────────────────
    public static class MakeupRequests
    {
        public const string Read    = "makeup_request:read";
        public const string Request = "makeup_request:request";
        public const string Manage  = "makeup_request:manage";
    }

    // ── Package Definitions ───────────────────────────────────────────────────
    public static class PackageDefinitions
    {
        public const string Read   = "package_definition:read";
        public const string Create = "package_definition:create";
        public const string Update = "package_definition:update";
        public const string Delete = "package_definition:delete";
    }

    // ── Student Packages ──────────────────────────────────────────────────────
    public static class StudentPackages
    {
        public const string Read     = "student_package:read";
        public const string Purchase = "student_package:purchase";
        public const string Cancel   = "student_package:cancel";
    }

    // ── Credit Ledger ─────────────────────────────────────────────────────────
    public static class CreditLedger
    {
        public const string Read    = "credit_ledger:read";
        public const string Consume = "credit_ledger:consume";
        public const string Grant   = "credit_ledger:grant";
        public const string Adjust  = "credit_ledger:adjust";
    }

    // ── Invoices ──────────────────────────────────────────────────────────────
    public static class Invoices
    {
        public const string Read   = "invoice:read";
        public const string Create = "invoice:create";
        public const string Update = "invoice:update";
        public const string Void   = "invoice:void";
    }

    // ── Finance Payments ──────────────────────────────────────────────────────
    // Named FinancePayments to avoid confusion with the domain entity class Payment.
    public static class FinancePayments
    {
        public const string Read    = "finance_payment:read";
        public const string Record  = "finance_payment:record";
        public const string Capture = "finance_payment:capture";
    }

    // ── Refunds ───────────────────────────────────────────────────────────────
    public static class Refunds
    {
        public const string Read    = "refund:read";
        public const string Request = "refund:request";
        public const string Process = "refund:process";
    }

    // ── Discounts ─────────────────────────────────────────────────────────────
    public static class Discounts
    {
        public const string Read  = "discount:read";
        public const string Apply = "discount:apply";
    }

    // ── Scholarships ──────────────────────────────────────────────────────────
    public static class Scholarships
    {
        public const string Read   = "scholarship:read";
        public const string Grant  = "scholarship:grant";
        public const string Update = "scholarship:update";
    }

    // ── Promotions ────────────────────────────────────────────────────────────
    public static class Promotions
    {
        public const string Read   = "promotion:read";
        public const string Create = "promotion:create";
        public const string Update = "promotion:update";
    }

    // ── Finance Reports ───────────────────────────────────────────────────────
    public static class FinanceReports
    {
        public const string Read = "finance_report:read";
    }
}
