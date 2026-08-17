namespace FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;

public class GetFixedDepositsRequest
{
    public long[]? AccountIds { get; set; }
    public string? Status { get; set; }
    public bool IncludeWithdrawn { get; set; } = true;
}
