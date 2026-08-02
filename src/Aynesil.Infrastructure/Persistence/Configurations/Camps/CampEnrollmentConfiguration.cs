using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampEnrollmentConfiguration : IEntityTypeConfiguration<CampEnrollment>
{
    public void Configure(EntityTypeBuilder<CampEnrollment> builder)
    {
        builder.ToTable("camp_enrollment", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampPeriodId).HasColumnName("camp_period_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.StudentPackageId).HasColumnName("student_package_id");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("enrolled")
            .IsRequired();
        builder.Property(x => x.EnrolledAt)
            .HasColumnName("enrolled_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.CampPeriodId, x.StudentId }).IsUnique();

        builder.HasMany(x => x.Attendances)
            .WithOne(a => a.Enrollment)
            .HasForeignKey(a => a.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reports)
            .WithOne(r => r.Enrollment)
            .HasForeignKey(r => r.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
