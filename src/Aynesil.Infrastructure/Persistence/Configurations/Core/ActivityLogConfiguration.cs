using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_log", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.ActivityType).HasColumnName("activity_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.TargetSchema).HasColumnName("target_schema");
        builder.Property(x => x.TargetTable).HasColumnName("target_table");
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.Context).HasColumnName("context").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnName("ip_address");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()").IsRequired();
    }
}
