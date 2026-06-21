using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Educators;

/// <summary>
/// Maps educators.educator_certification.
/// Audit columns present: created_at, updated_at, row_version.
/// Soft delete: deleted_at.
/// Absent from DDL (ignored): created_by, updated_by.
/// </summary>
public class EducatorCertificationConfiguration : IEntityTypeConfiguration<EducatorCertification>
{
    public void Configure(EntityTypeBuilder<EducatorCertification> builder)
    {
        builder.ToTable("educator_certification", schema: "educators");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.CertificationTypeId).HasColumnName("certification_type_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Issuer).HasColumnName("issuer");
        builder.Property(x => x.IssuedOn).HasColumnName("issued_on");
        builder.Property(x => x.ExpiresOn).HasColumnName("expires_on");
        builder.Property(x => x.FileId).HasColumnName("file_id");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.IsExpired);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
