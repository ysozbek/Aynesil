using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class SettingDefinitionConfiguration : IEntityTypeConfiguration<SettingDefinition>
{
    public void Configure(EntityTypeBuilder<SettingDefinition> builder)
    {
        builder.ToTable("setting_definition", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.DataType).HasColumnName("data_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultValue).HasColumnName("default_value").HasColumnType("jsonb");
        builder.Property(x => x.ScopeLevels).HasColumnName("scope_levels").HasColumnType("text[]").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.Key).IsUnique();

        builder.HasMany(x => x.Values)
            .WithOne(v => v.Definition)
            .HasForeignKey(v => v.SettingKey)
            .HasPrincipalKey(d => d.Key)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
