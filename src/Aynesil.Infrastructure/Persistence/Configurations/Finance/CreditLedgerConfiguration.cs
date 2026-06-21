using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.credit_ledger.
/// Append-only: no updated_at, no deleted_at — rows are NEVER modified after insertion.
/// Financial integrity: physical deletion is not allowed.
/// entry_type check: grant | consume | refund | adjustment | expire.
/// delta is positive for grants/refunds and negative for consume/expire.
/// </summary>
public class CreditLedgerConfiguration : IEntityTypeConfiguration<CreditLedger>
{
    public void Configure(EntityTypeBuilder<CreditLedger> builder)
    {
        builder.ToTable("credit_ledger", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentPackageId).HasColumnName("student_package_id").IsRequired();
        builder.Property(x => x.EntryType).HasColumnName("entry_type").IsRequired();
        builder.Property(x => x.Delta).HasColumnName("delta")
            .HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");

        // DDL index: ix_credit_ledger_pkg on (student_package_id, occurred_at)
        builder.HasIndex(x => new { x.StudentPackageId, x.OccurredAt })
            .HasDatabaseName("ix_credit_ledger_pkg");
    }
}
