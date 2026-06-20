using Aynesil.Domain.Modules.Ref.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ref;

public class RefTypeConfiguration : IEntityTypeConfiguration<RefType>
{
    public void Configure(EntityTypeBuilder<RefType> builder)
    {
        builder.ToTable("ref_type", schema: "ref");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsHierarchical).HasColumnName("is_hierarchical").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.AllowsTenantValues).HasColumnName("allows_tenant_values").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.ValueSchema).HasColumnName("value_schema").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Values)
            .WithOne(v => v.RefType)
            .HasForeignKey(v => v.RefTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
