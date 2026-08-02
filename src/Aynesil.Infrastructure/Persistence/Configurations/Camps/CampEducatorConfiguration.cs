using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampEducatorConfiguration : IEntityTypeConfiguration<CampEducator>
{
    public void Configure(EntityTypeBuilder<CampEducator> builder)
    {
        builder.ToTable("camp_educator", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampId).HasColumnName("camp_id").IsRequired();
        builder.Property(x => x.CampPeriodId).HasColumnName("camp_period_id");
        builder.Property(x => x.CampActivityId).HasColumnName("camp_activity_id");
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasDefaultValue("lead")
            .IsRequired();
        builder.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.AssignedBy).HasColumnName("assigned_by");

        builder.HasIndex(x => new { x.CampId, x.CampPeriodId, x.CampActivityId, x.EducatorId })
            .IsUnique();

        builder.HasOne(x => x.Camp)
            .WithMany(c => c.Educators)
            .HasForeignKey(x => x.CampId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Period)
            .WithMany()
            .HasForeignKey(x => x.CampPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Activity)
            .WithMany(a => a.Educators)
            .HasForeignKey(x => x.CampActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
