using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.program.
/// Audit columns present: created_at, updated_at, row_version.
/// Soft delete: deleted_at.
/// Absent from DDL (ignored): created_by, updated_by.
/// Unique: (corporation_id, code).
/// </summary>
public class EducationProgramConfiguration : IEntityTypeConfiguration<EducationProgram>
{
    public void Configure(EntityTypeBuilder<EducationProgram> builder)
    {
        builder.ToTable("program", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.ProgramTypeId).HasColumnName("program_type_id");
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.Code })
            .IsUnique()
            .HasDatabaseName("program_corporation_id_code_key");

        builder.HasMany(x => x.Translations)
            .WithOne(t => t.Program)
            .HasForeignKey(t => t.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Services)
            .WithOne(s => s.Program)
            .HasForeignKey(s => s.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StudentPrograms)
            .WithOne(sp => sp.Program)
            .HasForeignKey(sp => sp.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
