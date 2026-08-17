using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class ReceivableTypeConfiguration : IEntityTypeConfiguration<ReceivableType>
{
    public void Configure(EntityTypeBuilder<ReceivableType> builder)
    {
        builder.ToTable("receivable_types");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");

        builder.Property(rt => rt.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rt => rt.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(rt => rt.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(rt => rt.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(rt => rt.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rt => rt.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(rt => rt.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(rt => rt.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(rt => rt.CreatedBy)
            .HasColumnName("created_by");

        builder.HasOne(rt => rt.CreatedByUser)
            .WithMany()
            .HasForeignKey(rt => rt.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rt => rt.Code)
            .IsUnique()
            .HasDatabaseName("idx_receivable_types_code")
            .HasFilter("code IS NOT NULL");

        builder.HasIndex(rt => rt.SortOrder)
            .HasDatabaseName("idx_receivable_types_sort_order");

        builder.HasIndex(rt => rt.CreatedBy)
            .HasDatabaseName("idx_receivable_types_created_by")
            .HasFilter("is_deleted = false");
    }
}
