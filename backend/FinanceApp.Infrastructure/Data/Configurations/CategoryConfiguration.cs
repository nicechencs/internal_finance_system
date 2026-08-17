using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ParentId)
            .HasColumnName("parent_id");

        builder.Property(e => e.CategoryType)
            .HasColumnName("category_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<CategoryType>(v, true));

        builder.Property(e => e.Level)
            .HasColumnName("level")
            .IsRequired();

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
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

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        // Relationships
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.ParentId)
            .HasDatabaseName("idx_categories_parent")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CategoryType)
            .HasDatabaseName("idx_categories_type")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_categories_created_by")
            .HasFilter("is_deleted = false");
    }
}
