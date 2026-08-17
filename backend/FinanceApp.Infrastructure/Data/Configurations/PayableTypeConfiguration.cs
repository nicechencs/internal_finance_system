using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class PayableTypeConfiguration : IEntityTypeConfiguration<PayableType>
{
    public void Configure(EntityTypeBuilder<PayableType> builder)
    {
        builder.ToTable("payable_types");

        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Id).HasColumnName("id");

        builder.Property(pt => pt.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pt => pt.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(pt => pt.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(pt => pt.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pt => pt.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pt => pt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(pt => pt.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(pt => pt.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(pt => pt.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(pt => pt.CreatedBy)
            .HasColumnName("created_by");

        builder.HasOne(pt => pt.CreatedByUser)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pt => pt.Code)
            .IsUnique()
            .HasDatabaseName("idx_payable_types_code")
            .HasFilter("code IS NOT NULL");

        builder.HasIndex(pt => pt.SortOrder)
            .HasDatabaseName("idx_payable_types_sort_order");

        builder.HasIndex(pt => pt.CreatedBy)
            .HasDatabaseName("idx_payable_types_created_by")
            .HasFilter("is_deleted = false");
    }
}
