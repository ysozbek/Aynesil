using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampConfiguration : IEntityTypeConfiguration<Camp>
{
    public void Configure(EntityTypeBuilder<Camp> builder)
    {
        builder.ToTable("camp", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.CampTypeId).HasColumnName("camp_type_id");
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Location).HasColumnName("location");
        builder.Property(x => x.Capacity).HasColumnName("capacity");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

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

        builder.HasIndex(x => new { x.CorporationId, x.Code }).IsUnique();

        builder.HasMany(x => x.Periods)
            .WithOne(p => p.Camp)
            .HasForeignKey(p => p.CampId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
