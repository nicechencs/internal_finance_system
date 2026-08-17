using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.AccountType)
            .HasColumnName("account_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<AccountType>(v, true));

        builder.Property(e => e.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(50);

        builder.Property(e => e.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(100);

        builder.Property(e => e.OpeningBalance)
            .HasColumnName("opening_balance")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.CurrentBalance)
            .HasColumnName("current_balance")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasColumnName("currency")
            .HasMaxLength(10)
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

        // 乐观并发版本列（提供商无关的显式并发令牌）
        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        // Relationships
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // 定期存款相关字段
        builder.Property(e => e.InterestStartDate)
            .HasColumnName("interest_start_date");

        builder.Property(e => e.MaturityDate)
            .HasColumnName("maturity_date");

        builder.Property(e => e.InterestRate)
            .HasColumnName("interest_rate")
            .HasColumnType("decimal(8,4)");

        builder.Property(e => e.AutoRenewal)
            .HasColumnName("auto_renewal")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(e => e.AccountType)
            .HasDatabaseName("idx_accounts_type")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_accounts_active")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.MaturityDate)
            .HasDatabaseName("idx_accounts_maturity_date")
            .HasFilter("is_deleted = false AND account_type = 'fixeddeposit'");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_accounts_created_by")
            .HasFilter("is_deleted = false");
    }
}
