using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class NotificationTemplateTranslationConfiguration : IEntityTypeConfiguration<NotificationTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateTranslation> builder)
    {
        builder.ToTable("notification_template_translation", schema: "core");

        builder.HasKey(x => new { x.TemplateId, x.Locale });

        builder.Property(x => x.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject");
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();

        builder.HasOne(x => x.Template)
            .WithMany(t => t.Translations)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
