using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Domain.Modules.Ref.Entities;
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
public class AynesilDbContext : DbContext
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
