using Aynesil.Domain.Modules.Legal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Legal;

public class ContractTemplateTranslationConfiguration : IEntityTypeConfiguration<ContractTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<ContractTemplateTranslation> builder)
    {
        builder.ToTable("contract_template_translation", schema: "legal");

        builder.HasKey(x => new { x.ContractTemplateId, x.Locale });

        builder.Property(x => x.ContractTemplateId).HasColumnName("contract_template_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();
    }
}
