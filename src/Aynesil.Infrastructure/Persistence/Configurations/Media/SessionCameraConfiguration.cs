using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Media;

/// <summary>
/// EF Core configuration for media.session_camera.
/// Bare junction table: id, corporation_id, session_id, camera_id. No audit columns.
/// </summary>
public class SessionCameraConfiguration : IEntityTypeConfiguration<SessionCamera>
{
    public void Configure(EntityTypeBuilder<SessionCamera> builder)
    {
        builder.ToTable("session_camera", schema: "media");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.CameraId).HasColumnName("camera_id").IsRequired();

        builder.HasIndex(x => new { x.SessionId, x.CameraId }).IsUnique()
            .HasDatabaseName("session_camera_session_id_camera_id_key");
    }
}
