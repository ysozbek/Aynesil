using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_template.
/// corporation_id is NULLABLE (NULL = platform-provided template).
/// Lifecycle is managed via is_active — there is no deleted_at column.
/// DB columns created_by and updated_by do NOT exist; AuditableEntity properties
/// are explicitly ignored to prevent EF from generating stale columns.
/// </summary>
public class AssessmentTemplateConfiguration : IEntityTypeConfiguration<AssessmentTemplate>
{
    public void Configure(EntityTypeBuilder<AssessmentTemplate> builder)
    {
        builder.ToTable("assessment_template", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        // ── Tenant scope (nullable — NULL = platform template) ────────────────
        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");

        // ── Domain fields ─────────────────────────────────────────────────────
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.TypeId).HasColumnName("type_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.ScoringModel).HasColumnName("scoring_model").HasMaxLength(20);
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        // ── Audit (columns that DO exist in the DB) ───────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // ── Columns absent from DB — must be ignored ──────────────────────────
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        // ── Navigations ───────────────────────────────────────────────────────
        builder.HasMany(x => x.Translations)
            .WithOne(t => t.Template)
            .HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Sections)
            .WithOne(s => s.Template)
            .HasForeignKey(s => s.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Unique constraint: NULLS NOT DISTINCT (PostgreSQL 15+) ────────────
        // Modelled as a regular unique index; NULLS NOT DISTINCT is handled at
        // the DDL level by the migration SQL. EF does not generate the DDL for
        // this table (schema-first), so this index registration is for metadata only.
        builder.HasIndex(x => new { x.CorporationId, x.Code, x.Version })
            .HasDatabaseName("assessment_template_corporation_id_code_version_key");

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CorporationId, x.IsActive })
            .HasDatabaseName("ix_assessment_template_corp_active");
    }
}
