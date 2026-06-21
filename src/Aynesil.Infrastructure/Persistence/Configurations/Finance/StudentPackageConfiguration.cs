using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.student_package.
/// Soft delete: deleted_at.
/// Audit: created_at, created_by, updated_at, row_version — no updated_by in DDL.
/// Comment: "Remaining credits = SUM(finance.credit_ledger.delta)."
/// </summary>
public class StudentPackageConfiguration : IEntityTypeConfiguration<StudentPackage>
{
    public void Configure(EntityTypeBuilder<StudentPackage> builder)
    {
        builder.ToTable("student_package", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.PackageDefinitionId).HasColumnName("package_definition_id");
        builder.Property(x => x.PurchasedOn).HasColumnName("purchased_on")
            .HasColumnType("date").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.ExpiresOn).HasColumnName("expires_on")
            .HasColumnType("date");
        builder.Property(x => x.TotalCredits).HasColumnName("total_credits")
            .HasColumnType("numeric(10,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Price).HasColumnName("price")
            .HasColumnType("numeric(14,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency")
            .HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status")
            .HasDefaultValue("active").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsConcurrencyToken();

        // Not in DDL
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.StudentId })
            .HasDatabaseName("ix_student_package_student");

        builder.HasMany(x => x.CreditLedgerEntries)
            .WithOne()
            .HasForeignKey(l => l.StudentPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.InvoiceLines)
            .WithOne()
            .HasForeignKey(l => l.StudentPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Discounts)
            .WithOne()
            .HasForeignKey(d => d.StudentPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
