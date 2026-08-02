using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class ConsultancyPlanConfiguration : IEntityTypeConfiguration<ConsultancyPlan>
{
    public void Configure(EntityTypeBuilder<ConsultancyPlan> builder)
    {
        builder.ToTable("consultancy_plan", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.ConsultancyTypeId).HasColumnName("consultancy_type_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnName("period_start");
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end");
        builder.Property(x => x.Scope).HasColumnName("scope");
        builder.Property(x => x.LeadEducatorId).HasColumnName("lead_educator_id");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("draft")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsRequired()
            .IsConcurrencyToken();

        // DB schema has no deleted_at, created_by, or updated_by columns.
        // Lifecycle is managed through status transitions only.
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);

        builder.HasMany(x => x.Visits)
            .WithOne(v => v.Plan)
            .HasForeignKey(v => v.ConsultancyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reports)
            .WithOne(r => r.Plan)
            .HasForeignKey(r => r.ConsultancyPlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
