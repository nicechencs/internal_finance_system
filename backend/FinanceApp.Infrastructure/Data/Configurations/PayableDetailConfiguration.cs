using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class PayableDetailConfiguration : IEntityTypeConfiguration<PayableDetail>
{
    public void Configure(EntityTypeBuilder<PayableDetail> builder)
    {
        builder.ToTable("payable_details");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.PayableId)
            .HasColumnName("payable_id")
            .IsRequired();

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(e => e.PaymentDate)
            .HasColumnName("payment_date")
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.PaymentMethod)
            .HasColumnName("payment_method")
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasColumnName("description");

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
        builder.HasOne(e => e.Payable)
            .WithMany(e => e.Details)
            .HasForeignKey(e => e.PayableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.PayableId)
            .HasDatabaseName("idx_payable_details_payable");

        builder.HasIndex(e => new { e.PayableId, e.TransactionId })
            .IsUnique()
            .HasDatabaseName("ux_payable_details_payable_transaction")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.PaymentDate)
            .HasDatabaseName("idx_payable_details_date");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_payable_details_created_by")
            .HasFilter("is_deleted = false");
    }
}
