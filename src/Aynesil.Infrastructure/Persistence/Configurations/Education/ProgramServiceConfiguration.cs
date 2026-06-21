using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.program_service.
/// No audit columns in DDL. No soft delete.
/// </summary>
public class ProgramServiceConfiguration : IEntityTypeConfiguration<ProgramService>
{
    public void Configure(EntityTypeBuilder<ProgramService> builder)
    {
        builder.ToTable("program_service", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.ServiceTypeId).HasColumnName("service_type_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.DefaultDurationMinutes).HasColumnName("default_duration_minutes");
        builder.Property(x => x.DefaultSessionsPerWeek)
            .HasColumnName("default_sessions_per_week")
            .HasPrecision(4, 1);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();

        builder.HasOne(x => x.Program)
            .WithMany(p => p.Services)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
