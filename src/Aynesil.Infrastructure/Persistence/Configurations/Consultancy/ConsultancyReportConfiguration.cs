using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class ConsultancyReportConfiguration : IEntityTypeConfiguration<ConsultancyReport>
{
    public void Configure(EntityTypeBuilder<ConsultancyReport> builder)
    {
        builder.ToTable("consultancy_report", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.ConsultancyPlanId).HasColumnName("consultancy_plan_id");
        builder.Property(x => x.SchoolVisitId).HasColumnName("school_visit_id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.FileId).HasColumnName("file_id");
        builder.Property(x => x.AuthoredBy).HasColumnName("authored_by");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
