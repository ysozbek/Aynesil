using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.makeup_request.
/// Tracks missed session → makeup scheduling → completion lifecycle.
/// Audit: requested_at, updated_at, row_version (no created_by / deleted_at in DDL).
/// </summary>
public class MakeupRequestConfiguration : IEntityTypeConfiguration<MakeupRequest>
{
    public void Configure(EntityTypeBuilder<MakeupRequest> builder)
    {
        builder.ToTable("makeup_request", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.MissedSessionId).HasColumnName("missed_session_id");
        builder.Property(x => x.MissedReasonId).HasColumnName("missed_reason_id");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("requested").IsRequired();
        builder.Property(x => x.RequestedBy).HasColumnName("requested_by");
        builder.Property(x => x.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.MakeupSessionId).HasColumnName("makeup_session_id");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.ExpiresOn).HasColumnName("expires_on");
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();
    }
}
