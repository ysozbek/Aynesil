using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class ConsultancyAgreementConfiguration : IEntityTypeConfiguration<ConsultancyAgreement>
{
    public void Configure(EntityTypeBuilder<ConsultancyAgreement> builder)
    {
        builder.ToTable("consultancy_agreement", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.ConsultancyPlanId).HasColumnName("consultancy_plan_id").IsRequired();
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.AgreementTypeId).HasColumnName("agreement_type_id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.EndDate).HasColumnName("end_date");
        builder.Property(x => x.SignedDate).HasColumnName("signed_date");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("draft")
            .IsRequired();
        builder.Property(x => x.FileId).HasColumnName("file_id");
        builder.Property(x => x.SignedByName).HasColumnName("signed_by_name").HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.IsSigned);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Plan)
            .WithMany(p => p.Agreements)
            .HasForeignKey(x => x.ConsultancyPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
