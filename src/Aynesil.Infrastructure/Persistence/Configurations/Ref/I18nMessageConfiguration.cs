using Aynesil.Domain.Modules.Ref.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ref;

public class I18nMessageConfiguration : IEntityTypeConfiguration<I18nMessage>
{
    public void Configure(EntityTypeBuilder<I18nMessage> builder)
    {
        builder.ToTable("i18n_message", schema: "ref");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.Namespace).HasColumnName("namespace").HasMaxLength(100).IsRequired();
        builder.Property(x => x.MsgKey).HasColumnName("msg_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Value).HasColumnName("value").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasIndex(x => new { x.Namespace, x.MsgKey, x.Locale }).IsUnique();

        builder.HasOne(x => x.LocaleNavigation)
            .WithMany()
            .HasForeignKey(x => x.Locale)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
