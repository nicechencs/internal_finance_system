using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.NormalizedUsername)
            .HasColumnName("normalized_username")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(100);

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<UserRole>(v, true));

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(e => e.MustChangePassword)
            .HasColumnName("must_change_password")
            .IsRequired();

        builder.Property(e => e.AccessFailedCount)
            .HasColumnName("access_failed_count")
            .IsRequired();

        builder.Property(e => e.LockoutEndAt)
            .HasColumnName("lockout_end_at");

        builder.Property(e => e.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(e => e.PasswordChangedAt)
            .HasColumnName("password_changed_at")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        // 忽略 CreatedBy 字段（User 表不需要此字段）
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.CreatedByUser);

        // Indexes
        builder.HasIndex(e => e.Username)
            .HasDatabaseName("idx_users_username")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(e => e.NormalizedUsername)
            .HasDatabaseName("idx_users_normalized_username")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(e => new { e.IsDeleted, e.DeletedAt })
            .HasDatabaseName("idx_users_deleted");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_users_active")
            .HasFilter("is_deleted = false");
    }
}
