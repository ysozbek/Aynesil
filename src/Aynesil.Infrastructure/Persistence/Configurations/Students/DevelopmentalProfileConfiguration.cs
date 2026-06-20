using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.developmental_profile.
/// Audit: created_at, created_by, updated_at, updated_by, row_version (all present in DB).
/// No soft delete — DeletedAt column does NOT exist in the DB schema. Ignored here.
/// </summary>
public class DevelopmentalProfileConfiguration : IEntityTypeConfiguration<DevelopmentalProfile>
{
    public void Configure(EntityTypeBuilder<DevelopmentalProfile> builder)
    {
        builder.ToTable("developmental_profile", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.DevelopmentAreaId).HasColumnName("development_area_id");
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.Strengths).HasColumnName("strengths");
        builder.Property(x => x.Needs).HasColumnName("needs");
        builder.Property(x => x.AssessedOn).HasColumnName("assessed_on");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // deleted_at does NOT exist in DB — ignore the inherited property
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.StudentId, x.DevelopmentAreaId })
            .HasDatabaseName("ix_developmental_profile_student_area");
    }
}
