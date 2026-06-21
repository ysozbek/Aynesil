using Aynesil.Domain.Modules.Assessment.Entities;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Crm.Entities;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Domain.Modules.Ref.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Application katmanının veritabanına erişim sözleşmesi.
/// Infrastructure'a (AynesilDbContext) doğrudan bağımlılığı ortadan kaldırır.
/// Clean Architecture dependency kuralını korur: Application → Infrastructure değil,
/// Application ← Infrastructure (Infrastructure implement eder).
///
/// EF Core DbSet'leri ve SaveChangesAsync burada tanımlanır.
/// Tüm CQRS handler'ları bu interface'i kullanır.
/// </summary>
public interface IAppDbContext
{
    // ── ref schema ─────────────────────────────────────────────────────────
    DbSet<Locale> Locales { get; }
    DbSet<I18nMessage> I18nMessages { get; }
    DbSet<RefType> RefTypes { get; }
    DbSet<RefValue> RefValues { get; }
    DbSet<RefValueTranslation> RefValueTranslations { get; }
    DbSet<RefValueTenantOverride> RefValueTenantOverrides { get; }

    // ── core schema ─────────────────────────────────────────────────────────
    DbSet<Corporation> Corporations { get; }
    DbSet<Campus> Campuses { get; }
    DbSet<SettingDefinition> SettingDefinitions { get; }
    DbSet<SettingValue> SettingValues { get; }
    DbSet<FileObject> FileObjects { get; }
    DbSet<FileAttachment> FileAttachments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<AppNotification> Notifications { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<ReportDefinition> ReportDefinitions { get; }
    DbSet<ReportRun> ReportRuns { get; }
    DbSet<KpiDefinition> KpiDefinitions { get; }
    DbSet<KpiValue> KpiValues { get; }
    DbSet<IntegrationProvider> IntegrationProviders { get; }
    DbSet<IntegrationConnection> IntegrationConnections { get; }
    DbSet<OutboxEvent> OutboxEvents { get; }

    // ── assessment schema ────────────────────────────────────────────────────
    DbSet<AssessmentTemplate> AssessmentTemplates { get; }
    DbSet<AssessmentTemplateTranslation> AssessmentTemplateTranslations { get; }
    DbSet<AssessmentSection> AssessmentSections { get; }
    DbSet<AssessmentItem> AssessmentItems { get; }
    DbSet<AssessmentSession> AssessmentSessions { get; }
    DbSet<AssessmentResponse> AssessmentResponses { get; }
    DbSet<AssessmentReport> AssessmentReports { get; }
    DbSet<ProgramRecommendation> ProgramRecommendations { get; }

    // ── crm schema ──────────────────────────────────────────────────────────
    DbSet<Lead> Leads { get; }
    DbSet<LeadStatusHistory> LeadStatusHistories { get; }
    DbSet<LeadActivity> LeadActivities { get; }
    DbSet<Interview> Interviews { get; }

    // ── educators schema ────────────────────────────────────────────────────
    DbSet<Educator> Educators { get; }
    DbSet<EducatorCampus> EducatorCampuses { get; }
    DbSet<EducatorSpecialty> EducatorSpecialties { get; }
    DbSet<EducatorCertification> EducatorCertifications { get; }
    DbSet<EducatorHierarchy> EducatorHierarchies { get; }

    // ── education schema ────────────────────────────────────────────────────
    DbSet<EducationProgram> EducationPrograms { get; }
    DbSet<ProgramTranslation> ProgramTranslations { get; }
    DbSet<ProgramService> ProgramServices { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<StudentProgram> StudentPrograms { get; }

    // ── students schema ─────────────────────────────────────────────────────
    DbSet<Student> Students { get; }
    DbSet<StudentStatusHistory> StudentStatusHistories { get; }
    DbSet<StudentCampus> StudentCampuses { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<StudentGuardian> StudentGuardians { get; }
    DbSet<EmergencyContact> EmergencyContacts { get; }
    DbSet<DevelopmentalProfile> DevelopmentalProfiles { get; }
    DbSet<Diagnosis> Diagnoses { get; }
    DbSet<MedicalReport> MedicalReports { get; }
    DbSet<DevelopmentReport> DevelopmentReports { get; }
    DbSet<ExternalInstitutionReport> ExternalInstitutionReports { get; }
    DbSet<CaseNote> CaseNotes { get; }
    DbSet<GuardianPortalAccess> GuardianPortalAccesses { get; }

    // ── iam schema ──────────────────────────────────────────────────────────
    DbSet<UserAccount> UserAccounts { get; }
    DbSet<AuthSession> AuthSessions { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<MenuItemTranslation> MenuItemTranslations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// EF Core DatabaseFacade — raw SQL, transactions, connection management.
    /// SECURITY DEFINER fonksiyonları için SqlQueryRaw/ExecuteSqlRawAsync kullanılır.
    /// </summary>
    DatabaseFacade Database { get; }
}
