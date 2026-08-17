namespace FinanceApp.Domain.Enums;

public enum ImportBatchStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    PartialCompleted = 4
}
