using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampPeriodConfiguration : IEntityTypeConfiguration<CampPeriod>
{
    public void Configure(EntityTypeBuilder<CampPeriod> builder)
    {
        builder.ToTable("camp_period", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampId).HasColumnName("camp_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.Capacity).HasColumnName("capacity");

        // Cascade configured on the Camp side; declared here for clarity.
        builder.HasMany(x => x.Enrollments)
            .WithOne(e => e.Period)
            .HasForeignKey(e => e.CampPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
