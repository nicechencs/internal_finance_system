namespace FinanceApp.Application.Modules.Reporting.DTOs.Report;

public class PersonCostReportDto
{
    public List<PersonCostItemDto> Persons { get; set; } = new();
    public PersonCostSummaryDto Summary { get; set; } = new();
}

public class PersonCostItemDto
{
    public long PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string PersonType { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public decimal Commission { get; set; }
    public decimal Reimbursement { get; set; }
    public decimal Dividend { get; set; }
    public decimal TotalCost { get; set; }
}

public class PersonCostSummaryDto
{
    public decimal TotalSalary { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalReimbursement { get; set; }
    public decimal TotalCost { get; set; }
}
