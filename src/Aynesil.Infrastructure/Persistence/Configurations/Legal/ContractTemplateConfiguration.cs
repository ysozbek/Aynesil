using Aynesil.Domain.Modules.Legal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Legal;

public class ContractTemplateConfiguration : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("contract_template", schema: "legal");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.ContractTypeId).HasColumnName("contract_type_id");
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.IsCurrent).HasColumnName("is_current").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsRequired()
            .IsConcurrencyToken();

        // DDL has no created_by / updated_by columns.
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        // IsDeleted is a computed property — no dedicated column.
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.CorporationId, x.Code, x.Version })
            .IsUnique()
            .HasDatabaseName("ux_contract_template_corp_code_version");

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(t => t.ContractTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
