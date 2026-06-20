using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class KpiValueConfiguration : IEntityTypeConfiguration<KpiValue>
{
    public void Configure(EntityTypeBuilder<KpiValue> builder)
    {
        builder.ToTable("kpi_value", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.KpiId).HasColumnName("kpi_id").IsRequired();
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(x => x.NumericValue).HasColumnName("numeric_value").HasColumnType("numeric(18,4)");
        builder.Property(x => x.Detail).HasColumnName("detail").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.ComputedAt).HasColumnName("computed_at").HasDefaultValueSql("now()").IsRequired();

        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);

        builder.HasIndex(x => new { x.KpiId, x.SubjectType, x.SubjectId, x.PeriodStart, x.PeriodEnd })
            .IsUnique()
            .HasDatabaseName("uq_kpi_value_period");
    }
}
