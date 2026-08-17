using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;
using FinanceApp.Application.Modules.Reporting.DTOs.Report;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Api.Tests.Integration;

public class ReportingAggregationIntegrationTests : IntegrationTestBase
{
    public ReportingAggregationIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ReportsDashboardAndStatistics_ShouldAggregateSeededDataThroughSqlPaths()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var year = 2026;
        var month = 3;
        var account = new Account
        {
            Name = "报表聚合账户",
            AccountNumber = "ACC-REPORT",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 20000m,
            CurrentBalance = 20000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var customer = new Customer { Name = "聚合客户", IsActive = true, CreatedAt = DateTime.UtcNow };
        var supplier = new Supplier { Name = "聚合供应商", IsActive = true, CreatedAt = DateTime.UtcNow };
        var incomeCategory = new Category { Name = "销售收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow };
        var expenseCategory = new Category { Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project
        {
            Name = "聚合项目",
            ProjectCode = "AGG-1",
            ContractAmount = 100000m,
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Accounts.Add(account);
        DbContext.Customers.Add(customer);
        DbContext.Suppliers.Add(supplier);
        DbContext.Categories.AddRange(incomeCategory, expenseCategory);
        DbContext.Projects.Add(project);
        await DbContext.SaveChangesAsync();

        project.CustomerId = customer.Id;
        DbContext.Projects.Update(project);

        var income = new Transaction
        {
            TransactionDate = new DateTime(year, month, 5),
            Amount = 8000m,
            TransactionType = TransactionType.Income,
            AccountId = account.Id,
            CategoryId = incomeCategory.Id,
            ProjectId = project.Id,
            CustomerId = customer.Id,
            Status = TransactionStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        var expense = new Transaction
        {
            TransactionDate = new DateTime(year, month, 8),
            Amount = 2000m,
            TransactionType = TransactionType.Expense,
            AccountId = account.Id,
            CategoryId = expenseCategory.Id,
            SupplierId = supplier.Id,
            Status = TransactionStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        var transfer = new Transaction
        {
            TransactionDate = new DateTime(year, month, 12),
            Amount = 1500m,
            TransactionType = TransactionType.Transfer,
            TransferDirection = TransferDirection.Out,
            AccountId = account.Id,
            Description = "转账至备用金",
            Status = TransactionStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Transactions.AddRange(income, expense, transfer);
        await DbContext.SaveChangesAsync();

        var receivableOpen = new Receivable
        {
            ProjectId = project.Id,
            CustomerId = customer.Id,
            TotalAmount = 10000m,
            ReceivedAmount = 6000m,
            RemainingAmount = 4000m,
            Status = ReceivableStatus.Partial,
            CreatedAt = DateTime.UtcNow
        };
        var receivableSettled = new Receivable
        {
            ProjectId = project.Id,
            CustomerId = customer.Id,
            TotalAmount = 3000m,
            ReceivedAmount = 3000m,
            RemainingAmount = 9999m,
            Status = ReceivableStatus.Settled,
            CreatedAt = DateTime.UtcNow
        };
        var payableOpen = new Payable
        {
            ProjectId = project.Id,
            SupplierId = supplier.Id,
            TotalAmount = 5000m,
            PaidAmount = 2000m,
            RemainingAmount = 2500m,
            Status = PayableStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var payableSettled = new Payable
        {
            ProjectId = project.Id,
            SupplierId = supplier.Id,
            TotalAmount = 1000m,
            PaidAmount = 1000m,
            RemainingAmount = 8888m,
            Status = PayableStatus.Settled,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Receivables.AddRange(receivableOpen, receivableSettled);
        DbContext.Payables.AddRange(payableOpen, payableSettled);
        await DbContext.SaveChangesAsync();

        DbContext.PayableDetails.Add(new PayableDetail
        {
            PayableId = payableOpen.Id,
            TransactionId = expense.Id,
            PaymentDate = new DateTime(year, month, 8),
            Amount = 2000m,
            CreatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var monthly = await GetAsync<ApiResponse<MonthlyProfitReportDto>>(
            $"/api/reports/monthly-profit?year={year}&month={month}");
        monthly!.Success.Should().BeTrue();
        monthly.Data!.TotalIncome.Should().Be(8000m);
        monthly.Data.TotalExpense.Should().Be(2000m);
        monthly.Data.NetProfit.Should().Be(6000m);
        monthly.Data.IncomeByCategory.Should().ContainSingle(x => x.CategoryName == "销售收入" && x.Amount == 8000m);
        monthly.Data.ExpenseByCategory.Should().ContainSingle(x => x.CategoryName == "办公费用" && x.Amount == 2000m);

        var cashflow = await GetAsync<ApiResponse<CashflowReportDto>>(
            $"/api/reports/cashflow?startDate={year}-03-01&endDate={year}-04-30");
        cashflow!.Data!.TotalIncome.Should().Be(8000m);
        cashflow.Data.TotalExpense.Should().Be(2000m);
        cashflow.Data.MonthlyDetail.Should().HaveCount(2);
        cashflow.Data.MonthlyDetail[0].Month.Should().Be("2026-03");
        cashflow.Data.MonthlyDetail[1].Month.Should().Be("2026-04");
        cashflow.Data.MonthlyDetail[1].Income.Should().Be(0m);
        cashflow.Data.MonthlyDetail[1].OpeningBalance.Should().Be(cashflow.Data.MonthlyDetail[0].ClosingBalance);

        var annual = await GetAsync<ApiResponse<AnnualOverviewReportDto>>($"/api/reports/annual-overview?year={year}");
        annual!.Data!.TotalIncome.Should().Be(8000m);
        annual.Data.TotalExpense.Should().Be(2000m);
        annual.Data.TotalReceivable.Should().Be(4000m);
        annual.Data.TotalPayable.Should().Be(2500m);
        annual.Data.MonthlyTrend.Should().HaveCount(12);
        annual.Data.TopProjects.Should().ContainSingle(x => x.Name == "聚合项目" && x.Amount == 8000m);
        annual.Data.TopCustomers.Should().ContainSingle(x => x.Name == "聚合客户");
        annual.Data.TopSuppliers.Should().ContainSingle(x => x.Name == "聚合供应商");

        var projectProfit = await GetAsync<ApiResponse<ProjectProfitReportDto>>("/api/reports/project-profit");
        projectProfit!.Data!.Projects.Should().ContainSingle(p => p.ProjectId == project.Id);
        var item = projectProfit.Data.Projects[0];
        item.ReceivedAmount.Should().Be(9000m);
        item.TotalCost.Should().Be(2000m);
        item.ProfitAmount.Should().Be(7000m);

        var stats = await GetAsync<ApiResponse<TransactionStatisticsDto>>("/api/transactions/statistics");
        stats!.Data!.TotalIncome.Should().Be(8000m);
        stats.Data.TotalExpense.Should().Be(2000m);
        stats.Data.TotalTransfer.Should().Be(1500m);
        stats.Data.IncomeCount.Should().Be(1);
        stats.Data.ExpenseCount.Should().Be(1);
        stats.Data.TransferCount.Should().Be(1);
        stats.Data.TotalCount.Should().Be(3);

        var dashboard = await GetAsync<ApiResponse<List<MonthlyStatsDto>>>("/api/dashboard/monthly-stats?months=3");
        dashboard!.Success.Should().BeTrue();
        dashboard.Data.Should().HaveCount(3);
    }
}
