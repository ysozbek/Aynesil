using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Iam;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_account", schema: "iam");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.PreferredLocale).HasColumnName("preferred_locale").HasMaxLength(20);
        builder.Property(x => x.PrimaryCampusId).HasColumnName("primary_campus_id");
        builder.Property(x => x.MfaEnabled).HasColumnName("mfa_enabled").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.MfaSecret).HasColumnName("mfa_secret");
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.Username }).IsUnique();
        builder.HasIndex(x => new { x.CorporationId, x.Email })
            .IsUnique()
            .HasFilter("email IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("uq_user_email");

        builder.HasMany(x => x.Roles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Sessions).WithOne(s => s.User).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ExternalIdentities).WithOne(ui => ui.User).HasForeignKey(ui => ui.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
