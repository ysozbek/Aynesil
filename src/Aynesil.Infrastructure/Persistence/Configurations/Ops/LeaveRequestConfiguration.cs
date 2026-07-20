using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

/// <summary>
/// EF Core configuration for ops.leave_request.
/// Status: 'pending' | 'approved' | 'rejected' | 'cancelled' (DDL CHECK constraint).
/// Unit:   'day' | 'hour' (DDL CHECK constraint).
///
/// DDL notes:
///   - time_range is a GENERATED ALWAYS column — ignored in EF (no C# backing field).
///   - No deleted_at column — soft-delete is not supported; Cancel() is the lifecycle end.
///   - No updated_by column — UpdatedBy is ignored.
///   - Overlap exclusion is enforced by DB EXCLUDE USING GIST; the handler also validates it.
/// </summary>
public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_request", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id");
        builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(10)
            .HasDefaultValue("day").IsRequired();
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(6, 2);
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20)
            .HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsRequired().IsConcurrencyToken();

        // ops.leave_request has no deleted_at, updated_by columns.
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        // time_range is a GENERATED ALWAYS AS column — not mapped to a C# property.

        builder.HasMany(x => x.Approvals)
            .WithOne(a => a.LeaveRequest)
            .HasForeignKey(a => a.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
