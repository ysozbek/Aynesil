using Aynesil.Domain.Modules.Ref.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ref;

public class RefValueConfiguration : IEntityTypeConfiguration<RefValue>
{
    public void Configure(EntityTypeBuilder<RefValue> builder)
    {
        builder.ToTable("ref_value", schema: "ref");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.RefTypeId).HasColumnName("ref_type_id").IsRequired();
        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ParentValueId).HasColumnName("parent_value_id");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDefault).HasColumnName("is_default").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // Composite unique: (ref_type_id, id) — used for composite FK pinning in business tables
        builder.HasIndex(x => new { x.RefTypeId, x.Id }).IsUnique().HasDatabaseName("uq_ref_value_type_id");
        // Unique code per (category, scope) — partial unique handled by DB partial unique index
        builder.HasIndex(x => new { x.RefTypeId, x.CorporationId, x.Code }).IsUnique().HasDatabaseName("uq_ref_value_code");

        // Ignore computed property — no DB column
        builder.Ignore(x => x.IsDeleted);

        // Soft-delete global filter set in DbContext.OnModelCreating
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.RefType)
            .WithMany(rt => rt.Values)
            .HasForeignKey(x => x.RefTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentValue)
            .WithMany(p => p.ChildValues)
            .HasForeignKey(x => x.ParentValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Translations)
            .WithOne(t => t.RefValue)
            .HasForeignKey(t => t.RefValueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TenantOverrides)
            .WithOne(o => o.RefValue)
            .HasForeignKey(o => o.RefValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
