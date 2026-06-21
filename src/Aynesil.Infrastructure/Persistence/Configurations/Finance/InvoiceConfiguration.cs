using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.invoice.
/// Soft delete: deleted_at.
/// Audit: created_at, updated_at, row_version — no created_by / updated_by in DDL.
/// Unique: (corporation_id, invoice_no) — invoice_no can be null; nulls are not considered equal
/// by the PostgreSQL unique constraint (i.e. multiple drafts without a number are allowed).
/// status check: draft | issued | paid | partial | void | overdue.
/// Financial integrity: void transitions are tracked — physical delete is not allowed.
/// </summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");
        builder.Property(x => x.InvoiceNo).HasColumnName("invoice_no");
        builder.Property(x => x.IssueDate).HasColumnName("issue_date")
            .HasColumnType("date").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date")
            .HasColumnType("date");
        builder.Property(x => x.Currency).HasColumnName("currency")
            .HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(x => x.Subtotal).HasColumnName("subtotal")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.DiscountTotal).HasColumnName("discount_total")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.TaxTotal).HasColumnName("tax_total")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.GrandTotal).HasColumnName("grand_total")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status")
            .HasDefaultValue("draft").IsRequired();

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

        builder.HasIndex(x => new { x.CorporationId, x.InvoiceNo })
            .IsUnique()
            .HasDatabaseName("invoice_corporation_id_invoice_no_key");

        builder.HasIndex(x => new { x.CorporationId, x.StudentId, x.IssueDate })
            .HasDatabaseName("ix_invoice_student");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Discounts)
            .WithOne()
            .HasForeignKey(d => d.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
