using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.goal_library.
/// corporation_id is nullable (NULL = platform-provided library).
/// Absent from DDL (ignored): created_by, updated_by.
/// No soft-delete: deleted_at column does not exist on this table.
/// </summary>
public class GoalLibraryConfiguration : IEntityTypeConfiguration<GoalLibrary>
{
    public void Configure(EntityTypeBuilder<GoalLibrary> builder)
    {
        builder.ToTable("goal_library", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasMany(x => x.Templates)
            .WithOne(t => t.Library)
            .HasForeignKey(t => t.LibraryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
