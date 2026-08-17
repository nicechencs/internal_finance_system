namespace FinanceApp.Application.Modules.MasterData.DTOs.Project;

public class InitializeReceivablesRequest
{
    public string Mode { get; set; } = "once"; // "once" or "installment"
    public int InstallmentCount { get; set; }
    public List<ReceivableInstallmentDto> Installments { get; set; } = new();
}

public class ReceivableInstallmentDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
}
