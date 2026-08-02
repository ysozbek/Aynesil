using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class ObservationRecordConfiguration : IEntityTypeConfiguration<ObservationRecord>
{
    public void Configure(EntityTypeBuilder<ObservationRecord> builder)
    {
        builder.ToTable("observation_record", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SchoolVisitId).HasColumnName("school_visit_id").IsRequired();
        builder.Property(x => x.ObservationTypeId).HasColumnName("observation_type_id");
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(300);
        builder.Property(x => x.Observation).HasColumnName("observation").IsRequired();
        builder.Property(x => x.Recommendations).HasColumnName("recommendations");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
    }
}
