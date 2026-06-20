using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class FileObjectConfiguration : IEntityTypeConfiguration<FileObject>
{
    public void Configure(EntityTypeBuilder<FileObject> builder)
    {
        builder.ToTable("file_object", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StorageBackend).HasColumnName("storage_backend").HasMaxLength(10).HasDefaultValue("s3").IsRequired();
        builder.Property(x => x.Bucket).HasColumnName("bucket");
        builder.Property(x => x.ObjectKey).HasColumnName("object_key").IsRequired();
        builder.Property(x => x.OriginalName).HasColumnName("original_name").IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("mime_type");
        builder.Property(x => x.ByteSize).HasColumnName("byte_size");
        builder.Property(x => x.ChecksumSha256).HasColumnName("checksum_sha256");
        builder.Property(x => x.IsSensitive).HasColumnName("is_sensitive").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.VirusScanStatus).HasColumnName("virus_scan_status").HasMaxLength(20).HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasQueryFilter(x => x.DeletedAt == null);

        // FileObject uses TenantEntity but overrides audit to match DDL (no created_by/updated_at/updated_by)
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedAt);
        builder.Ignore(e => e.UpdatedBy);

        builder.HasMany(x => x.Attachments)
            .WithOne(a => a.File)
            .HasForeignKey(a => a.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
