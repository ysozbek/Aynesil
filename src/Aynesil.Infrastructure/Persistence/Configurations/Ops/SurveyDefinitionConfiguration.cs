using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class SurveyDefinitionConfiguration : IEntityTypeConfiguration<SurveyDefinition>
{
    public void Configure(EntityTypeBuilder<SurveyDefinition> builder)
    {
        builder.ToTable("survey_definition", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.TypeId).HasColumnName("type_id");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Target).HasColumnName("target").HasMaxLength(20)
            .HasDefaultValue("guardian").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsRequired()
            .IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.Questions)
            .WithOne(q => q.Survey)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
