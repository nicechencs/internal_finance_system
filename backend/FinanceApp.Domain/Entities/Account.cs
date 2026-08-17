using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Domain.Entities;

public class Account : BaseEntity, IConcurrencyVersioned
{
    /// <summary>乐观并发版本号（由 AppDbContext 自动维护）</summary>
    public long Version { get; set; }

    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal Balance => CurrentBalance;
    public string Currency { get; set; } = "CNY";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // 定期存款相关字段
    public DateTime? InterestStartDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal? InterestRate { get; set; }
    public bool AutoRenewal { get; set; } = false;

    // 导航属性
    public ICollection<FixedDepositRecord> FixedDepositRecords { get; set; } = new List<FixedDepositRecord>();
}
