using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TagBindingConfiguration : IEntityTypeConfiguration<TagBinding>
{
    public void Configure(EntityTypeBuilder<TagBinding> builder)
    {
        builder.ToTable("tag_bindings");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.TagId)
            .HasColumnName("tag_id")
            .IsRequired();

        builder.Property(e => e.OwnerType)
            .HasColumnName("owner_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<TagScope>(v, true));

        builder.Property(e => e.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

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

        // Relationships
        builder.HasOne(b => b.Tag)
            .WithMany(t => t.Bindings)
            .HasForeignKey(b => b.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.TagId, e.OwnerType, e.OwnerId })
            .HasDatabaseName("idx_tag_bindings_tag")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => new { e.OwnerType, e.OwnerId, e.TagId })
            .IsUnique()
            .HasDatabaseName("ux_tag_bindings_owner_tag")
            .HasFilter("is_deleted = false");
    }
}
