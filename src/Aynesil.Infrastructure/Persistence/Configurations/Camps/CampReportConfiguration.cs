using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampReportConfiguration : IEntityTypeConfiguration<CampReport>
{
    public void Configure(EntityTypeBuilder<CampReport> builder)
    {
        builder.ToTable("camp_report", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampEnrollmentId).HasColumnName("camp_enrollment_id").IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.FileId).HasColumnName("file_id");
        builder.Property(x => x.AuthoredBy).HasColumnName("authored_by");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
