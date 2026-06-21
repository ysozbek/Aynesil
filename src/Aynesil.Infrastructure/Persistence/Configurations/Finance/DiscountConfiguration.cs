using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.discount.
/// Immutable: created_at only — discount records are financial history and are never modified.
/// discount_type_id references ref.ref_value (ref_type 'discount_type') — configurable.
/// A discount may target either an invoice (invoice_id) or a student package (student_package_id).
/// </summary>
public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("discount", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        builder.Property(x => x.StudentPackageId).HasColumnName("student_package_id");
        builder.Property(x => x.DiscountTypeId).HasColumnName("discount_type_id");
        builder.Property(x => x.IsPercentage).HasColumnName("is_percentage")
            .HasDefaultValue(true).IsRequired();
        builder.Property(x => x.Value).HasColumnName("value")
            .HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("ix_discount_invoice");

        builder.HasIndex(x => x.StudentPackageId)
            .HasDatabaseName("ix_discount_package");
    }
}
