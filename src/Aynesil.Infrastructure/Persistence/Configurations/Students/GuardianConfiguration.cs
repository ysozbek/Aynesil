using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.guardian.
/// Full audit (created_at, created_by, updated_at, updated_by, row_version).
/// Soft delete via deleted_at.
/// Email is citext in DB — EF maps it as string; case-insensitive uniqueness is DB-level.
/// </summary>
public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("guardian", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(x => x.NationalId).HasColumnName("national_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.Phone).HasColumnName("phone");
        builder.Property(x => x.Occupation).HasColumnName("occupation");
        builder.Property(x => x.AddressLine).HasColumnName("address_line");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.Students)
            .WithOne()
            .HasForeignKey(sg => sg.GuardianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PortalAccesses)
            .WithOne()
            .HasForeignKey(pa => pa.GuardianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.CorporationId, x.UserId })
            .HasFilter("user_id IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("ix_guardian_corp_user");
    }
}
