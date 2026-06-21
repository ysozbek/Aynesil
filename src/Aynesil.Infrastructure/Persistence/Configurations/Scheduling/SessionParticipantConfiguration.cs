using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.session_participant.
/// Unique: (session_id, student_id). No audit columns. Cascade-deleted from session.
/// </summary>
public class SessionParticipantConfiguration : IEntityTypeConfiguration<SessionParticipant>
{
    public void Configure(EntityTypeBuilder<SessionParticipant> builder)
    {
        builder.ToTable("session_participant", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.StudentProgramId).HasColumnName("student_program_id");
        builder.Property(x => x.Role).HasColumnName("role").HasDefaultValue("student").IsRequired();

        builder.HasIndex(x => new { x.SessionId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("session_participant_session_id_student_id_key");
    }
}
