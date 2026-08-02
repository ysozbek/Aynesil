using Aynesil.Domain.Modules.Consultancy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Consultancy;

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("institution", schema: "consultancy");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.InstitutionTypeId).HasColumnName("institution_type_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.City).HasColumnName("city");
        builder.Property(x => x.District).HasColumnName("district");
        builder.Property(x => x.ContactName).HasColumnName("contact_name");
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(50);
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
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

        // DB schema does not have created_by or updated_by columns.
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.Plans)
            .WithOne(p => p.Institution)
            .HasForeignKey(p => p.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Visits)
            .WithOne(v => v.Institution)
            .HasForeignKey(v => v.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
