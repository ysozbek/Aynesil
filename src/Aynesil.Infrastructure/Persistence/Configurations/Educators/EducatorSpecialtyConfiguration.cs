using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Educators;

/// <summary>
/// Maps educators.educator_specialty.
/// No audit columns. No soft delete.
/// Unique: (educator_id, specialty_id).
/// </summary>
public class EducatorSpecialtyConfiguration : IEntityTypeConfiguration<EducatorSpecialty>
{
    public void Configure(EntityTypeBuilder<EducatorSpecialty> builder)
    {
        builder.ToTable("educator_specialty", schema: "educators");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.SpecialtyId).HasColumnName("specialty_id").IsRequired();

        builder.HasIndex(x => new { x.EducatorId, x.SpecialtyId })
            .IsUnique()
            .HasDatabaseName("educator_specialty_educator_id_specialty_id_key");
    }
}
