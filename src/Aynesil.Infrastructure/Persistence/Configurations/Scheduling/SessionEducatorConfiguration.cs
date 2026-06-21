using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.session_educator.
/// Unique: (session_id, educator_id). No audit columns. Cascade-deleted from session.
/// Educator double-booking is enforced by a DB trigger.
/// ix_session_educator_lookup supports conflict detection queries.
/// </summary>
public class SessionEducatorConfiguration : IEntityTypeConfiguration<SessionEducator>
{
    public void Configure(EntityTypeBuilder<SessionEducator> builder)
    {
        builder.ToTable("session_educator", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasDefaultValue("lead").IsRequired();

        builder.HasIndex(x => new { x.SessionId, x.EducatorId })
            .IsUnique()
            .HasDatabaseName("session_educator_session_id_educator_id_key");

        builder.HasIndex(x => x.EducatorId)
            .HasDatabaseName("ix_session_educator_lookup");
    }
}
