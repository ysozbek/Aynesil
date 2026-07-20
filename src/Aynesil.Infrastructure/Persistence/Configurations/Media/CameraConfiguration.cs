using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Media;

/// <summary>
/// EF Core configuration for media.camera.
///
/// DDL notes:
///   - created_by / updated_by columns are NOT present — those parent fields are ignored.
///   - deleted_at IS present — soft-delete filter applied.
///   - row_version IS present — concurrency token.
///   - camera_type_id added via V20 migration (nullable FK → ref.ref_value).
/// </summary>
public class CameraConfiguration : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("camera", schema: "media");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.CameraTypeId).HasColumnName("camera_type_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.StreamProviderId).HasColumnName("stream_provider_id");
        builder.Property(x => x.StreamRef).HasColumnName("stream_ref").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsRequired().IsConcurrencyToken();

        // media.camera has no created_by or updated_by columns.
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.CorporationId, x.Code }).IsUnique()
            .HasDatabaseName("camera_corporation_id_code_key");

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
