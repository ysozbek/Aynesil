using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Media;

/// <summary>
/// EF Core configuration for media.viewing_log.
///
/// DDL notes:
///   - Composite PK: (id bigint GENERATED ALWAYS AS IDENTITY, started_at timestamptz).
///   - PARTITIONED BY RANGE (started_at). EF writes to the parent table;
///     PostgreSQL routes rows to the correct partition automatically.
///     The default partition (viewing_log_default) catches all unpartitioned rows.
///   - ip_address maps to PostgreSQL inet via Npgsql (System.Net.IPAddress).
///   - Immutable append-only log — no soft-delete, no update operations beyond ending a session.
/// </summary>
public class ViewingLogConfiguration : IEntityTypeConfiguration<ViewingLog>
{
    public void Configure(EntityTypeBuilder<ViewingLog> builder)
    {
        builder.ToTable("viewing_log", schema: "media");

        builder.HasKey(x => new { x.Id, x.StartedAt });

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.CameraId).HasColumnName("camera_id");
        builder.Property(x => x.AuthorizationId).HasColumnName("authorization_id");
        builder.Property(x => x.StartedAt).HasColumnName("started_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.EndedAt).HasColumnName("ended_at");
        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");
    }
}
