using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.Reporting;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reporting.DTOs.Report;
using FinanceApp.Application.Modules.Reporting.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class ReportControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<ILogger<ReportController>> _loggerMock;
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _loggerMock = new Mock<ILogger<ReportController>>();
        _controller = new ReportController(_reportServiceMock.Object, _loggerMock.Object);
    }

    #region GetMonthlyProfitReport

    [Fact]
    public async Task GetMonthlyProfitReport_ValidRequest_ReturnsOkWithReport()
    {
        // Arrange
        var year = 2026;
        var month = 3;
        var expectedReport = new MonthlyProfitReportDto
        {
            Year = year,
            Month = month,
            TotalIncome = 100000.00m,
            TotalExpense = 60000.00m,
            NetProfit = 40000.00m,
            ProfitRate = 40.00m,
            IncomeByCategory = new List<CategoryAmountDto>
            {
                new CategoryAmountDto { CategoryName = "项目收入", Amount = 100000.00m }
            },
            ExpenseByCategory = new List<CategoryAmountDto>
            {
                new CategoryAmountDto { CategoryName = "人员成本", Amount = 40000.00m },
                new CategoryAmountDto { CategoryName = "运营费用", Amount = 20000.00m }
            }
        };

        _reportServiceMock
            .Setup(x => x.GetMonthlyProfitReportAsync(year, month))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetMonthlyProfitReport(year, month);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<MonthlyProfitReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Year.Should().Be(year);
        apiResponse.Data.Month.Should().Be(month);
        apiResponse.Data.TotalIncome.Should().Be(100000.00m);
        apiResponse.Data.TotalExpense.Should().Be(60000.00m);
        apiResponse.Data.NetProfit.Should().Be(40000.00m);
        apiResponse.Data.ProfitRate.Should().Be(40.00m);
        apiResponse.Data.IncomeByCategory.Should().HaveCount(1);
        apiResponse.Data.ExpenseByCategory.Should().HaveCount(2);

        _reportServiceMock.Verify(x => x.GetMonthlyProfitReportAsync(year, month), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyProfitReport_EmptyProjectProfits_ReturnsOkWithEmptyData()
    {
        // Arrange
        var year = 2026;
        var month = 1;
        var expectedReport = new MonthlyProfitReportDto
        {
            Year = year,
            Month = month,
            TotalIncome = 0m,
            TotalExpense = 0m,
            NetProfit = 0m,
            ProfitRate = 0m,
            IncomeByCategory = new List<CategoryAmountDto>(),
            ExpenseByCategory = new List<CategoryAmountDto>()
        };

        _reportServiceMock
            .Setup(x => x.GetMonthlyProfitReportAsync(year, month))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetMonthlyProfitReport(year, month);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<MonthlyProfitReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalIncome.Should().Be(0m);
        apiResponse.Data.IncomeByCategory.Should().BeEmpty();
        apiResponse.Data.ExpenseByCategory.Should().BeEmpty();

        _reportServiceMock.Verify(x => x.GetMonthlyProfitReportAsync(year, month), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyProfitReport_ServiceThrowsException_Rethrows()
    {
        // Arrange
        var year = 2026;
        var month = 3;

        _reportServiceMock
            .Setup(x => x.GetMonthlyProfitReportAsync(year, month))
            .ThrowsAsync(new InvalidOperationException("数据库连接失败"));

        // Act
        var act = async () => await _controller.GetMonthlyProfitReport(year, month);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("数据库连接失败");

        _reportServiceMock.Verify(x => x.GetMonthlyProfitReportAsync(year, month), Times.Once);
    }

    #endregion

    #region GetCashflowReport

    [Fact]
    public async Task GetCashflowReport_ValidRequest_ReturnsOkWithReport()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 3, 31);
        var expectedReport = new CashflowReportDto
        {
            StartDate = "2026-01-01",
            EndDate = "2026-03-31",
            OpeningBalance = 50000.00m,
            TotalIncome = 300000.00m,
            TotalExpense = 180000.00m,
            ClosingBalance = 170000.00m,
            MonthlyDetail = new List<MonthlyDetailDto>
            {
                new MonthlyDetailDto
                {
                    Month = "2026-01",
                    OpeningBalance = 50000.00m,
                    Income = 100000.00m,
                    Expense = 60000.00m,
                    ClosingBalance = 90000.00m
                },
                new MonthlyDetailDto
                {
                    Month = "2026-02",
                    OpeningBalance = 90000.00m,
                    Income = 100000.00m,
                    Expense = 60000.00m,
                    ClosingBalance = 130000.00m
                }
            }
        };

        _reportServiceMock
            .Setup(x => x.GetCashflowReportAsync(startDate, endDate))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetCashflowReport(startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CashflowReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.StartDate.Should().Be("2026-01-01");
        apiResponse.Data.EndDate.Should().Be("2026-03-31");
        apiResponse.Data.OpeningBalance.Should().Be(50000.00m);
        apiResponse.Data.ClosingBalance.Should().Be(170000.00m);
        apiResponse.Data.TotalIncome.Should().Be(300000.00m);
        apiResponse.Data.TotalExpense.Should().Be(180000.00m);
        apiResponse.Data.MonthlyDetail.Should().HaveCount(2);

        _reportServiceMock.Verify(x => x.GetCashflowReportAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetCashflowReport_EmptyMonthlyDetail_ReturnsOkWithEmptyList()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 1, 31);
        var expectedReport = new CashflowReportDto
        {
            StartDate = "2026-01-01",
            EndDate = "2026-01-31",
            OpeningBalance = 0m,
            TotalIncome = 0m,
            TotalExpense = 0m,
            ClosingBalance = 0m,
            MonthlyDetail = new List<MonthlyDetailDto>()
        };

        _reportServiceMock
            .Setup(x => x.GetCashflowReportAsync(startDate, endDate))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetCashflowReport(startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CashflowReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.MonthlyDetail.Should().BeEmpty();

        _reportServiceMock.Verify(x => x.GetCashflowReportAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetCashflowReport_ServiceThrowsException_Rethrows()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 3, 31);

        _reportServiceMock
            .Setup(x => x.GetCashflowReportAsync(startDate, endDate))
            .ThrowsAsync(new InvalidOperationException("查询失败"));

        // Act
        var act = async () => await _controller.GetCashflowReport(startDate, endDate);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("查询失败");

        _reportServiceMock.Verify(x => x.GetCashflowReportAsync(startDate, endDate), Times.Once);
    }

    #endregion

    #region GetProjectProfitReport

    [Fact]
    public async Task GetProjectProfitReport_ReturnsOkWithReport()
    {
        // Arrange
        var expectedReport = new ProjectProfitReportDto
        {
            Projects = new List<ProjectProfitItemDto>
            {
                new ProjectProfitItemDto
                {
                    ProjectId = 1,
                    ProjectName = "项目A",
                    CustomerName = "客户甲",
                    ContractAmount = 500000.00m,
                    ReceivedAmount = 300000.00m,
                    TotalCost = 200000.00m,
                    ProfitAmount = 100000.00m,
                    ProfitRate = 33.33m
                },
                new ProjectProfitItemDto
                {
                    ProjectId = 2,
                    ProjectName = "项目B",
                    CustomerName = "客户乙",
                    ContractAmount = 200000.00m,
                    ReceivedAmount = 200000.00m,
                    TotalCost = 150000.00m,
                    ProfitAmount = 50000.00m,
                    ProfitRate = 25.00m
                }
            },
            Summary = new ProjectProfitSummaryDto
            {
                TotalContract = 700000.00m,
                TotalReceived = 500000.00m,
                TotalCost = 350000.00m,
                TotalProfit = 150000.00m,
                AvgProfitRate = 29.17m
            }
        };

        _reportServiceMock
            .Setup(x => x.GetProjectProfitReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetProjectProfitReport();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProjectProfitReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Projects.Should().HaveCount(2);
        apiResponse.Data.Projects[0].ProjectName.Should().Be("项目A");
        apiResponse.Data.Summary.TotalProfit.Should().Be(150000.00m);
        apiResponse.Data.Summary.AvgProfitRate.Should().Be(29.17m);

        _reportServiceMock.Verify(x => x.GetProjectProfitReportAsync(), Times.Once);
    }

    [Fact]
    public async Task GetProjectProfitReport_NoProjects_ReturnsOkWithEmptyList()
    {
        // Arrange
        var expectedReport = new ProjectProfitReportDto
        {
            Projects = new List<ProjectProfitItemDto>(),
            Summary = new ProjectProfitSummaryDto()
        };

        _reportServiceMock
            .Setup(x => x.GetProjectProfitReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetProjectProfitReport();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProjectProfitReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Projects.Should().BeEmpty();

        _reportServiceMock.Verify(x => x.GetProjectProfitReportAsync(), Times.Once);
    }

    [Fact]
    public async Task GetProjectProfitReport_ServiceThrowsException_Rethrows()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.GetProjectProfitReportAsync())
            .ThrowsAsync(new InvalidOperationException("生成报表失败"));

        // Act
        var act = async () => await _controller.GetProjectProfitReport();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("生成报表失败");

        _reportServiceMock.Verify(x => x.GetProjectProfitReportAsync(), Times.Once);
    }

    #endregion

    #region GetPersonCostReport

    [Fact]
    public async Task GetPersonCostReport_ReturnsOkWithReport()
    {
        // Arrange
        var expectedReport = new PersonCostReportDto
        {
            Persons = new List<PersonCostItemDto>
            {
                new PersonCostItemDto
                {
                    PersonId = 1,
                    PersonName = "张三",
                    PersonType = "员工",
                    Salary = 10000.00m,
                    Commission = 2000.00m,
                    Reimbursement = 500.00m,
                    Dividend = 0.00m,
                    TotalCost = 12500.00m
                },
                new PersonCostItemDto
                {
                    PersonId = 2,
                    PersonName = "李四",
                    PersonType = "合伙人",
                    Salary = 15000.00m,
                    Commission = 0.00m,
                    Reimbursement = 800.00m,
                    Dividend = 5000.00m,
                    TotalCost = 20800.00m
                }
            },
            Summary = new PersonCostSummaryDto
            {
                TotalSalary = 25000.00m,
                TotalCommission = 2000.00m,
                TotalReimbursement = 1300.00m,
                TotalCost = 33300.00m
            }
        };

        _reportServiceMock
            .Setup(x => x.GetPersonCostReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetPersonCostReport();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PersonCostReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Persons.Should().HaveCount(2);
        apiResponse.Data.Persons[0].PersonName.Should().Be("张三");
        apiResponse.Data.Persons[1].PersonName.Should().Be("李四");
        apiResponse.Data.Summary.TotalSalary.Should().Be(25000.00m);
        apiResponse.Data.Summary.TotalCost.Should().Be(33300.00m);

        _reportServiceMock.Verify(x => x.GetPersonCostReportAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPersonCostReport_NoPersons_ReturnsOkWithEmptyList()
    {
        // Arrange
        var expectedReport = new PersonCostReportDto
        {
            Persons = new List<PersonCostItemDto>(),
            Summary = new PersonCostSummaryDto()
        };

        _reportServiceMock
            .Setup(x => x.GetPersonCostReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetPersonCostReport();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PersonCostReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Persons.Should().BeEmpty();

        _reportServiceMock.Verify(x => x.GetPersonCostReportAsync(), Times.Once);
    }

    #endregion

    #region GetSupplierExpenseReport

    [Fact]
    public async Task GetSupplierExpenseReport_ReturnsOkWithReport()
    {
        // Arrange
        var expectedReport = new SupplierExpenseReportDto
        {
            Suppliers = new List<SupplierExpenseItemDto>
            {
                new SupplierExpenseItemDto
                {
                    SupplierId = 1,
                    SupplierName = "供应商A",
                    TotalExpense = 50000.00m,
                    TransactionCount = 10,
                    Rank = 1
                },
                new SupplierExpenseItemDto
                {
                    SupplierId = 2,
                    SupplierName = "供应商B",
                    TotalExpense = 30000.00m,
                    TransactionCount = 5,
                    Rank = 2
                }
            },
            Summary = new SupplierExpenseSummaryDto
            {
                TotalExpense = 80000.00m,
                SupplierCount = 2
            }
        };

        _reportServiceMock
            .Setup(x => x.GetSupplierExpenseReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetSupplierExpenseReport();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<SupplierExpenseReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Suppliers.Should().HaveCount(2);
        apiResponse.Data.Suppliers[0].SupplierName.Should().Be("供应商A");
        apiResponse.Data.Suppliers[0].Rank.Should().Be(1);
        apiResponse.Data.Suppliers[1].TransactionCount.Should().Be(5);
        apiResponse.Data.Summary.TotalExpense.Should().Be(80000.00m);
        apiResponse.Data.Summary.SupplierCount.Should().Be(2);

        _reportServiceMock.Verify(x => x.GetSupplierExpenseReportAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSupplierExpenseReport_NoSuppliers_ReturnsOkWithEmptyList()
    {
        // Arrange
        var expectedReport = new SupplierExpenseReportDto
        {
            Suppliers = new List<SupplierExpenseItemDto>(),
            Summary = new SupplierExpenseSummaryDto
            {
                TotalExpense = 0m,
                SupplierCount = 0
            }
        };

        _reportServiceMock
            .Setup(x => x.GetSupplierExpenseReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetSupplierExpenseReport();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<SupplierExpenseReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Suppliers.Should().BeEmpty();
        apiResponse.Data.Summary.SupplierCount.Should().Be(0);

        _reportServiceMock.Verify(x => x.GetSupplierExpenseReportAsync(), Times.Once);
    }

    #endregion

    #region GetAnnualOverviewReport

    [Fact]
    public async Task GetAnnualOverviewReport_ValidRequest_ReturnsOkWithReport()
    {
        // Arrange
        var year = 2026;
        var expectedReport = new AnnualOverviewReportDto
        {
            Year = year,
            TotalIncome = 1200000.00m,
            TotalExpense = 720000.00m,
            NetProfit = 480000.00m,
            ProfitRate = 40.00m,
            TotalReceivable = 200000.00m,
            TotalPayable = 100000.00m,
            MonthlyTrend = new List<MonthlyTrendDto>
            {
                new MonthlyTrendDto { Month = 1, Income = 100000.00m, Expense = 60000.00m, Profit = 40000.00m },
                new MonthlyTrendDto { Month = 2, Income = 110000.00m, Expense = 65000.00m, Profit = 45000.00m },
                new MonthlyTrendDto { Month = 3, Income = 90000.00m, Expense = 55000.00m, Profit = 35000.00m }
            },
            TopProjects = new List<TopItemDto>
            {
                new TopItemDto { Id = 1, Name = "项目A", Amount = 500000.00m },
                new TopItemDto { Id = 2, Name = "项目B", Amount = 300000.00m }
            },
            TopCustomers = new List<TopItemDto>
            {
                new TopItemDto { Id = 1, Name = "客户甲", Amount = 600000.00m }
            },
            TopSuppliers = new List<TopItemDto>
            {
                new TopItemDto { Id = 1, Name = "供应商A", Amount = 200000.00m }
            }
        };

        _reportServiceMock
            .Setup(x => x.GetAnnualOverviewReportAsync(year))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetAnnualOverviewReport(year);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AnnualOverviewReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Year.Should().Be(year);
        apiResponse.Data.TotalIncome.Should().Be(1200000.00m);
        apiResponse.Data.TotalExpense.Should().Be(720000.00m);
        apiResponse.Data.NetProfit.Should().Be(480000.00m);
        apiResponse.Data.ProfitRate.Should().Be(40.00m);
        apiResponse.Data.TotalReceivable.Should().Be(200000.00m);
        apiResponse.Data.TotalPayable.Should().Be(100000.00m);
        apiResponse.Data.MonthlyTrend.Should().HaveCount(3);
        apiResponse.Data.TopProjects.Should().HaveCount(2);
        apiResponse.Data.TopCustomers.Should().HaveCount(1);
        apiResponse.Data.TopSuppliers.Should().HaveCount(1);

        _reportServiceMock.Verify(x => x.GetAnnualOverviewReportAsync(year), Times.Once);
    }

    [Fact]
    public async Task GetAnnualOverviewReport_EmptyYear_ReturnsOkWithEmptyData()
    {
        // Arrange
        var year = 2025;
        var expectedReport = new AnnualOverviewReportDto
        {
            Year = year,
            TotalIncome = 0m,
            TotalExpense = 0m,
            NetProfit = 0m,
            ProfitRate = 0m,
            TotalReceivable = 0m,
            TotalPayable = 0m,
            MonthlyTrend = new List<MonthlyTrendDto>(),
            TopProjects = new List<TopItemDto>(),
            TopCustomers = new List<TopItemDto>(),
            TopSuppliers = new List<TopItemDto>()
        };

        _reportServiceMock
            .Setup(x => x.GetAnnualOverviewReportAsync(year))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetAnnualOverviewReport(year);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AnnualOverviewReportDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Year.Should().Be(year);
        apiResponse.Data.MonthlyTrend.Should().BeEmpty();
        apiResponse.Data.TopProjects.Should().BeEmpty();
        apiResponse.Data.TopCustomers.Should().BeEmpty();
        apiResponse.Data.TopSuppliers.Should().BeEmpty();

        _reportServiceMock.Verify(x => x.GetAnnualOverviewReportAsync(year), Times.Once);
    }

    #endregion
}
