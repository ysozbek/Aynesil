using Aynesil.Domain.Modules.Ref.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ref;

public class LocaleConfiguration : IEntityTypeConfiguration<Locale>
{
    public void Configure(EntityTypeBuilder<Locale> builder)
    {
        builder.ToTable("locale", schema: "ref");

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.EnglishName).HasColumnName("english_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NativeName).HasColumnName("native_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(3).HasDefaultValue("ltr").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
    }
}
