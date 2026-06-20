using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class ReportRunConfiguration : IEntityTypeConfiguration<ReportRun>
{
    public void Configure(EntityTypeBuilder<ReportRun> builder)
    {
        builder.ToTable("report_run", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.ReportDefinitionId).HasColumnName("report_definition_id").IsRequired();
        builder.Property(x => x.Params).HasColumnName("params").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("running").IsRequired();
        builder.Property(x => x.ResultFileId).HasColumnName("result_file_id");
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.RequestedBy).HasColumnName("requested_by");

        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
    }
}
