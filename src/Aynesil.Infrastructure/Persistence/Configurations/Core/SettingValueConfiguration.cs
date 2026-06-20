using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class SettingValueConfiguration : IEntityTypeConfiguration<SettingValue>
{
    public void Configure(EntityTypeBuilder<SettingValue> builder)
    {
        builder.ToTable("setting_value", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.SettingKey).HasColumnName("setting_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ScopeLevel).HasColumnName("scope_level").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.Value).HasColumnName("value").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Unique per (key, scope_level, corporation_id, scope_id) — NULLS NOT DISTINCT in PG
        builder.HasIndex(x => new { x.SettingKey, x.ScopeLevel, x.CorporationId, x.ScopeId })
            .IsUnique()
            .HasDatabaseName("uq_setting_value");
    }
}
