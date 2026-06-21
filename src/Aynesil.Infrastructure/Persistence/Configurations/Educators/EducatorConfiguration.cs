using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Educators;

/// <summary>
/// Maps educators.educator.
/// Full audit: created_at, created_by, updated_at, updated_by.
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// </summary>
public class EducatorConfiguration : IEntityTypeConfiguration<Educator>
{
    public void Configure(EntityTypeBuilder<Educator> builder)
    {
        builder.ToTable("educator", schema: "educators");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(x => x.TitleId).HasColumnName("title_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.Phone).HasColumnName("phone");
        builder.Property(x => x.EmploymentType).HasColumnName("employment_type");
        builder.Property(x => x.HireDate).HasColumnName("hire_date");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.PrimaryCampusId).HasColumnName("primary_campus_id");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.IsActive })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_educator_corp_active");

        builder.HasMany(x => x.Campuses)
            .WithOne()
            .HasForeignKey(c => c.EducatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Specialties)
            .WithOne()
            .HasForeignKey(s => s.EducatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Certifications)
            .WithOne()
            .HasForeignKey(c => c.EducatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Supervisors)
            .WithOne()
            .HasForeignKey(h => h.EducatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Subordinates)
            .WithOne()
            .HasForeignKey(h => h.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
