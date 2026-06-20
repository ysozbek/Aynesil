using Aynesil.Domain.Modules.Ref.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ref;

public class RefValueTranslationConfiguration : IEntityTypeConfiguration<RefValueTranslation>
{
    public void Configure(EntityTypeBuilder<RefValueTranslation> builder)
    {
        builder.ToTable("ref_value_translation", schema: "ref");

        builder.HasKey(x => new { x.RefValueId, x.Locale });

        builder.Property(x => x.RefValueId).HasColumnName("ref_value_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortLabel).HasColumnName("short_label").HasMaxLength(50);
        builder.Property(x => x.Description).HasColumnName("description");

        builder.HasOne(x => x.RefValue)
            .WithMany(v => v.Translations)
            .HasForeignKey(x => x.RefValueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LocaleNavigation)
            .WithMany()
            .HasForeignKey(x => x.Locale)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
