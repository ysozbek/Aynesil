using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.promotion.
/// No audit fields in DDL.
/// Unique: (corporation_id, code).
/// </summary>
public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotion", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.IsPercentage).HasColumnName("is_percentage")
            .HasDefaultValue(true).IsRequired();
        builder.Property(x => x.Value).HasColumnName("value")
            .HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from")
            .HasColumnType("date");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to")
            .HasColumnType("date");
        builder.Property(x => x.MaxRedemptions).HasColumnName("max_redemptions");
        builder.Property(x => x.IsActive).HasColumnName("is_active")
            .HasDefaultValue(true).IsRequired();

        builder.HasIndex(x => new { x.CorporationId, x.Code })
            .IsUnique()
            .HasDatabaseName("promotion_corporation_id_code_key");
    }
}
