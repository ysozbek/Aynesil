using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.recurring_schedule_educator (V35).
/// Unique: (recurring_schedule_id, educator_id). No audit columns. Cascade-deleted from the rule.
/// </summary>
public class RecurringScheduleEducatorConfiguration : IEntityTypeConfiguration<RecurringScheduleEducator>
{
    public void Configure(EntityTypeBuilder<RecurringScheduleEducator> builder)
    {
        builder.ToTable("recurring_schedule_educator", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.RecurringScheduleId).HasColumnName("recurring_schedule_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasDefaultValue("lead").IsRequired();

        builder.HasIndex(x => new { x.RecurringScheduleId, x.EducatorId })
            .IsUnique()
            .HasDatabaseName("recurring_schedule_educator_recurring_schedule_id_educator_id_key");

        builder.HasIndex(x => x.EducatorId)
            .HasDatabaseName("ix_recurring_schedule_educator_educator");
    }
}
