using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

/// <summary>
/// EF Core configuration for ops.leave_approval.
/// decision: 'approved' | 'rejected' | 'pending' (DDL CHECK constraint).
///
/// DDL note: table has no audit columns beyond corporation_id.
/// All AuditableEntity and SoftDeleteEntity fields are ignored.
/// </summary>
public class LeaveApprovalConfiguration : IEntityTypeConfiguration<LeaveApproval>
{
    public void Configure(EntityTypeBuilder<LeaveApproval> builder)
    {
        builder.ToTable("leave_approval", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.LeaveRequestId).HasColumnName("leave_request_id").IsRequired();
        builder.Property(x => x.StepNo).HasColumnName("step_no").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.ApproverId).HasColumnName("approver_id");
        builder.Property(x => x.Decision).HasColumnName("decision").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Comment).HasColumnName("comment");
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at");

        // ops.leave_approval DDL has no audit columns.
        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.IsDeleted);

        builder.HasOne(x => x.LeaveRequest)
            .WithMany(lr => lr.Approvals)
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
