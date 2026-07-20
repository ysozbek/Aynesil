using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

/// <summary>
/// EF Core configuration for ops.leave_balance.
/// Unique constraint (DB): (educator_id, leave_type_id, period_year).
/// Unit: 'day' | 'hour' (DDL CHECK constraint).
///
/// DDL note: table has no audit columns. All AuditableEntity and SoftDeleteEntity
/// fields are ignored. Remaining is a C# computed property, not a DB column.
/// </summary>
public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("leave_balance", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id");
        builder.Property(x => x.PeriodYear).HasColumnName("period_year").IsRequired();
        builder.Property(x => x.Entitled).HasColumnName("entitled")
            .HasPrecision(7, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Used).HasColumnName("used")
            .HasPrecision(7, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(10)
            .HasDefaultValue("day").IsRequired();

        // Remaining is a C# computed property (Entitled - Used), not a DB column.
        builder.Ignore(x => x.Remaining);

        // ops.leave_balance DDL has no audit columns.
        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.IsDeleted);
    }
}
