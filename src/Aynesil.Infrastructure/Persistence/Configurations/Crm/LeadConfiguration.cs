using Aynesil.Domain.Modules.Crm.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Crm;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("lead", schema: "crm");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        // ── Tenant scope ──────────────────────────────────────────────────────
        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");

        // ── Reference data FK columns ─────────────────────────────────────────
        builder.Property(x => x.SourceId).HasColumnName("source_id");
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.PipelineStageId).HasColumnName("pipeline_stage_id");

        // ── Domain fields ─────────────────────────────────────────────────────
        builder.Property(x => x.ChildName).HasColumnName("child_name");
        builder.Property(x => x.ChildBirthDate).HasColumnName("child_birth_date");
        builder.Property(x => x.ContactName).HasColumnName("contact_name").IsRequired();
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(30);
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(254);
        builder.Property(x => x.PresentingNeed).HasColumnName("presenting_need");
        builder.Property(x => x.ReferralDetail).HasColumnName("referral_detail");
        builder.Property(x => x.AssignedToId).HasColumnName("assigned_to");
        builder.Property(x => x.Score).HasColumnName("score");
        builder.Property(x => x.ConvertedStudentId).HasColumnName("converted_student_id");
        builder.Property(x => x.ConvertedAt).HasColumnName("converted_at");

        // ── Audit ─────────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // ── Soft-delete global query filter ───────────────────────────────────
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // ── One-to-many navigation ────────────────────────────────────────────
        builder.HasMany(x => x.StatusHistory)
            .WithOne(h => h.Lead)
            .HasForeignKey(h => h.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Activities)
            .WithOne(a => a.Lead)
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Interviews)
            .WithOne(i => i.Lead)
            .HasForeignKey(i => i.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CorporationId, x.PipelineStageId })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_lead_pipeline");

        builder.HasIndex(x => x.CorporationId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_lead_corp");

        builder.HasIndex(x => new { x.CorporationId, x.AssignedToId })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_lead_assigned");
    }
}
