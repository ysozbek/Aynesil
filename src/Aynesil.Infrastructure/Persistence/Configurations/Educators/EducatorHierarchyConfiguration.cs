using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Educators;

/// <summary>
/// Maps educators.educator_hierarchy.
/// No audit columns. No soft delete.
/// Unique NULLS NOT DISTINCT: (educator_id, supervisor_id, relationship_id, campus_id).
/// Check constraint educator_id != supervisor_id is enforced at DB level.
/// </summary>
public class EducatorHierarchyConfiguration : IEntityTypeConfiguration<EducatorHierarchy>
{
    public void Configure(EntityTypeBuilder<EducatorHierarchy> builder)
    {
        builder.ToTable("educator_hierarchy", schema: "educators");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.SupervisorId).HasColumnName("supervisor_id").IsRequired();
        builder.Property(x => x.RelationshipId).HasColumnName("relationship_id");
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.ActiveFrom).HasColumnName("active_from").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.ActiveTo).HasColumnName("active_to");

        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => new { x.EducatorId, x.SupervisorId, x.RelationshipId, x.CampusId })
            .HasDatabaseName("educator_hierarchy_educator_id_supervisor_id_relationship_id_campus_id_key");
    }
}
