using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Iam;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_item", schema: "iam");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Route).HasColumnName("route").HasMaxLength(300);
        builder.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(100);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.RequiredPermissionId).HasColumnName("required_permission_id");
        builder.Property(x => x.FeatureFlag).HasColumnName("feature_flag").HasMaxLength(100);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasIndex(x => new { x.CorporationId, x.Code }).IsUnique().HasDatabaseName("uq_menu_item_code");

        builder.HasOne(x => x.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequiredPermission)
            .WithMany()
            .HasForeignKey(x => x.RequiredPermissionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Translations)
            .WithOne(t => t.MenuItem)
            .HasForeignKey(t => t.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
