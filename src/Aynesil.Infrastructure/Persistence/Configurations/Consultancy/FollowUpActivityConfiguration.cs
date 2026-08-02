using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class FollowUpActivityConfiguration : IEntityTypeConfiguration<FollowUpActivity>
{
    public void Configure(EntityTypeBuilder<FollowUpActivity> builder)
    {
        builder.ToTable("follow_up_activity", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.ConsultancyPlanId).HasColumnName("consultancy_plan_id");
        builder.Property(x => x.SchoolVisitId).HasColumnName("school_visit_id");
        builder.Property(x => x.ObservationRecordId).HasColumnName("observation_record_id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.AssignedTo).HasColumnName("assigned_to");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("pending")
            .IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CompletedBy).HasColumnName("completed_by");
        builder.Property(x => x.Notes).HasColumnName("notes");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsRequired()
            .IsConcurrencyToken();

        // No deleted_at column — lifecycle via status only.
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);
    }
}
