using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("bank_transactions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(e => e.ImportBatchId)
            .HasColumnName("import_batch_id");

        builder.Property(e => e.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(e => e.TransactionTime)
            .HasColumnName("transaction_time");

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Balance)
            .HasColumnName("balance")
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Direction)
            .HasColumnName("direction")
            .HasMaxLength(10)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<BankTransactionDirection>(v, true));

        builder.Property(e => e.Counterparty)
            .HasColumnName("counterparty")
            .HasMaxLength(200);

        builder.Property(e => e.CounterpartyAccount)
            .HasColumnName("counterparty_account")
            .HasMaxLength(50);

        builder.Property(e => e.CounterpartyBank)
            .HasColumnName("counterparty_bank")
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.Memo)
            .HasColumnName("memo");

        builder.Property(e => e.TransactionNumber)
            .HasColumnName("transaction_number")
            .HasMaxLength(100);

        builder.Property(e => e.UniqueHash)
            .HasColumnName("unique_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.IsProcessed)
            .HasColumnName("is_processed")
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
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ImportBatch)
            .WithMany()
            .HasForeignKey(e => e.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.UniqueHash)
            .HasDatabaseName("idx_bank_transactions_hash")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("idx_bank_transactions_account");

        builder.HasIndex(e => e.TransactionDate)
            .HasDatabaseName("idx_bank_transactions_date");

        builder.HasIndex(e => e.IsProcessed)
            .HasDatabaseName("idx_bank_transactions_processed");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_bank_transactions_created_by")
            .HasFilter("is_deleted = false");
    }
}
