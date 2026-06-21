using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.package_definition.
/// Soft delete: deleted_at.
/// Audit: created_at, updated_at, row_version — no created_by / updated_by in DDL.
/// Unique: (corporation_id, code).
/// </summary>
public class PackageDefinitionConfiguration : IEntityTypeConfiguration<PackageDefinition>
{
    public void Configure(EntityTypeBuilder<PackageDefinition> builder)
    {
        builder.ToTable("package_definition", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.PackageTypeId).HasColumnName("package_type_id");
        builder.Property(x => x.ProgramId).HasColumnName("program_id");
        builder.Property(x => x.TotalCredits).HasColumnName("total_credits")
            .HasColumnType("numeric(10,2)");
        builder.Property(x => x.ValidityDays).HasColumnName("validity_days");
        builder.Property(x => x.ListPrice).HasColumnName("list_price")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency")
            .HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active")
            .HasDefaultValue(true).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsConcurrencyToken();

        // Not in DDL
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.Code })
            .IsUnique()
            .HasDatabaseName("package_definition_corporation_id_code_key");

        builder.HasMany(x => x.StudentPackages)
            .WithOne(sp => sp.PackageDefinition)
            .HasForeignKey(sp => sp.PackageDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
