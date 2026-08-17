namespace FinanceApp.Application.Modules.MasterData.DTOs.Account;

public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }

    // 定期存款相关字段
    public DateTime? InterestStartDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal? InterestRate { get; set; }
    public bool AutoRenewal { get; set; } = false;
}
