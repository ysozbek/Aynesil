using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Iam;

public class MenuItemTranslationConfiguration : IEntityTypeConfiguration<MenuItemTranslation>
{
    public void Configure(EntityTypeBuilder<MenuItemTranslation> builder)
    {
        builder.ToTable("menu_item_translation", schema: "iam");

        builder.HasKey(x => new { x.MenuItemId, x.Locale });

        builder.Property(x => x.MenuItemId).HasColumnName("menu_item_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Label).HasColumnName("label").IsRequired();

        builder.HasOne(x => x.MenuItem)
            .WithMany(m => m.Translations)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
