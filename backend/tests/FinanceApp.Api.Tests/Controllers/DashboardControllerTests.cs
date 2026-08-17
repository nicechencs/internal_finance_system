using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.Reporting;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;
using FinanceApp.Application.Modules.Reporting.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_dashboardServiceMock.Object, _loggerMock.Object);
    }

    #region GetSummary

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummaryData()
    {
        // Arrange
        var expectedSummary = new DashboardSummaryDto
        {
            TotalIncome = 100000.00m,
            TotalExpense = 60000.00m,
            NetProfit = 40000.00m,
            TotalBalance = 150000.00m,
            AccountCount = 5,
            TransactionCount = 120,
            ProjectCount = 8
        };

        _dashboardServiceMock
            .Setup(x => x.GetSummaryAsync())
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DashboardSummaryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalIncome.Should().Be(100000.00m);
        apiResponse.Data.TotalExpense.Should().Be(60000.00m);
        apiResponse.Data.NetProfit.Should().Be(40000.00m);
        apiResponse.Data.TotalBalance.Should().Be(150000.00m);
        apiResponse.Data.AccountCount.Should().Be(5);
        apiResponse.Data.TransactionCount.Should().Be(120);
        apiResponse.Data.ProjectCount.Should().Be(8);

        _dashboardServiceMock.Verify(x => x.GetSummaryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSummary_ServiceThrowsException_Throws()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetSummaryAsync())
            .ThrowsAsync(new Exception("数据库连接失败"));

        // Act
        Func<Task> act = async () => await _controller.GetSummary();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("数据库连接失败");

        _dashboardServiceMock.Verify(x => x.GetSummaryAsync(), Times.Once);
    }

    #endregion

    #region GetMonthlyStats

    [Fact]
    public async Task GetMonthlyStats_DefaultMonths_ReturnsOkWithStats()
    {
        // Arrange
        var expectedStats = new List<MonthlyStatsDto>
        {
            new MonthlyStatsDto { Month = "2026-01", Income = 50000.00m, Expense = 30000.00m, Net = 20000.00m },
            new MonthlyStatsDto { Month = "2026-02", Income = 60000.00m, Expense = 35000.00m, Net = 25000.00m },
            new MonthlyStatsDto { Month = "2026-03", Income = 45000.00m, Expense = 28000.00m, Net = 17000.00m }
        };

        _dashboardServiceMock
            .Setup(x => x.GetMonthlyStatsAsync(12))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetMonthlyStats();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<MonthlyStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(3);
        apiResponse.Data[0].Month.Should().Be("2026-01");
        apiResponse.Data[0].Income.Should().Be(50000.00m);
        apiResponse.Data[0].Expense.Should().Be(30000.00m);
        apiResponse.Data[0].Net.Should().Be(20000.00m);

        _dashboardServiceMock.Verify(x => x.GetMonthlyStatsAsync(12), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyStats_CustomMonths_ReturnsOkWithStats()
    {
        // Arrange
        var expectedStats = new List<MonthlyStatsDto>
        {
            new MonthlyStatsDto { Month = "2026-01", Income = 50000.00m, Expense = 30000.00m, Net = 20000.00m },
            new MonthlyStatsDto { Month = "2026-02", Income = 60000.00m, Expense = 35000.00m, Net = 25000.00m },
            new MonthlyStatsDto { Month = "2026-03", Income = 45000.00m, Expense = 28000.00m, Net = 17000.00m }
        };

        _dashboardServiceMock
            .Setup(x => x.GetMonthlyStatsAsync(6))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetMonthlyStats(6);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<MonthlyStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(3);

        _dashboardServiceMock.Verify(x => x.GetMonthlyStatsAsync(6), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyStats_ReturnsEmptyList_ReturnsOkWithEmptyData()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetMonthlyStatsAsync(12))
            .ReturnsAsync(new List<MonthlyStatsDto>());

        // Act
        var result = await _controller.GetMonthlyStats();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<MonthlyStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();

        _dashboardServiceMock.Verify(x => x.GetMonthlyStatsAsync(12), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyStats_ServiceThrowsException_Throws()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetMonthlyStatsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("查询失败"));

        // Act
        Func<Task> act = async () => await _controller.GetMonthlyStats();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("查询失败");

        _dashboardServiceMock.Verify(x => x.GetMonthlyStatsAsync(12), Times.Once);
    }

    #endregion

    #region GetExpenseByCategory

    [Fact]
    public async Task GetExpenseByCategory_NoDates_ReturnsOkWithStats()
    {
        // Arrange
        var expectedStats = new List<CategoryStatsDto>
        {
            new CategoryStatsDto { CategoryName = "办公费用", Amount = 15000.00m, Percentage = 30.00m },
            new CategoryStatsDto { CategoryName = "差旅费用", Amount = 20000.00m, Percentage = 40.00m },
            new CategoryStatsDto { CategoryName = "人员工资", Amount = 15000.00m, Percentage = 30.00m }
        };

        _dashboardServiceMock
            .Setup(x => x.GetExpenseByCategoryAsync(null, null))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetExpenseByCategory(null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(3);
        apiResponse.Data[0].CategoryName.Should().Be("办公费用");
        apiResponse.Data[0].Amount.Should().Be(15000.00m);
        apiResponse.Data[0].Percentage.Should().Be(30.00m);

        _dashboardServiceMock.Verify(x => x.GetExpenseByCategoryAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetExpenseByCategory_WithDateRange_ReturnsOkWithStats()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 3, 31);
        var expectedStats = new List<CategoryStatsDto>
        {
            new CategoryStatsDto { CategoryName = "办公费用", Amount = 8000.00m, Percentage = 50.00m },
            new CategoryStatsDto { CategoryName = "差旅费用", Amount = 8000.00m, Percentage = 50.00m }
        };

        _dashboardServiceMock
            .Setup(x => x.GetExpenseByCategoryAsync(startDate, endDate))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetExpenseByCategory(startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _dashboardServiceMock.Verify(x => x.GetExpenseByCategoryAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetExpenseByCategory_ReturnsEmptyList_ReturnsOkWithEmptyData()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetExpenseByCategoryAsync(null, null))
            .ReturnsAsync(new List<CategoryStatsDto>());

        // Act
        var result = await _controller.GetExpenseByCategory(null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();

        _dashboardServiceMock.Verify(x => x.GetExpenseByCategoryAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetExpenseByCategory_ServiceThrowsException_Throws()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetExpenseByCategoryAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("分类统计查询失败"));

        // Act
        Func<Task> act = async () => await _controller.GetExpenseByCategory(null, null);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("分类统计查询失败");

        _dashboardServiceMock.Verify(x => x.GetExpenseByCategoryAsync(null, null), Times.Once);
    }

    #endregion

    #region GetIncomeByCategory

    [Fact]
    public async Task GetIncomeByCategory_NoDates_ReturnsOkWithStats()
    {
        // Arrange
        var expectedStats = new List<CategoryStatsDto>
        {
            new CategoryStatsDto { CategoryName = "项目收入", Amount = 80000.00m, Percentage = 60.00m },
            new CategoryStatsDto { CategoryName = "咨询收入", Amount = 30000.00m, Percentage = 22.50m },
            new CategoryStatsDto { CategoryName = "其他收入", Amount = 23333.00m, Percentage = 17.50m }
        };

        _dashboardServiceMock
            .Setup(x => x.GetIncomeByCategoryAsync(null, null))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetIncomeByCategory(null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(3);
        apiResponse.Data[0].CategoryName.Should().Be("项目收入");
        apiResponse.Data[0].Amount.Should().Be(80000.00m);
        apiResponse.Data[0].Percentage.Should().Be(60.00m);

        _dashboardServiceMock.Verify(x => x.GetIncomeByCategoryAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetIncomeByCategory_WithDateRange_ReturnsOkWithStats()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 3, 31);
        var expectedStats = new List<CategoryStatsDto>
        {
            new CategoryStatsDto { CategoryName = "项目收入", Amount = 50000.00m, Percentage = 100.00m }
        };

        _dashboardServiceMock
            .Setup(x => x.GetIncomeByCategoryAsync(startDate, endDate))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetIncomeByCategory(startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(1);

        _dashboardServiceMock.Verify(x => x.GetIncomeByCategoryAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetIncomeByCategory_ReturnsEmptyList_ReturnsOkWithEmptyData()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetIncomeByCategoryAsync(null, null))
            .ReturnsAsync(new List<CategoryStatsDto>());

        // Act
        var result = await _controller.GetIncomeByCategory(null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryStatsDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();

        _dashboardServiceMock.Verify(x => x.GetIncomeByCategoryAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetIncomeByCategory_ServiceThrowsException_Throws()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetIncomeByCategoryAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("收入分类统计查询失败"));

        // Act
        Func<Task> act = async () => await _controller.GetIncomeByCategory(null, null);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("收入分类统计查询失败");

        _dashboardServiceMock.Verify(x => x.GetIncomeByCategoryAsync(null, null), Times.Once);
    }

    #endregion

    #region GetRecentTransactions

    [Fact]
    public async Task GetRecentTransactions_DefaultCount_ReturnsOkWithTransactions()
    {
        // Arrange
        var expectedTransactions = new List<RecentTransactionDto>
        {
            new RecentTransactionDto
            {
                Id = 1,
                TransactionDate = new DateTime(2026, 3, 14),
                Type = "收入",
                Amount = 50000.00m,
                AccountName = "工商银行主账户",
                CategoryName = "项目收入",
                CounterpartyName = "客户A",
                Description = "项目款到账"
            },
            new RecentTransactionDto
            {
                Id = 2,
                TransactionDate = new DateTime(2026, 3, 13),
                Type = "支出",
                Amount = 3000.00m,
                AccountName = "工商银行主账户",
                CategoryName = "办公费用",
                CounterpartyName = "供应商B",
                Description = "办公用品采购"
            }
        };

        _dashboardServiceMock
            .Setup(x => x.GetRecentTransactionsAsync(10))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetRecentTransactions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<RecentTransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data[0].Id.Should().Be(1);
        apiResponse.Data[0].Type.Should().Be("收入");
        apiResponse.Data[0].Amount.Should().Be(50000.00m);
        apiResponse.Data[0].AccountName.Should().Be("工商银行主账户");
        apiResponse.Data[0].CategoryName.Should().Be("项目收入");
        apiResponse.Data[0].CounterpartyName.Should().Be("客户A");
        apiResponse.Data[0].Description.Should().Be("项目款到账");

        _dashboardServiceMock.Verify(x => x.GetRecentTransactionsAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetRecentTransactions_CustomCount_ReturnsOkWithTransactions()
    {
        // Arrange
        var expectedTransactions = new List<RecentTransactionDto>
        {
            new RecentTransactionDto
            {
                Id = 1,
                TransactionDate = new DateTime(2026, 3, 14),
                Type = "收入",
                Amount = 50000.00m,
                AccountName = "工商银行主账户"
            }
        };

        _dashboardServiceMock
            .Setup(x => x.GetRecentTransactionsAsync(5))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetRecentTransactions(5);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<RecentTransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(1);

        _dashboardServiceMock.Verify(x => x.GetRecentTransactionsAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetRecentTransactions_ReturnsEmptyList_ReturnsOkWithEmptyData()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetRecentTransactionsAsync(10))
            .ReturnsAsync(new List<RecentTransactionDto>());

        // Act
        var result = await _controller.GetRecentTransactions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<RecentTransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();

        _dashboardServiceMock.Verify(x => x.GetRecentTransactionsAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetRecentTransactions_WithNullOptionalFields_ReturnsOkWithTransactions()
    {
        // Arrange
        var expectedTransactions = new List<RecentTransactionDto>
        {
            new RecentTransactionDto
            {
                Id = 1,
                TransactionDate = new DateTime(2026, 3, 14),
                Type = "支出",
                Amount = 1000.00m,
                AccountName = "现金账户",
                CategoryName = null,
                CounterpartyName = null,
                Description = null
            }
        };

        _dashboardServiceMock
            .Setup(x => x.GetRecentTransactionsAsync(10))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _controller.GetRecentTransactions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<RecentTransactionDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(1);
        apiResponse.Data[0].CategoryName.Should().BeNull();
        apiResponse.Data[0].CounterpartyName.Should().BeNull();
        apiResponse.Data[0].Description.Should().BeNull();

        _dashboardServiceMock.Verify(x => x.GetRecentTransactionsAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetRecentTransactions_ServiceThrowsException_Throws()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetRecentTransactionsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("获取最近交易失败"));

        // Act
        Func<Task> act = async () => await _controller.GetRecentTransactions();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("获取最近交易失败");

        _dashboardServiceMock.Verify(x => x.GetRecentTransactionsAsync(10), Times.Once);
    }

    #endregion
}
