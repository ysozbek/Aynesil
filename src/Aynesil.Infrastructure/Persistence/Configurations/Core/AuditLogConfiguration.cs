using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log", schema: "core");

        // PK is bigint generated always as identity at DB level. Composite PK (id, occurred_at)
        // for partition pruning. EF maps only id as the entity key.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.SchemaName).HasColumnName("schema_name").IsRequired();
        builder.Property(x => x.TableName).HasColumnName("table_name").IsRequired();
        builder.Property(x => x.RowId).HasColumnName("row_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(10).IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.OldData).HasColumnName("old_data").HasColumnType("jsonb");
        builder.Property(x => x.NewData).HasColumnName("new_data").HasColumnType("jsonb");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()").IsRequired();
    }
}
