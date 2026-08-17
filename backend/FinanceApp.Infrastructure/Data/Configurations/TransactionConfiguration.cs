using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.BankTransactionId)
            .HasColumnName("bank_transaction_id");

        builder.Property(e => e.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.TransactionType)
            .HasColumnName("transaction_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<TransactionType>(v, true));

        builder.Property(e => e.TransferDirection)
            .HasColumnName("transfer_direction")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<TransferDirection>(v, true));

        builder.Property(e => e.CategoryId)
            .HasColumnName("category_id");

        builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(e => e.SupplierId)
            .HasColumnName("supplier_id");

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id");

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<TransactionStatus>(v, true));

        builder.Property(e => e.IsAllocated)
            .HasColumnName("is_allocated")
            .IsRequired();

        builder.Property(e => e.AllocationStatus)
            .HasColumnName("allocation_status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<AllocationStatus>(v, true));

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(e => e.RelatedTransactionId)
            .HasColumnName("related_transaction_id");

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
        builder.HasOne(e => e.BankTransaction)
            .WithMany()
            .HasForeignKey(e => e.BankTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RelatedTransaction)
            .WithMany()
            .HasForeignKey(e => e.RelatedTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ReceivableDetails)
            .WithOne(rd => rd.Transaction)
            .HasForeignKey(rd => rd.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.PayableDetails)
            .WithOne(pd => pd.Transaction)
            .HasForeignKey(pd => pd.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.TagBindings);

        // Indexes
        builder.HasIndex(e => e.TransactionDate)
            .HasDatabaseName("idx_transactions_date")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.TransactionType)
            .HasDatabaseName("idx_transactions_type")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("idx_transactions_category")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("idx_transactions_account")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("idx_transactions_project")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CustomerId)
            .HasDatabaseName("idx_transactions_customer")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.SupplierId)
            .HasDatabaseName("idx_transactions_supplier")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("idx_transactions_person")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.AllocationStatus)
            .HasDatabaseName("idx_transactions_allocation_status")
            .HasFilter("is_deleted = false");
    }
}
