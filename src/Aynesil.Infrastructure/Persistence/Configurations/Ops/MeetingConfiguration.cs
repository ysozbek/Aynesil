using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("meeting", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.MeetingTypeId).HasColumnName("meeting_type_id");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(x => x.EndsAt).HasColumnName("ends_at");
        builder.Property(x => x.Location).HasColumnName("location");
        builder.Property(x => x.RoomId).HasColumnName("room_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20)
            .HasDefaultValue("scheduled").IsRequired();
        builder.Property(x => x.OrganizerId).HasColumnName("organizer_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsRequired()
            .IsConcurrencyToken();

        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.Participants)
            .WithOne(p => p.Meeting)
            .HasForeignKey(p => p.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
