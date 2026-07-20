using Aynesil.Domain.Modules.Legal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Legal;

/// <summary>
/// EF Core configuration for legal.student_consent.
/// Minimal read-model configuration introduced to support consent validation
/// in the Media / Camera module. Full Legal module implementation is deferred.
///
/// DDL notes:
///   - No deleted_at column — physical delete forbidden; state transitions are the lifecycle.
///   - state: 'granted' | 'withdrawn' | 'expired' (CHECK constraint in DDL).
///   - valid_until is a DATE column → mapped as DateOnly.
///   - row_version IS present — concurrency token.
/// </summary>
public class StudentConsentConfiguration : IEntityTypeConfiguration<StudentConsent>
{
    public void Configure(EntityTypeBuilder<StudentConsent> builder)
    {
        builder.ToTable("student_consent", schema: "legal");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");
        builder.Property(x => x.ConsentTypeId).HasColumnName("consent_type_id");
        builder.Property(x => x.State).HasColumnName("state").HasMaxLength(20)
            .HasDefaultValue("granted").IsRequired();
        builder.Property(x => x.GrantedAt).HasColumnName("granted_at");
        builder.Property(x => x.WithdrawnAt).HasColumnName("withdrawn_at");
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsRequired().IsConcurrencyToken();

        // No deleted_at column in legal.student_consent.
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.CorporationId, x.StudentId, x.ConsentTypeId })
            .HasDatabaseName("ix_consent_student_type");

        builder.HasQueryFilter(x => x.State != "expired");
    }
}
