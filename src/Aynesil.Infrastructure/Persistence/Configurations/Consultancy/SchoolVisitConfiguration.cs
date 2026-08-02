using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class SchoolVisitConfiguration : IEntityTypeConfiguration<SchoolVisit>
{
    public void Configure(EntityTypeBuilder<SchoolVisit> builder)
    {
        builder.ToTable("school_visit", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.ConsultancyPlanId).HasColumnName("consultancy_plan_id");
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.VisitDate).HasColumnName("visit_date").IsRequired();
        builder.Property(x => x.VisitorId).HasColumnName("visitor_id");
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(500);
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("planned")
            .IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasMany(x => x.Observations)
            .WithOne(o => o.Visit)
            .HasForeignKey(o => o.SchoolVisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reports)
            .WithOne(r => r.Visit)
            .HasForeignKey(r => r.SchoolVisitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
