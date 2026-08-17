using FluentAssertions;
using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Modules.Reporting.Models;
using FinanceApp.Application.Modules.Reporting.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class ReportServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IRepository<Person>> _personRepositoryMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepositoryMock;
    private readonly Mock<IRepository<Customer>> _customerRepositoryMock;
    private readonly Mock<IRepository<Account>> _accountRepositoryMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock;
    private readonly Mock<IProjectFinancialSummaryService> _projectFinancialSummaryServiceMock;
    private readonly Mock<ILogger<ReportService>> _loggerMock;
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _personRepositoryMock = new Mock<IRepository<Person>>();
        _supplierRepositoryMock = new Mock<IRepository<Supplier>>();
        _customerRepositoryMock = new Mock<IRepository<Customer>>();
        _accountRepositoryMock = new Mock<IRepository<Account>>();
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _payableRepositoryMock = new Mock<IRepository<Payable>>();
        _projectFinancialSummaryServiceMock = new Mock<IProjectFinancialSummaryService>();
        _loggerMock = new Mock<ILogger<ReportService>>();

        _service = new ReportService(
            _transactionRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _personRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableRepositoryMock.Object,
            _projectFinancialSummaryServiceMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService()
        );
    }

    #region GetMonthlyProfitReportAsync Tests

    [Fact]
    public async Task GetMonthlyProfitReportAsync_WithValidData_ShouldReturnCorrectReport()
    {
        // Arrange
        var year = 2024;
        var month = 3;
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var category1 = new Category { Id = 1, Name = "销售收入", CategoryType = CategoryType.Income };
        var category2 = new Category { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 10000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, month, 15),
                CategoryId = 1,
                Category = category1,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 5000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, month, 20),
                CategoryId = 1,
                Category = category1,
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 3000m,
                TransactionType = TransactionType.Expense,
                TransactionDate = new DateTime(year, month, 10),
                CategoryId = 2,
                Category = category2,
                IsDeleted = false
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetMonthlyProfitReportAsync(year, month);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(year);
        result.Month.Should().Be(month);
        result.TotalIncome.Should().Be(15000m);
        result.TotalExpense.Should().Be(3000m);
        result.NetProfit.Should().Be(12000m);
        result.ProfitRate.Should().BeApproximately(80m, 0.01m);
        result.IncomeByCategory.Should().HaveCount(1);
        result.IncomeByCategory[0].CategoryName.Should().Be("销售收入");
        result.IncomeByCategory[0].Amount.Should().Be(15000m);
        result.ExpenseByCategory.Should().HaveCount(1);
        result.ExpenseByCategory[0].CategoryName.Should().Be("办公费用");
        result.ExpenseByCategory[0].Amount.Should().Be(3000m);
    }

    [Fact]
    public async Task GetMonthlyProfitReportAsync_WithNoTransactions_ShouldReturnZeroValues()
    {
        // Arrange
        var year = 2024;
        var month = 3;
        var transactions = new List<Transaction>();

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetMonthlyProfitReportAsync(year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.ProfitRate.Should().Be(0m);
        result.IncomeByCategory.Should().BeEmpty();
        result.ExpenseByCategory.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlyProfitReportAsync_WithOnlyExpenses_ShouldReturnNegativeProfit()
    {
        // Arrange
        var year = 2024;
        var month = 3;
        var category = new Category { Id = 1, Name = "办公费用", CategoryType = CategoryType.Expense };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 5000m,
                TransactionType = TransactionType.Expense,
                TransactionDate = new DateTime(year, month, 10),
                CategoryId = 1,
                Category = category,
                IsDeleted = false
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetMonthlyProfitReportAsync(year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(5000m);
        result.NetProfit.Should().Be(-5000m);
        result.ProfitRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetMonthlyProfitReportAsync_ShouldExcludeTransfersAndUncategorizedFromCategoryBreakdown()
    {
        var year = 2024;
        var month = 3;
        var incomeCategory = new Category { Id = 1, Name = "销售收入", CategoryType = CategoryType.Income };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 10000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, month, 5),
                CategoryId = 1,
                Category = incomeCategory,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 4000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, month, 8),
                CategoryId = null,
                Category = null,
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 9999m,
                TransactionType = TransactionType.Transfer,
                TransactionDate = new DateTime(year, month, 12),
                Description = "转账至备用金",
                IsDeleted = false
            }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        var result = await _service.GetMonthlyProfitReportAsync(year, month);

        result.TotalIncome.Should().Be(14000m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(14000m);
        result.IncomeByCategory.Should().ContainSingle();
        result.IncomeByCategory[0].CategoryName.Should().Be("销售收入");
        result.IncomeByCategory[0].Amount.Should().Be(10000m);
        result.ExpenseByCategory.Should().BeEmpty();
    }

    #endregion

    #region GetCashflowReportAsync Tests

    [Fact]
    public async Task GetCashflowReportAsync_WithValidData_ShouldReturnCorrectReport()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 3, 31);

        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "银行账户1", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsDeleted = false },
            new() { Id = 2, Name = "银行账户2", AccountType = AccountType.Bank, CurrentBalance = 5000m, IsDeleted = false }
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 8000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(2024, 1, 15),
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 3000m,
                TransactionType = TransactionType.Expense,
                TransactionDate = new DateTime(2024, 2, 10),
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 5000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(2024, 3, 5),
                IsDeleted = false
            }
        };

        var accountQueryableMock = accounts.AsQueryable().BuildMock();
        _accountRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(accountQueryableMock.Object);

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCashflowReportAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().Be("2024-01-01");
        result.EndDate.Should().Be("2024-03-31");
        result.OpeningBalance.Should().Be(15000m);
        result.TotalIncome.Should().Be(13000m);
        result.TotalExpense.Should().Be(3000m);
        result.ClosingBalance.Should().Be(25000m);
        result.MonthlyDetail.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCashflowReportAsync_WithMonthlyDetails_ShouldCalculateCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 2, 28);

        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "银行账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsDeleted = false }
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 5000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(2024, 1, 15),
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 2000m,
                TransactionType = TransactionType.Expense,
                TransactionDate = new DateTime(2024, 1, 20),
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 3000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(2024, 2, 10),
                IsDeleted = false
            }
        };

        var accountQueryableMock = accounts.AsQueryable().BuildMock();
        _accountRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(accountQueryableMock.Object);

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCashflowReportAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.MonthlyDetail.Should().HaveCount(2);

        var jan = result.MonthlyDetail[0];
        jan.Month.Should().Be("2024-01");
        jan.OpeningBalance.Should().Be(10000m);
        jan.Income.Should().Be(5000m);
        jan.Expense.Should().Be(2000m);
        jan.ClosingBalance.Should().Be(13000m);

        var feb = result.MonthlyDetail[1];
        feb.Month.Should().Be("2024-02");
        feb.OpeningBalance.Should().Be(13000m);
        feb.Income.Should().Be(3000m);
        feb.Expense.Should().Be(0m);
        feb.ClosingBalance.Should().Be(16000m);
    }

    [Fact]
    public async Task GetCashflowReportAsync_ShouldKeepEmptyMonthsAndExcludeTransfers()
    {
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 3, 31);

        _accountRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Account>
            {
                new() { Id = 1, Name = "主账户", AccountType = AccountType.Bank, CurrentBalance = 20000m, IsDeleted = false }
            }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 1,
                    Amount = 5000m,
                    TransactionType = TransactionType.Income,
                    TransactionDate = new DateTime(2024, 1, 10),
                    IsDeleted = false
                },
                new()
                {
                    Id = 2,
                    Amount = 8000m,
                    TransactionType = TransactionType.Transfer,
                    TransactionDate = new DateTime(2024, 2, 15),
                    Description = "转账至账户B",
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        var result = await _service.GetCashflowReportAsync(startDate, endDate);

        result.TotalIncome.Should().Be(5000m);
        result.TotalExpense.Should().Be(0m);
        result.OpeningBalance.Should().Be(20000m);
        result.ClosingBalance.Should().Be(25000m);
        result.MonthlyDetail.Should().HaveCount(3);

        result.MonthlyDetail[0].Month.Should().Be("2024-01");
        result.MonthlyDetail[0].Income.Should().Be(5000m);
        result.MonthlyDetail[0].Expense.Should().Be(0m);
        result.MonthlyDetail[0].ClosingBalance.Should().Be(25000m);

        result.MonthlyDetail[1].Month.Should().Be("2024-02");
        result.MonthlyDetail[1].Income.Should().Be(0m);
        result.MonthlyDetail[1].Expense.Should().Be(0m);
        result.MonthlyDetail[1].OpeningBalance.Should().Be(25000m);
        result.MonthlyDetail[1].ClosingBalance.Should().Be(25000m);

        result.MonthlyDetail[2].Month.Should().Be("2024-03");
        result.MonthlyDetail[2].Income.Should().Be(0m);
        result.MonthlyDetail[2].OpeningBalance.Should().Be(25000m);
        result.MonthlyDetail[2].ClosingBalance.Should().Be(25000m);
    }

    #endregion

    #region GetProjectProfitReportAsync Tests

    [Fact]
    public async Task GetProjectProfitReportAsync_WithValidData_ShouldReturnCorrectReport()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "客户A", IsDeleted = false };

        var projects = new List<Project>
        {
            new()
            {
                Id = 1,
                Name = "项目A",
                CustomerId = 1,
                Customer = customer,
                ContractAmount = 100000m,
                ReceivedAmount = 80000m,
                TotalCost = 60000m,
                ProfitAmount = 20000m,
                ProfitRate = 25m,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Name = "项目B",
                CustomerId = 1,
                Customer = customer,
                ContractAmount = 50000m,
                ReceivedAmount = 50000m,
                TotalCost = 30000m,
                ProfitAmount = 20000m,
                ProfitRate = 40m,
                IsDeleted = false
            }
        };

        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);
        _projectFinancialSummaryServiceMock
            .Setup(x => x.GetProjectSummariesAsync(It.IsAny<IReadOnlyCollection<long>>()))
            .ReturnsAsync(new Dictionary<long, ProjectFinancialSummary>
            {
                [1] = new ProjectFinancialSummary
                {
                    ProjectId = 1,
                    ContractAmount = 100000m,
                    ReceivedAmount = 80000m,
                    TotalCost = 60000m,
                    ProfitAmount = 20000m,
                    ProfitRate = 25m
                },
                [2] = new ProjectFinancialSummary
                {
                    ProjectId = 2,
                    ContractAmount = 50000m,
                    ReceivedAmount = 50000m,
                    TotalCost = 30000m,
                    ProfitAmount = 20000m,
                    ProfitRate = 40m
                }
            });

        // Act
        var result = await _service.GetProjectProfitReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().HaveCount(2);
        result.Summary.TotalContract.Should().Be(150000m);
        result.Summary.TotalReceived.Should().Be(130000m);
        result.Summary.TotalCost.Should().Be(90000m);
        result.Summary.TotalProfit.Should().Be(40000m);
        result.Summary.AvgProfitRate.Should().BeApproximately(32.5m, 0.01m);

        _projectFinancialSummaryServiceMock.Verify(
            x => x.GetProjectSummariesAsync(It.Is<IReadOnlyCollection<long>>(ids => ids.Count == 2)),
            Times.Once);
        _projectFinancialSummaryServiceMock.Verify(
            x => x.GetProjectSummaryAsync(It.IsAny<long>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProjectProfitReportAsync_WithNoProjects_ShouldReturnEmptyReport()
    {
        // Arrange
        var projects = new List<Project>();

        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);
        _projectFinancialSummaryServiceMock
            .Setup(x => x.GetProjectSummariesAsync(It.IsAny<IReadOnlyCollection<long>>()))
            .ReturnsAsync(new Dictionary<long, ProjectFinancialSummary>());

        // Act
        var result = await _service.GetProjectProfitReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().BeEmpty();
        result.Summary.TotalContract.Should().Be(0m);
        result.Summary.TotalReceived.Should().Be(0m);
        result.Summary.TotalCost.Should().Be(0m);
        result.Summary.TotalProfit.Should().Be(0m);
        result.Summary.AvgProfitRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetProjectProfitReportAsync_WithDirtyProjectFields_ShouldUseSummaryServiceValues()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "Customer-A", IsDeleted = false };
        var projects = new List<Project>
        {
            new()
            {
                Id = 1001,
                Name = "Project-A",
                CustomerId = 1,
                Customer = customer,
                ContractAmount = 200000m,
                ReceivedAmount = 1m,
                TotalCost = 1m,
                ProfitAmount = 1m,
                ProfitRate = 1m,
                IsDeleted = false
            }
        };

        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);
        _projectFinancialSummaryServiceMock
            .Setup(x => x.GetProjectSummariesAsync(It.IsAny<IReadOnlyCollection<long>>()))
            .ReturnsAsync(new Dictionary<long, ProjectFinancialSummary>
            {
                [1001] = new ProjectFinancialSummary
                {
                    ProjectId = 1001,
                    ContractAmount = 200000m,
                    ReceivedAmount = 180000m,
                    TotalCost = 120000m,
                    ProfitAmount = 60000m,
                    ProfitRate = 33.33m
                }
            });

        // Act
        var result = await _service.GetProjectProfitReportAsync();

        // Assert
        result.Projects.Should().HaveCount(1);
        result.Projects[0].ContractAmount.Should().Be(200000m);
        result.Projects[0].ReceivedAmount.Should().Be(180000m);
        result.Projects[0].TotalCost.Should().Be(120000m);
        result.Projects[0].ProfitAmount.Should().Be(60000m);
        result.Projects[0].ProfitRate.Should().Be(33.33m);

        result.Summary.TotalContract.Should().Be(200000m);
        result.Summary.TotalReceived.Should().Be(180000m);
        result.Summary.TotalCost.Should().Be(120000m);
        result.Summary.TotalProfit.Should().Be(60000m);
        result.Summary.AvgProfitRate.Should().Be(33.33m);
    }

    #endregion

    #region GetPersonCostReportAsync Tests

    [Fact]
    public async Task GetPersonCostReportAsync_WithValidData_ShouldReturnCorrectReport()
    {
        // Arrange
        var persons = new List<Person>
        {
            new()
            {
                Id = 1,
                Name = "张三",
                PersonType = PersonType.Employee,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Name = "李四",
                PersonType = PersonType.Shareholder,
                IsDeleted = false
            }
        };

        var salaryCategory = new Category { Id = 1, Name = "工资", CategoryType = CategoryType.Expense };
        var commissionCategory = new Category { Id = 2, Name = "提成", CategoryType = CategoryType.Expense };
        var reimbursementCategory = new Category { Id = 3, Name = "报销", CategoryType = CategoryType.Expense };
        var dividendCategory = new Category { Id = 4, Name = "分红", CategoryType = CategoryType.Expense };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 5000m,
                TransactionType = TransactionType.Expense,
                PersonId = 1,
                CategoryId = 1,
                Category = salaryCategory,
                Person = persons[0],
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 1000m,
                TransactionType = TransactionType.Expense,
                PersonId = 1,
                CategoryId = 2,
                Category = commissionCategory,
                Person = persons[0],
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 500m,
                TransactionType = TransactionType.Expense,
                PersonId = 1,
                CategoryId = 3,
                Category = reimbursementCategory,
                Person = persons[0],
                IsDeleted = false
            },
            new()
            {
                Id = 4,
                Amount = 10000m,
                TransactionType = TransactionType.Expense,
                PersonId = 2,
                CategoryId = 4,
                Category = dividendCategory,
                Person = persons[1],
                IsDeleted = false
            }
        };

        var personQueryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(personQueryableMock.Object);

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        // Act
        var result = await _service.GetPersonCostReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Persons.Should().HaveCount(2);

        var person1 = result.Persons.First(p => p.PersonId == 1);
        person1.Salary.Should().Be(5000m);
        person1.Commission.Should().Be(1000m);
        person1.Reimbursement.Should().Be(500m);
        person1.Dividend.Should().Be(0m);
        person1.TotalCost.Should().Be(6500m);

        var person2 = result.Persons.First(p => p.PersonId == 2);
        person2.Dividend.Should().Be(10000m);
        person2.TotalCost.Should().Be(10000m);

        result.Summary.TotalSalary.Should().Be(5000m);
        result.Summary.TotalCommission.Should().Be(1000m);
        result.Summary.TotalReimbursement.Should().Be(500m);
        result.Summary.TotalCost.Should().Be(16500m);
    }

    [Fact]
    public async Task GetPersonCostReportAsync_WithNoTransactions_ShouldReturnZeroCosts()
    {
        // Arrange
        var persons = new List<Person>
        {
            new()
            {
                Id = 1,
                Name = "张三",
                PersonType = PersonType.Employee,
                IsDeleted = false
            }
        };

        var transactions = new List<Transaction>();

        var personQueryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(personQueryableMock.Object);

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        // Act
        var result = await _service.GetPersonCostReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Persons.Should().HaveCount(1);
        result.Persons[0].TotalCost.Should().Be(0m);
        result.Summary.TotalCost.Should().Be(0m);
    }

    [Fact]
    public async Task GetPersonCostReportAsync_ShouldMatchSalaryAndCommissionAliases()
    {
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "王五", PersonType = PersonType.Employee, IsDeleted = false }
        };
        var salaryAlias = new Category { Id = 1, Name = "基本薪资", CategoryType = CategoryType.Expense };
        var commissionAlias = new Category { Id = 2, Name = "销售佣金", CategoryType = CategoryType.Expense };

        _personRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(persons.AsQueryable().BuildMock().Object);
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 1,
                    PersonId = 1,
                    Amount = 8000m,
                    TransactionType = TransactionType.Expense,
                    Category = salaryAlias,
                    IsDeleted = false
                },
                new()
                {
                    Id = 2,
                    PersonId = 1,
                    Amount = 1500m,
                    TransactionType = TransactionType.Expense,
                    Category = commissionAlias,
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        var result = await _service.GetPersonCostReportAsync();

        result.Persons.Should().ContainSingle();
        result.Persons[0].Salary.Should().Be(8000m);
        result.Persons[0].Commission.Should().Be(1500m);
        result.Persons[0].TotalCost.Should().Be(9500m);
        result.Summary.TotalSalary.Should().Be(8000m);
        result.Summary.TotalCommission.Should().Be(1500m);
        result.Summary.TotalCost.Should().Be(9500m);
    }

    #endregion

    #region GetSupplierExpenseReportAsync Tests

    [Fact]
    public async Task GetSupplierExpenseReportAsync_WithValidData_ShouldReturnCorrectReport()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "供应商A", IsDeleted = false },
            new() { Id = 2, Name = "供应商B", IsDeleted = false },
            new() { Id = 3, Name = "供应商C", IsDeleted = false }
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 10000m,
                TransactionType = TransactionType.Expense,
                SupplierId = 1,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 5000m,
                TransactionType = TransactionType.Expense,
                SupplierId = 1,
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 8000m,
                TransactionType = TransactionType.Expense,
                SupplierId = 2,
                IsDeleted = false
            }
        };

        var supplierQueryableMock = suppliers.AsQueryable().BuildMock();
        _supplierRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(supplierQueryableMock.Object);

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        // Act
        var result = await _service.GetSupplierExpenseReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Suppliers.Should().HaveCount(2);
        result.Suppliers[0].Rank.Should().Be(1);
        result.Suppliers[0].SupplierId.Should().Be(1);
        result.Suppliers[0].TotalExpense.Should().Be(15000m);
        result.Suppliers[0].TransactionCount.Should().Be(2);
        result.Suppliers[1].Rank.Should().Be(2);
        result.Suppliers[1].SupplierId.Should().Be(2);
        result.Suppliers[1].TotalExpense.Should().Be(8000m);
        result.Summary.TotalExpense.Should().Be(23000m);
        result.Summary.SupplierCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSupplierExpenseReportAsync_WithNoExpenses_ShouldReturnEmptyReport()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "供应商A", IsDeleted = false }
        };

        var transactions = new List<Transaction>();

        var supplierQueryableMock = suppliers.AsQueryable().BuildMock();
        _supplierRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(supplierQueryableMock.Object);

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        // Act
        var result = await _service.GetSupplierExpenseReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Suppliers.Should().BeEmpty();
        result.Summary.TotalExpense.Should().Be(0m);
        result.Summary.SupplierCount.Should().Be(0);
    }

    #endregion

    #region GetAnnualOverviewReportAsync Tests

    [Fact]
    public async Task GetAnnualOverviewReportAsync_WithValidData_ShouldReturnCorrectReport()
    {
        // Arrange
        var year = 2024;
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year + 1, 1, 1);

        var project = new Project { Id = 1, Name = "项目A", IsDeleted = false };
        var customer = new Customer { Id = 1, Name = "客户A", IsDeleted = false };
        var supplier = new Supplier { Id = 1, Name = "供应商A", IsDeleted = false };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 50000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, 1, 15),
                ProjectId = 1,
                Project = project,
                CustomerId = 1,
                Customer = customer,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 30000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, 6, 10),
                ProjectId = 1,
                Project = project,
                CustomerId = 1,
                Customer = customer,
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 20000m,
                TransactionType = TransactionType.Expense,
                TransactionDate = new DateTime(year, 3, 5),
                SupplierId = 1,
                Supplier = supplier,
                IsDeleted = false
            }
        };

        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                TotalAmount = 10000m,
                RemainingAmount = 5000m,
                Status = ReceivableStatus.Partial,
                IsDeleted = false
            }
        };

        var payables = new List<Payable>
        {
            new()
            {
                Id = 1,
                TotalAmount = 8000m,
                RemainingAmount = 3000m,
                Status = PayableStatus.Partial,
                IsDeleted = false
            }
        };

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        var receivableQueryableMock = receivables.AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(receivableQueryableMock.Object);

        var payableQueryableMock = payables.AsQueryable().BuildMock();
        _payableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(payableQueryableMock.Object);

        // Act
        var result = await _service.GetAnnualOverviewReportAsync(year);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(year);
        result.TotalIncome.Should().Be(80000m);
        result.TotalExpense.Should().Be(20000m);
        result.NetProfit.Should().Be(60000m);
        result.ProfitRate.Should().BeApproximately(75m, 0.01m);
        result.TotalReceivable.Should().Be(5000m);
        result.TotalPayable.Should().Be(3000m);
        result.MonthlyTrend.Should().HaveCount(12);
        result.TopProjects.Should().HaveCount(1);
        result.TopCustomers.Should().HaveCount(1);
        result.TopSuppliers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAnnualOverviewReportAsync_WithMonthlyTrend_ShouldCalculateCorrectly()
    {
        // Arrange
        var year = 2024;

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 10000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, 1, 15),
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 5000m,
                TransactionType = TransactionType.Expense,
                TransactionDate = new DateTime(year, 1, 20),
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Amount = 8000m,
                TransactionType = TransactionType.Income,
                TransactionDate = new DateTime(year, 2, 10),
                IsDeleted = false
            }
        };

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        var receivableQueryableMock = new List<Receivable>().AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(receivableQueryableMock.Object);

        var payableQueryableMock = new List<Payable>().AsQueryable().BuildMock();
        _payableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(payableQueryableMock.Object);

        // Act
        var result = await _service.GetAnnualOverviewReportAsync(year);

        // Assert
        result.Should().NotBeNull();
        result.MonthlyTrend.Should().HaveCount(12);

        var jan = result.MonthlyTrend.First(m => m.Month == 1);
        jan.Income.Should().Be(10000m);
        jan.Expense.Should().Be(5000m);
        jan.Profit.Should().Be(5000m);

        var feb = result.MonthlyTrend.First(m => m.Month == 2);
        feb.Income.Should().Be(8000m);
        feb.Expense.Should().Be(0m);
        feb.Profit.Should().Be(8000m);

        var mar = result.MonthlyTrend.First(m => m.Month == 3);
        mar.Income.Should().Be(0m);
        mar.Expense.Should().Be(0m);
        mar.Profit.Should().Be(0m);
    }

    [Fact]
    public async Task GetAnnualOverviewReportAsync_WithTopItems_ShouldReturnTop10()
    {
        // Arrange
        var year = 2024;

        var projects = Enumerable.Range(1, 15).Select(i => new Project
        {
            Id = i,
            Name = $"项目{i}",
            IsDeleted = false
        }).ToList();

        var transactions = Enumerable.Range(1, 15).Select(i => new Transaction
        {
            Id = i,
            Amount = i * 1000m,
            TransactionType = TransactionType.Income,
            TransactionDate = new DateTime(year, 1, 1),
            ProjectId = i,
            Project = projects[i - 1],
            IsDeleted = false
        }).ToList();

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        var receivableQueryableMock = new List<Receivable>().AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(receivableQueryableMock.Object);

        var payableQueryableMock = new List<Payable>().AsQueryable().BuildMock();
        _payableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(payableQueryableMock.Object);

        // Act
        var result = await _service.GetAnnualOverviewReportAsync(year);

        // Assert
        result.Should().NotBeNull();
        result.TopProjects.Should().HaveCount(10);
        result.TopProjects[0].Amount.Should().Be(15000m);
        result.TopProjects[9].Amount.Should().Be(6000m);
    }

    [Fact]
    public async Task GetAnnualOverviewReportAsync_WithNoData_ShouldReturnEmptyReport()
    {
        // Arrange
        var year = 2024;

        var transactionQueryableMock = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        var receivableQueryableMock = new List<Receivable>().AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(receivableQueryableMock.Object);

        var payableQueryableMock = new List<Payable>().AsQueryable().BuildMock();
        _payableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(payableQueryableMock.Object);

        // Act
        var result = await _service.GetAnnualOverviewReportAsync(year);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.ProfitRate.Should().Be(0m);
        result.TopProjects.Should().BeEmpty();
        result.TopCustomers.Should().BeEmpty();
        result.TopSuppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAnnualOverviewReportAsync_ShouldIgnoreSettledReceivablesAndPayables()
    {
        var year = 2024;
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 1,
                    Amount = 1000m,
                    TransactionType = TransactionType.Income,
                    TransactionDate = new DateTime(year, 4, 1),
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Receivable>
            {
                new() { Id = 1, RemainingAmount = 4000m, Status = ReceivableStatus.Partial, IsDeleted = false },
                new() { Id = 2, RemainingAmount = 9999m, Status = ReceivableStatus.Settled, IsDeleted = false }
            }.AsQueryable().BuildMock().Object);

        _payableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Payable>
            {
                new() { Id = 1, RemainingAmount = 2500m, Status = PayableStatus.Pending, IsDeleted = false },
                new() { Id = 2, RemainingAmount = 8888m, Status = PayableStatus.Settled, IsDeleted = false }
            }.AsQueryable().BuildMock().Object);

        var result = await _service.GetAnnualOverviewReportAsync(year);

        result.TotalIncome.Should().Be(1000m);
        result.TotalReceivable.Should().Be(4000m);
        result.TotalPayable.Should().Be(2500m);
        result.MonthlyTrend.Should().HaveCount(12);
    }

    #endregion
}
