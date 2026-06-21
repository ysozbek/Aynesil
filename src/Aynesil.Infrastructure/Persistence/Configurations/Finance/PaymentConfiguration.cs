using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.payment.
/// Financial immutability: no deleted_at — payment rows are never deleted.
/// Audit: created_at, updated_at, row_version (no created_by / updated_by in DDL).
/// status: pending | authorized | captured | failed | refunded
/// idempotency_key: unique per corporation with NULLS NOT DISTINCT (DB-enforced at DDL level).
/// gateway_provider_id: FK to core.integration_connection (payment gateway seam).
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.PaymentMethodId).HasColumnName("payment_method_id");
        builder.Property(x => x.Amount).HasColumnName("amount")
            .HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency")
            .HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status")
            .HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.GatewayProviderId).HasColumnName("gateway_provider_id");
        builder.Property(x => x.GatewayReference).HasColumnName("gateway_reference");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.PaidAt).HasColumnName("paid_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsConcurrencyToken();

        // Not in DDL — financial immutability means no soft delete
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.CorporationId, x.InvoiceId })
            .HasDatabaseName("ix_payment_invoice");

        builder.HasIndex(x => new { x.CorporationId, x.StudentId })
            .HasDatabaseName("ix_payment_student");

        // idempotency_key uniqueness is enforced at DB level with NULLS NOT DISTINCT;
        // EF index is informational only
        builder.HasIndex(x => new { x.CorporationId, x.IdempotencyKey })
            .HasDatabaseName("payment_corporation_id_idempotency_key_key");

        builder.HasMany(x => x.Refunds)
            .WithOne()
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
