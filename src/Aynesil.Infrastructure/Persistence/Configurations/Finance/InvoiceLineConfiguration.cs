using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.invoice_line.
/// Cascade-deleted when the parent invoice is deleted.
/// No independent audit or soft delete — follows the parent invoice lifecycle.
/// </summary>
public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_line", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").IsRequired();
        builder.Property(x => x.StudentPackageId).HasColumnName("student_package_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity")
            .HasColumnType("numeric(10,2)").HasDefaultValue(1m).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.LineTotal).HasColumnName("line_total")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order")
            .HasDefaultValue(0).IsRequired();
    }
}
