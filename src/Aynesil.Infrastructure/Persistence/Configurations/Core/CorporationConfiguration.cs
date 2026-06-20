using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class CorporationConfiguration : IEntityTypeConfiguration<Corporation>
{
    public void Configure(EntityTypeBuilder<Corporation> builder)
    {
        builder.ToTable("corporation", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.LegalName).HasColumnName("legal_name").IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(x => x.DefaultLocale).HasColumnName("default_locale").HasMaxLength(20).HasDefaultValue("tr").IsRequired();
        builder.Property(x => x.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3).HasDefaultValue("TRY").IsRequired();
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasDefaultValue("Europe/Istanbul").IsRequired();
        builder.Property(x => x.TaxOffice).HasColumnName("tax_office");
        builder.Property(x => x.TaxNumber).HasColumnName("tax_number");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.Settings).HasColumnName("settings").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);
        // Corporation does not inherit SoftDeleteEntity so no global filter applies here.
        // Soft delete for Corporation is intentional absence from global filter.
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Campuses)
            .WithOne(c => c.Corporation)
            .HasForeignKey(c => c.CorporationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
