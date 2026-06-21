using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.program_translation.
/// Composite primary key (program_id, locale) — no surrogate id column in DDL.
/// Cascade-deleted when parent program is deleted.
/// </summary>
public class ProgramTranslationConfiguration : IEntityTypeConfiguration<ProgramTranslation>
{
    public void Configure(EntityTypeBuilder<ProgramTranslation> builder)
    {
        builder.ToTable("program_translation", schema: "education");

        builder.HasKey(x => new { x.ProgramId, x.Locale });

        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");

        builder.HasOne(x => x.Program)
            .WithMany(p => p.Translations)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
