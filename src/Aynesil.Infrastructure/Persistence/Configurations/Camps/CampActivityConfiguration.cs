using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampActivityConfiguration : IEntityTypeConfiguration<CampActivity>
{
    public void Configure(EntityTypeBuilder<CampActivity> builder)
    {
        builder.ToTable("camp_activity", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampPeriodId).HasColumnName("camp_period_id").IsRequired();
        builder.Property(x => x.ActivityTypeId).HasColumnName("activity_type_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.StartsAt).HasColumnName("starts_at");
        builder.Property(x => x.EndsAt).HasColumnName("ends_at");
        builder.Property(x => x.Location).HasColumnName("location");
        builder.Property(x => x.Capacity).HasColumnName("capacity");
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Period)
            .WithMany(p => p.Activities)
            .HasForeignKey(x => x.CampPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Participations)
            .WithOne(p => p.Activity)
            .HasForeignKey(p => p.CampActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
