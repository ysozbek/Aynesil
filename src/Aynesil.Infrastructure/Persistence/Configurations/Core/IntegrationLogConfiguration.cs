using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
{
    public void Configure(EntityTypeBuilder<IntegrationLog> builder)
    {
        builder.ToTable("integration_log", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.ConnectionId).HasColumnName("connection_id");
        builder.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(10).IsRequired();
        builder.Property(x => x.Request).HasColumnName("request").HasColumnType("jsonb");
        builder.Property(x => x.Response).HasColumnName("response").HasColumnType("jsonb");
        builder.Property(x => x.StatusCode).HasColumnName("status_code");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()").IsRequired();
    }
}
