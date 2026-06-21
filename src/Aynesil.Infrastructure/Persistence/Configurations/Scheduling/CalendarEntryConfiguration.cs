using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.calendar_entry.
/// School-wide or campus-specific calendar events (holidays, closures, events, term breaks).
/// Minimal audit: created_at only. No soft delete, no row_version.
/// </summary>
public class CalendarEntryConfiguration : IEntityTypeConfiguration<CalendarEntry>
{
    public void Configure(EntityTypeBuilder<CalendarEntry> builder)
    {
        builder.ToTable("calendar_entry", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.EntryType).HasColumnName("entry_type").HasDefaultValue("holiday").IsRequired();
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").IsRequired();
        builder.Property(x => x.IsAllDay).HasColumnName("is_all_day").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => new { x.CorporationId, x.StartsAt })
            .HasDatabaseName("ix_calendar_entry_corp_date");
    }
}
