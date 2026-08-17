using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class FixedDepositRecordConfiguration : IEntityTypeConfiguration<FixedDepositRecord>
{
    public void Configure(EntityTypeBuilder<FixedDepositRecord> builder)
    {
        builder.ToTable("fixed_deposit_records");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(e => e.Principal)
            .HasColumnName("principal")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.DepositDate)
            .HasColumnName("deposit_date")
            .IsRequired();

        builder.Property(e => e.MaturityDate)
            .HasColumnName("maturity_date")
            .IsRequired();

        builder.Property(e => e.TermMonths)
            .HasColumnName("term_months")
            .IsRequired();

        builder.Property(e => e.InterestRate)
            .HasColumnName("interest_rate")
            .HasColumnType("decimal(8,4)")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<FixedDepositStatus>(v, true));

        builder.Property(e => e.WithdrawalDate)
            .HasColumnName("withdrawal_date");

        builder.Property(e => e.ActualInterest)
            .HasColumnName("actual_interest")
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.IsEarlyWithdrawal)
            .HasColumnName("is_early_withdrawal")
            .IsRequired();

        builder.Property(e => e.DepositTransactionId)
            .HasColumnName("deposit_transaction_id")
            .IsRequired();

        builder.Property(e => e.WithdrawalTransactionId)
            .HasColumnName("withdrawal_transaction_id");

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

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
            .WithMany(a => a.FixedDepositRecords)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("idx_fixed_deposit_records_account_id")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_fixed_deposit_records_status")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.MaturityDate)
            .HasDatabaseName("idx_fixed_deposit_records_maturity_date")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_fixed_deposit_records_created_by")
            .HasFilter("is_deleted = false");
    }
}
