using Aynesil.Domain.Modules.Legal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Legal;

public class StudentContractConfiguration : IEntityTypeConfiguration<StudentContract>
{
    public void Configure(EntityTypeBuilder<StudentContract> builder)
    {
        builder.ToTable("student_contract", schema: "legal");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.TemplateId).HasColumnName("template_id");
        builder.Property(x => x.TemplateVersion).HasColumnName("template_version");
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("draft")
            .IsRequired();

        builder.Property(x => x.SignedAt).HasColumnName("signed_at");
        builder.Property(x => x.SignedByName).HasColumnName("signed_by_name");
        builder.Property(x => x.SignatureMethod).HasColumnName("signature_method").HasMaxLength(20);
        builder.Property(x => x.SignatureRef).HasColumnName("signature_ref");
        builder.Property(x => x.SignedFileId).HasColumnName("signed_file_id");
        builder.Property(x => x.StartsOn).HasColumnName("starts_on");
        builder.Property(x => x.EndsOn).HasColumnName("ends_on");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsRequired()
            .IsConcurrencyToken();

        // DDL has no updated_by column.
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.CorporationId, x.StudentId })
            .HasDatabaseName("ix_student_contract_corp_student");

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
