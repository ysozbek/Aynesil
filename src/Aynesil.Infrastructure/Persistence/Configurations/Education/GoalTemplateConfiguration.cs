using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.goal_template.
/// corporation_id is nullable (NULL = platform-provided template).
/// Soft delete: deleted_at present in DDL.
/// Absent from DDL (ignored): created_by, updated_by.
/// </summary>
public class GoalTemplateConfiguration : IEntityTypeConfiguration<GoalTemplate>
{
    public void Configure(EntityTypeBuilder<GoalTemplate> builder)
    {
        builder.ToTable("goal_template", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.LibraryId).HasColumnName("library_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.DevelopmentAreaId).HasColumnName("development_area_id");
        builder.Property(x => x.Code).HasColumnName("code");
        builder.Property(x => x.Statement).HasColumnName("statement").IsRequired();
        builder.Property(x => x.DefaultCriteria).HasColumnName("default_criteria");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.Translations)
            .WithOne(t => t.Template)
            .HasForeignKey(t => t.GoalTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
