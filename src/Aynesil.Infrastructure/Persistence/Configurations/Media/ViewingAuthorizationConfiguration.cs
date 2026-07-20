using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Media;

/// <summary>
/// EF Core configuration for media.viewing_authorization.
///
/// DDL notes:
///   - No updated_at, updated_by, row_version, deleted_at columns — all ignored.
///   - access_type_id added via V20 migration (nullable FK → ref.ref_value).
///   - ix_viewing_auth_guardian: partial index on guardian_id where is_revoked = false.
///   - Record is effectively immutable after creation; Revoke() is the only mutation.
/// </summary>
public class ViewingAuthorizationConfiguration : IEntityTypeConfiguration<ViewingAuthorization>
{
    public void Configure(EntityTypeBuilder<ViewingAuthorization> builder)
    {
        builder.ToTable("viewing_authorization", schema: "media");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.ConsentId).HasColumnName("consent_id");
        builder.Property(x => x.AccessTypeId).HasColumnName("access_type_id");
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ValidTo).HasColumnName("valid_to").IsRequired();
        builder.Property(x => x.GrantedBy).HasColumnName("granted_by");
        builder.Property(x => x.IsRevoked).HasColumnName("is_revoked")
            .HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();

        // Columns not present in media.viewing_authorization DDL.
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => x.GuardianId)
            .HasFilter("is_revoked = false")
            .HasDatabaseName("ix_viewing_auth_guardian");
    }
}
