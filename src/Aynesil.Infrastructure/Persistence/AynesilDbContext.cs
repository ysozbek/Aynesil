using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Assessment.Entities;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Crm.Entities;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Finance.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Domain.Modules.Ref.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Aynesil platform.
///
/// Multi-tenancy strategy: Shared database, shared schema, tenant isolation via PostgreSQL RLS.
/// The application connects as the least-privilege 'aynesil_app' role. The
/// TenantConnectionInterceptor sets app.current_corporation_id and app.current_user_id
/// GUCs on every connection, activating the RLS tenant_isolation policy.
/// The table owner role ('aynesil_owner') bypasses RLS and is used only for migrations/seeding.
///
/// Global query filters provide a secondary application-level guard for soft-deleted rows.
/// Business modules (Layer 2) extend this context via partial configurations or register
/// their own DbContexts that use the same underlying connection.
/// </summary>
public class AynesilDbContext : DbContext, IAppDbContext
{
    public AynesilDbContext(DbContextOptions<AynesilDbContext> options) : base(options) { }

    // ── ref schema ─────────────────────────────────────────────────────────
    public DbSet<Locale> Locales => Set<Locale>();
    public DbSet<I18nMessage> I18nMessages => Set<I18nMessage>();
    public DbSet<RefType> RefTypes => Set<RefType>();
    public DbSet<RefValue> RefValues => Set<RefValue>();
    public DbSet<RefValueTranslation> RefValueTranslations => Set<RefValueTranslation>();
    public DbSet<RefValueTenantOverride> RefValueTenantOverrides => Set<RefValueTenantOverride>();

    // ── core schema ─────────────────────────────────────────────────────────
    public DbSet<Corporation> Corporations => Set<Corporation>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
    public DbSet<FileObject> FileObjects => Set<FileObject>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationTemplateTranslation> NotificationTemplateTranslations => Set<NotificationTemplateTranslation>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();
    public DbSet<ReportRun> ReportRuns => Set<ReportRun>();
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiValue> KpiValues => Set<KpiValue>();
    public DbSet<IntegrationProvider> IntegrationProviders => Set<IntegrationProvider>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<IntegrationLog> IntegrationLogs => Set<IntegrationLog>();

    // ── assessment schema ────────────────────────────────────────────────────
    public DbSet<AssessmentTemplate> AssessmentTemplates => Set<AssessmentTemplate>();
    public DbSet<AssessmentTemplateTranslation> AssessmentTemplateTranslations => Set<AssessmentTemplateTranslation>();
    public DbSet<AssessmentSection> AssessmentSections => Set<AssessmentSection>();
    public DbSet<AssessmentItem> AssessmentItems => Set<AssessmentItem>();
    public DbSet<AssessmentSession> AssessmentSessions => Set<AssessmentSession>();
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
    public DbSet<AssessmentReport> AssessmentReports => Set<AssessmentReport>();
    public DbSet<ProgramRecommendation> ProgramRecommendations => Set<ProgramRecommendation>();

    // ── crm schema ──────────────────────────────────────────────────────────
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadStatusHistory> LeadStatusHistories => Set<LeadStatusHistory>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<Interview> Interviews => Set<Interview>();

    // ── educators schema ────────────────────────────────────────────────────
    public DbSet<Educator> Educators => Set<Educator>();
    public DbSet<EducatorCampus> EducatorCampuses => Set<EducatorCampus>();
    public DbSet<EducatorSpecialty> EducatorSpecialties => Set<EducatorSpecialty>();
    public DbSet<EducatorCertification> EducatorCertifications => Set<EducatorCertification>();
    public DbSet<EducatorHierarchy> EducatorHierarchies => Set<EducatorHierarchy>();

    // ── education schema ────────────────────────────────────────────────────
    public DbSet<EducationProgram> EducationPrograms => Set<EducationProgram>();
    public DbSet<ProgramTranslation> ProgramTranslations => Set<ProgramTranslation>();
    public DbSet<ProgramService> ProgramServices => Set<ProgramService>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<StudentProgram> StudentPrograms => Set<StudentProgram>();

    // goals & plan sub-domain
    public DbSet<GoalLibrary> GoalLibraries => Set<GoalLibrary>();
    public DbSet<GoalTemplate> GoalTemplates => Set<GoalTemplate>();
    public DbSet<GoalTemplateTranslation> GoalTemplateTranslations => Set<GoalTemplateTranslation>();
    public DbSet<StudentGoal> StudentGoals => Set<StudentGoal>();
    public DbSet<GoalProgress> GoalProgressRecords => Set<GoalProgress>();
    public DbSet<AcademicPeriod> AcademicPeriods => Set<AcademicPeriod>();
    public DbSet<EducationPlan> EducationPlans => Set<EducationPlan>();
    public DbSet<EducationPlanGoal> EducationPlanGoals => Set<EducationPlanGoal>();
    public DbSet<EducationPlanReview> EducationPlanReviews => Set<EducationPlanReview>();
    public DbSet<EducationPlanApproval> EducationPlanApprovals => Set<EducationPlanApproval>();
    public DbSet<EducationPlanRevision> EducationPlanRevisions => Set<EducationPlanRevision>();

    // ── scheduling schema ───────────────────────────────────────────────────
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<CalendarEntry> CalendarEntries => Set<CalendarEntry>();
    public DbSet<RecurringSchedule> RecurringSchedules => Set<RecurringSchedule>();
    public DbSet<RecurrenceException> RecurrenceExceptions => Set<RecurrenceException>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();
    public DbSet<SessionEducator> SessionEducators => Set<SessionEducator>();
    public DbSet<SessionGoal> SessionGoals => Set<SessionGoal>();
    public DbSet<SessionNote> SessionNotes => Set<SessionNote>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<MakeupRequest> MakeupRequests => Set<MakeupRequest>();

    // ── finance schema ──────────────────────────────────────────────────────
    public DbSet<PackageDefinition> PackageDefinitions => Set<PackageDefinition>();
    public DbSet<StudentPackage> StudentPackages => Set<StudentPackage>();
    public DbSet<CreditLedger> CreditLedgerEntries => Set<CreditLedger>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    // ── students schema ─────────────────────────────────────────────────────
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentStatusHistory> StudentStatusHistories => Set<StudentStatusHistory>();
    public DbSet<StudentCampus> StudentCampuses => Set<StudentCampus>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<DevelopmentalProfile> DevelopmentalProfiles => Set<DevelopmentalProfile>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<MedicalReport> MedicalReports => Set<MedicalReport>();
    public DbSet<DevelopmentReport> DevelopmentReports => Set<DevelopmentReport>();
    public DbSet<ExternalInstitutionReport> ExternalInstitutionReports => Set<ExternalInstitutionReport>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();
    public DbSet<GuardianPortalAccess> GuardianPortalAccesses => Set<GuardianPortalAccess>();
    public DbSet<StudentCareAssignment> StudentCareAssignments => Set<StudentCareAssignment>();

    // ── iam schema ──────────────────────────────────────────────────────────
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<IdentityProvider> IdentityProviders => Set<IdentityProvider>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemTranslation> MenuItemTranslations => Set<MenuItemTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly.
        // Each configuration that needs a soft-delete filter applies HasQueryFilter explicitly.
        // We intentionally do NOT use a reflection-based global loop because several entities
        // inherit SoftDeleteEntity but have Ignored DeletedAt columns (the DDL for those tables
        // does not include deleted_at). Explicit filters in configurations are safer.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AynesilDbContext).Assembly);
    }
}
