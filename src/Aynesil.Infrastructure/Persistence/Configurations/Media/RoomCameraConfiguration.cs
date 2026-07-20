using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Media;

/// <summary>
/// EF Core configuration for media.room_camera.
/// Bare junction table: id, corporation_id, room_id, camera_id. No audit columns.
/// </summary>
public class RoomCameraConfiguration : IEntityTypeConfiguration<RoomCamera>
{
    public void Configure(EntityTypeBuilder<RoomCamera> builder)
    {
        builder.ToTable("room_camera", schema: "media");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(x => x.CameraId).HasColumnName("camera_id").IsRequired();

        builder.HasIndex(x => new { x.RoomId, x.CameraId }).IsUnique()
            .HasDatabaseName("room_camera_room_id_camera_id_key");
    }
}
