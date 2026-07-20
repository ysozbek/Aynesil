using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class ParentFeedbackConfiguration : IEntityTypeConfiguration<ParentFeedback>
{
    public void Configure(EntityTypeBuilder<ParentFeedback> builder)
    {
        builder.ToTable("parent_feedback", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");
        builder.Property(x => x.EducatorId).HasColumnName("educator_id");
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.Rating).HasColumnName("rating");
        builder.Property(x => x.Comment).HasColumnName("comment");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        // ops.parent_feedback DDL: id, corporation_id, guardian_id, educator_id,
        // session_id, rating, comment, created_at — no other audit columns
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.IsDeleted);
    }
}
