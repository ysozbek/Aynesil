using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.student.
/// Full audit (created_at, created_by, updated_at, updated_by, row_version).
/// Soft delete via deleted_at.
/// GIN trigram index on (first_name || ' ' || last_name) exists in DB (ix_student_name) —
/// no EF equivalent needed, EF will not recreate it.
/// </summary>
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("student", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentNo).HasColumnName("student_no");
        builder.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(x => x.NationalId).HasColumnName("national_id");
        builder.Property(x => x.BirthDate).HasColumnName("birth_date");
        builder.Property(x => x.Gender).HasColumnName("gender");
        builder.Property(x => x.PrimaryCampusId).HasColumnName("primary_campus_id");
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.LeadId).HasColumnName("lead_id");
        builder.Property(x => x.PhotoFileId).HasColumnName("photo_file_id");
        builder.Property(x => x.Notes).HasColumnName("notes");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.StudentNo })
            .IsUnique()
            .HasFilter("student_no IS NOT NULL")
            .HasDatabaseName("student_corporation_id_student_no_key");

        builder.HasIndex(x => new { x.CorporationId, x.StatusId })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_student_corp_status");

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Campuses)
            .WithOne()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Guardians)
            .WithOne()
            .HasForeignKey(sg => sg.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EmergencyContacts)
            .WithOne()
            .HasForeignKey(ec => ec.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DevelopmentalProfiles)
            .WithOne()
            .HasForeignKey(dp => dp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Diagnoses)
            .WithOne()
            .HasForeignKey(d => d.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MedicalReports)
            .WithOne()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DevelopmentReports)
            .WithOne()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ExternalInstitutionReports)
            .WithOne()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CaseNotes)
            .WithOne()
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.IsDeleted);
    }
}
