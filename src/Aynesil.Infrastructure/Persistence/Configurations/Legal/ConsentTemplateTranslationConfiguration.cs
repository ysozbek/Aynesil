using Aynesil.Domain.Modules.Legal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Legal;

public class ConsentTemplateTranslationConfiguration : IEntityTypeConfiguration<ConsentTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<ConsentTemplateTranslation> builder)
    {
        builder.ToTable("consent_template_translation", schema: "legal");

        builder.HasKey(x => new { x.ConsentTemplateId, x.Locale });

        builder.Property(x => x.ConsentTemplateId).HasColumnName("consent_template_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();
    }
}
