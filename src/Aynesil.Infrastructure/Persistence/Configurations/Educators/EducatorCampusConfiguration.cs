using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Educators;

/// <summary>
/// Maps educators.educator_campus.
/// No audit columns in DDL. No soft delete.
/// Unique: (educator_id, campus_id).
/// </summary>
public class EducatorCampusConfiguration : IEntityTypeConfiguration<EducatorCampus>
{
    public void Configure(EntityTypeBuilder<EducatorCampus> builder)
    {
        builder.ToTable("educator_campus", schema: "educators");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id").IsRequired();
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ActiveFrom).HasColumnName("active_from").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.ActiveTo).HasColumnName("active_to");

        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => new { x.EducatorId, x.CampusId })
            .IsUnique()
            .HasDatabaseName("educator_campus_educator_id_campus_id_key");
    }
}
