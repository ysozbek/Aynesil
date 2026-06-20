using Aynesil.Domain.Modules.Ref.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ref;

public class RefValueTenantOverrideConfiguration : IEntityTypeConfiguration<RefValueTenantOverride>
{
    public void Configure(EntityTypeBuilder<RefValueTenantOverride> builder)
    {
        builder.ToTable("ref_value_tenant_override", schema: "ref");

        builder.HasKey(x => new { x.CorporationId, x.RefValueId });

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.RefValueId).HasColumnName("ref_value_id").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.IsDefault).HasColumnName("is_default");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.RefValue)
            .WithMany(v => v.TenantOverrides)
            .HasForeignKey(x => x.RefValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
