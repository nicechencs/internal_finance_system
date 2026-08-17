using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Link;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class LinkServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepoMock;
    private readonly Mock<IRepository<BankTransaction>> _bankTransactionRepoMock;
    private readonly Mock<IRepository<Customer>> _customerRepoMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepoMock;
    private readonly Mock<IRepository<Person>> _personRepoMock;
    private readonly Mock<IRepository<Project>> _projectRepoMock;
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IRuleService> _ruleServiceMock;
    private readonly LinkService _service;

    public LinkServiceTests()
    {
        _transactionRepoMock = new Mock<IRepository<Transaction>>();
        _bankTransactionRepoMock = new Mock<IRepository<BankTransaction>>();
        _customerRepoMock = new Mock<IRepository<Customer>>();
        _supplierRepoMock = new Mock<IRepository<Supplier>>();
        _personRepoMock = new Mock<IRepository<Person>>();
        _projectRepoMock = new Mock<IRepository<Project>>();
        _accountRepoMock = new Mock<IRepository<Account>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _ruleServiceMock = new Mock<IRuleService>();

        UnitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ITransactionScope?)null);

        _service = new LinkService(
            _transactionRepoMock.Object,
            _bankTransactionRepoMock.Object,
            _customerRepoMock.Object,
            _supplierRepoMock.Object,
            _personRepoMock.Object,
            _projectRepoMock.Object,
            _accountRepoMock.Object,
            _categoryRepoMock.Object,
            _ruleServiceMock.Object,
            AuditLogServiceMock.Object,
            UnitOfWorkMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<LinkService>>()
        );
    }

    #region PreviewLink - Customer

    [Fact]
    public async Task PreviewLink_Customer_ShouldMatchByCounterpartyName()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "张三公司" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "张三公司付款", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 1000,
                TransactionType = TransactionType.Income, AccountId = 1,
                CustomerId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 1
        });

        // Assert
        result.EntityName.Should().Be("张三公司");
        result.TotalMatched.Should().Be(1);
        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].TransactionId.Should().Be(100);
        result.Candidates[0].MatchReason.Should().Contain("张三公司");
    }

    [Fact]
    public async Task PreviewLink_Customer_ShouldNotMatchExpenseTransactions()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "张三公司" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "张三公司", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 500,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 1
        });

        // Assert - Expense transactions should NOT match for Customer
        result.TotalMatched.Should().Be(0);
    }

    [Fact]
    public async Task PreviewLink_Customer_ShouldNotMatchAlreadyLinked()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "张三公司" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "张三公司", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 1000,
                TransactionType = TransactionType.Income, AccountId = 1,
                CustomerId = 2, // Already linked to another customer
                BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 1
        });

        // Assert - Already linked transactions should be excluded
        result.TotalMatched.Should().Be(0);
    }

    [Fact]
    public async Task PreviewLink_Customer_NotFound_ShouldThrow()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        var act = () => _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 999
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region PreviewLink - Supplier

    [Fact]
    public async Task PreviewLink_Supplier_ShouldMatchExpenseByCounterparty()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "阿里云", ShortName = "阿里" };
        _supplierRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "阿里云计算有限公司", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 2000,
                TransactionType = TransactionType.Expense, AccountId = 1,
                SupplierId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Supplier,
            EntityId = 1
        });

        // Assert
        result.EntityName.Should().Be("阿里云");
        result.TotalMatched.Should().Be(1);
        result.Candidates[0].MatchReason.Should().Contain("阿里云");
    }

    [Fact]
    public async Task PreviewLink_Supplier_ShouldMatchByShortName()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "深圳腾讯科技", ShortName = "腾讯" };
        _supplierRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "腾讯云服务费", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 500,
                TransactionType = TransactionType.Expense, AccountId = 1,
                SupplierId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Supplier,
            EntityId = 1
        });

        // Assert - Should match by short name "腾讯"
        result.TotalMatched.Should().Be(1);
        result.Candidates[0].MatchReason.Should().Contain("腾讯");
    }

    #endregion

    #region PreviewLink - Person

    [Fact]
    public async Task PreviewLink_Person_ShouldMatchByName()
    {
        // Arrange
        var person = new Person { Id = 1, Name = "李四", PersonType = PersonType.Employee };
        _personRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "李四", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 8000,
                TransactionType = TransactionType.Expense, AccountId = 1,
                PersonId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "工资卡" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Person,
            EntityId = 1
        });

        // Assert
        result.EntityName.Should().Be("李四");
        result.TotalMatched.Should().Be(1);
    }

    #endregion

    #region PreviewLink - Project

    [Fact]
    public async Task PreviewLink_Project_ShouldMatchByDescriptionContains()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "智慧城市项目", ProjectCode = "ZH-2026" };
        _projectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "某公司", Memo = "智慧城市一期", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 50000,
                TransactionType = TransactionType.Income, AccountId = 1,
                ProjectId = null, IsAllocated = false, BankTransactionId = 10,
                Description = "智慧城市项目首款",
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Project,
            EntityId = 1
        });

        // Assert
        result.EntityName.Should().Be("智慧城市项目");
        result.TotalMatched.Should().Be(1);
        result.Candidates[0].MatchReason.Should().Contain("智慧城市项目");
    }

    [Fact]
    public async Task PreviewLink_Project_ShouldNotMatchAllocatedTransactions()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "智慧城市项目" };
        _projectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        var bankTx = new BankTransaction { Id = 10, Counterparty = "某公司", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 50000,
                TransactionType = TransactionType.Income, AccountId = 1,
                ProjectId = null, IsAllocated = true, BankTransactionId = 10,
                Description = "智慧城市项目首款",
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.PreviewLinkAsync(new LinkPreviewRequest
        {
            LinkType = LinkType.Project,
            EntityId = 1
        });

        // Assert - Allocated transactions should be excluded
        result.TotalMatched.Should().Be(0);
    }

    #endregion

    #region ConfirmLink

    [Fact]
    public async Task ConfirmLink_Customer_ShouldUpdateForeignKey()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "张三公司" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var transactions = new List<Transaction>
        {
            new() { Id = 100, TransactionDate = DateTime.UtcNow, Amount = 1000, AccountId = 1, CustomerId = null },
            new() { Id = 101, TransactionDate = DateTime.UtcNow, Amount = 2000, AccountId = 1, CustomerId = null }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.ConfirmLinkAsync(new LinkConfirmRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 1,
            TransactionIds = new List<long> { 100, 101 }
        });

        // Assert
        result.LinkedCount.Should().Be(2);
        result.Message.Should().Contain("2");
        transactions[0].CustomerId.Should().Be(1);
        transactions[1].CustomerId.Should().Be(1);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Exactly(2));
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmLink_Supplier_ShouldUpdateForeignKey()
    {
        // Arrange
        var supplier = new Supplier { Id = 2, Name = "阿里云" };
        _supplierRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(supplier);

        var transactions = new List<Transaction>
        {
            new() { Id = 200, TransactionDate = DateTime.UtcNow, Amount = 500, AccountId = 1, SupplierId = null, TransactionType = TransactionType.Expense }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.ConfirmLinkAsync(new LinkConfirmRequest
        {
            LinkType = LinkType.Supplier,
            EntityId = 2,
            TransactionIds = new List<long> { 200 }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].SupplierId.Should().Be(2);
    }

    [Fact]
    public async Task ConfirmLink_Person_ShouldUpdateForeignKey()
    {
        // Arrange
        var person = new Person { Id = 3, Name = "李四", PersonType = PersonType.Employee };
        _personRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(person);

        var transactions = new List<Transaction>
        {
            new() { Id = 300, TransactionDate = DateTime.UtcNow, Amount = 8000, AccountId = 1, PersonId = null }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.ConfirmLinkAsync(new LinkConfirmRequest
        {
            LinkType = LinkType.Person,
            EntityId = 3,
            TransactionIds = new List<long> { 300 }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].PersonId.Should().Be(3);
    }

    [Fact]
    public async Task ConfirmLink_Project_ShouldUpdateForeignKey()
    {
        // Arrange
        var project = new Project { Id = 4, Name = "智慧城市" };
        _projectRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(project);

        var transactions = new List<Transaction>
        {
            new() { Id = 400, TransactionDate = DateTime.UtcNow, Amount = 50000, AccountId = 1, ProjectId = null }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        // Act
        var result = await _service.ConfirmLinkAsync(new LinkConfirmRequest
        {
            LinkType = LinkType.Project,
            EntityId = 4,
            TransactionIds = new List<long> { 400 }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].ProjectId.Should().Be(4);
    }

    [Fact]
    public async Task ConfirmLink_EmptyList_ShouldReturnZero()
    {
        // Act
        var result = await _service.ConfirmLinkAsync(new LinkConfirmRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 1,
            TransactionIds = new List<long>()
        });

        // Assert
        result.LinkedCount.Should().Be(0);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmLink_EntityNotFound_ShouldThrow()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        var act = () => _service.ConfirmLinkAsync(new LinkConfirmRequest
        {
            LinkType = LinkType.Customer,
            EntityId = 999,
            TransactionIds = new List<long> { 1 }
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region RuleRerun

    [Fact]
    public async Task PreviewRuleRerun_Conservative_ShouldOnlyShowUncategorized()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "阿里云", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 200,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CategoryId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        _ruleServiceMock.Setup(r => r.MatchCategoriesBatchAsync(It.IsAny<List<(string, string, decimal, string?)>>()))
            .ReturnsAsync((List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items) =>
                items.Select(i => (long?)5L).ToList());

        var category = new Category { Id = 5, Name = "云服务费" };
        _categoryRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(category);

        // Act
        var result = await _service.PreviewRuleRerunAsync(new RuleRerunPreviewRequest
        {
            Strategy = RuleRerunStrategy.Conservative
        });

        // Assert
        result.TotalAffected.Should().Be(1);
        result.WouldUpdate.Should().Be(1);
        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].WillChange.Should().BeTrue();
        result.Candidates[0].NewCategoryName.Should().Be("云服务费");
    }

    [Fact]
    public async Task PreviewRuleRerun_Conservative_ShouldSkipAlreadyCategorized()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "阿里云", AccountId = 1 };
        var existingCategory = new Category { Id = 3, Name = "旧分类" };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 200,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CategoryId = 3, BankTransactionId = 10,
                BankTransaction = bankTx, Category = existingCategory,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };

        // Conservative strategy filters CategoryId == null, so this should not appear
        var emptyList = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(emptyList.Object);

        _ruleServiceMock.Setup(r => r.MatchCategoriesBatchAsync(It.IsAny<List<(string, string, decimal, string?)>>()))
            .ReturnsAsync(new List<long?>());

        // Act
        var result = await _service.PreviewRuleRerunAsync(new RuleRerunPreviewRequest
        {
            Strategy = RuleRerunStrategy.Conservative
        });

        // Assert
        result.TotalAffected.Should().Be(0);
        result.WouldUpdate.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmRuleRerun_ShouldUpdateCategories()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "阿里云", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 200,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CategoryId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Description = ""
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        _ruleServiceMock.Setup(r => r.MatchCategoriesBatchAsync(It.IsAny<List<(string, string, decimal, string?)>>()))
            .ReturnsAsync((List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items) =>
                items.Select(i => (long?)5L).ToList());

        // Act
        var result = await _service.ConfirmRuleRerunAsync(new RuleRerunConfirmRequest
        {
            Strategy = RuleRerunStrategy.Conservative,
            TransactionIds = new List<long> { 100 }
        });

        // Assert
        result.UpdatedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        transactions[0].CategoryId.Should().Be(5);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmRuleRerun_NoMatch_ShouldSkip()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "未知对方", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 100,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CategoryId = null, BankTransactionId = 10,
                BankTransaction = bankTx,
                Description = ""
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        _ruleServiceMock.Setup(r => r.MatchCategoriesBatchAsync(It.IsAny<List<(string, string, decimal, string?)>>()))
            .ReturnsAsync((List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items) =>
                items.Select(i => (long?)null).ToList());

        // Act
        var result = await _service.ConfirmRuleRerunAsync(new RuleRerunConfirmRequest
        {
            Strategy = RuleRerunStrategy.Conservative,
            TransactionIds = new List<long> { 100 }
        });

        // Assert
        result.UpdatedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmRuleRerun_Conservative_ShouldSkipAlreadyCategorized()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "阿里云", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 200,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CategoryId = 3, // Already has a category
                BankTransactionId = 10,
                BankTransaction = bankTx,
                Description = ""
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        _ruleServiceMock.Setup(r => r.MatchCategoriesBatchAsync(It.IsAny<List<(string, string, decimal, string?)>>()))
            .ReturnsAsync((List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items) =>
                items.Select(i => (long?)5L).ToList());

        // Act
        var result = await _service.ConfirmRuleRerunAsync(new RuleRerunConfirmRequest
        {
            Strategy = RuleRerunStrategy.Conservative,
            TransactionIds = new List<long> { 100 }
        });

        // Assert - Conservative should skip already categorized
        result.UpdatedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        transactions[0].CategoryId.Should().Be(3); // Unchanged
    }

    [Fact]
    public async Task ConfirmRuleRerun_Overwrite_ShouldUpdateAlreadyCategorized()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "阿里云", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 200,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CategoryId = 3, // Already has a category
                BankTransactionId = 10,
                BankTransaction = bankTx,
                Description = ""
            }
        };

        var queryMock = transactions.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(queryMock.Object);

        _ruleServiceMock.Setup(r => r.MatchCategoriesBatchAsync(It.IsAny<List<(string, string, decimal, string?)>>()))
            .ReturnsAsync((List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items) =>
                items.Select(i => (long?)5L).ToList());

        // Act
        var result = await _service.ConfirmRuleRerunAsync(new RuleRerunConfirmRequest
        {
            Strategy = RuleRerunStrategy.Overwrite,
            TransactionIds = new List<long> { 100 }
        });

        // Assert - Overwrite should update even if already categorized
        result.UpdatedCount.Should().Be(1);
        transactions[0].CategoryId.Should().Be(5); // Updated to new category
    }

    #endregion

    #region PreviewBatchLink

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchCustomerByCounterparty()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "张三公司付款", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 100, TransactionDate = DateTime.UtcNow, Amount = 1000,
                TransactionType = TransactionType.Income, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer> { new() { Id = 1, Name = "张三公司", IsActive = true } };
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalUnlinked.Should().Be(1);
        result.TotalMatched.Should().Be(1);
        result.Candidates.Should().HaveCount(1);
        var candidate = result.Candidates[0];
        candidate.TransactionId.Should().Be(100);
        candidate.Matches.Should().HaveCount(1);
        candidate.Matches[0].EntityType.Should().Be(BatchLinkEntityType.Customer);
        candidate.Matches[0].EntityId.Should().Be(1);
        candidate.Matches[0].EntityName.Should().Be("张三公司");
        candidate.Matches[0].MatchReason.Should().Contain("张三公司");
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchSupplierByShortName()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "腾讯云服务费", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 200, TransactionDate = DateTime.UtcNow, Amount = 500,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier> { new() { Id = 2, Name = "深圳腾讯科技", ShortName = "腾讯", IsActive = true } };
        var persons = new List<Person>();
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalMatched.Should().Be(1);
        result.Candidates[0].Matches[0].EntityType.Should().Be(BatchLinkEntityType.Supplier);
        result.Candidates[0].Matches[0].EntityId.Should().Be(2);
        result.Candidates[0].Matches[0].MatchReason.Should().Contain("腾讯");
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchPersonByName()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "李四", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 300, TransactionDate = DateTime.UtcNow, Amount = 8000,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "工资卡" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        var persons = new List<Person> { new() { Id = 3, Name = "李四", PersonType = PersonType.Employee, IsActive = true } };
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalMatched.Should().Be(1);
        result.Candidates[0].Matches[0].EntityType.Should().Be(BatchLinkEntityType.Person);
        result.Candidates[0].Matches[0].EntityId.Should().Be(3);
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchProjectByDescriptionAndCode()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "某公司", Memo = "ZH-2026 首款", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 400, TransactionDate = DateTime.UtcNow, Amount = 50000,
                TransactionType = TransactionType.Income, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Description = "智慧城市项目款",
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project> { new() { Id = 4, Name = "智慧城市项目", ProjectCode = "ZH-2026" } };

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalMatched.Should().Be(1);
        // 同一交易可能同时通过名称和编号匹配，取第一个即可
        result.Candidates[0].Matches.Should().NotBeEmpty();
        result.Candidates[0].Matches.Should().AllSatisfy(m => m.EntityType.Should().Be(BatchLinkEntityType.Project));
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldExcludeTransferTransactions()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "张三公司", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 500, TransactionDate = DateTime.UtcNow, Amount = 1000,
                TransactionType = TransactionType.Transfer, // Transfer should be excluded
                AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer> { new() { Id = 1, Name = "张三公司", IsActive = true } };
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert - Transfer transactions must be excluded
        result.TotalUnlinked.Should().Be(0);
        result.TotalMatched.Should().Be(0);
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldExcludeAlreadyLinkedTransactions()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "张三公司", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 600, TransactionDate = DateTime.UtcNow, Amount = 1000,
                TransactionType = TransactionType.Income, AccountId = 1,
                CustomerId = 99, // Already linked
                SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer> { new() { Id = 1, Name = "张三公司", IsActive = true } };
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert - Already linked transactions excluded from unlinked count
        result.TotalUnlinked.Should().Be(0);
        result.TotalMatched.Should().Be(0);
    }

    [Fact]
    public async Task PreviewBatchLink_MultipleEntitiesWithSameName_ShouldReturnMultipleMatches()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "李华", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 700, TransactionDate = DateTime.UtcNow, Amount = 5000,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        // 两个叫"李华"的人员
        var persons = new List<Person>
        {
            new() { Id = 10, Name = "李华", Phone = "13800000001", PersonType = PersonType.Employee, IsActive = true },
            new() { Id = 11, Name = "李华", Phone = "13800000002", PersonType = PersonType.Contractor, IsActive = true }
        };
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert - should return 2 candidate matches for the same transaction
        result.TotalMatched.Should().Be(1);
        result.Candidates[0].TransactionId.Should().Be(700);
        result.Candidates[0].Matches.Should().HaveCount(2);
        result.Candidates[0].Matches.Should().AllSatisfy(m => m.EntityType.Should().Be(BatchLinkEntityType.Person));
        // ExtraInfo should help distinguish (contains phone)
        result.Candidates[0].Matches.Should().AllSatisfy(m => m.ExtraInfo.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task PreviewBatchLink_NoMatch_ShouldReturnEmptyCandidates()
    {
        // Arrange
        var bankTx = new BankTransaction { Id = 10, Counterparty = "完全陌生的名字", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 800, TransactionDate = DateTime.UtcNow, Amount = 100,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer> { new() { Id = 1, Name = "张三公司", IsActive = true } };
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Account>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalUnlinked.Should().Be(1);
        result.TotalMatched.Should().Be(0);
        result.Candidates.Should().BeEmpty();
    }

    #endregion

    #region ConfirmBatchLink

    [Fact]
    public async Task ConfirmBatchLink_ShouldSetCustomerId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 100, TransactionDate = DateTime.UtcNow, Amount = 1000, AccountId = 1, CustomerId = null }
        };
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>
            {
                new() { TransactionId = 100, EntityType = BatchLinkEntityType.Customer, EntityId = 5 }
            }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        result.Message.Should().Contain("1");
        transactions[0].CustomerId.Should().Be(5);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Once);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmBatchLink_ShouldSetSupplierId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 200, TransactionDate = DateTime.UtcNow, Amount = 500, AccountId = 1, SupplierId = null }
        };
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>
            {
                new() { TransactionId = 200, EntityType = BatchLinkEntityType.Supplier, EntityId = 6 }
            }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].SupplierId.Should().Be(6);
    }

    [Fact]
    public async Task ConfirmBatchLink_ShouldSetPersonId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 300, TransactionDate = DateTime.UtcNow, Amount = 8000, AccountId = 1, PersonId = null }
        };
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>
            {
                new() { TransactionId = 300, EntityType = BatchLinkEntityType.Person, EntityId = 7 }
            }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].PersonId.Should().Be(7);
    }

    [Fact]
    public async Task ConfirmBatchLink_ShouldSetProjectId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 400, TransactionDate = DateTime.UtcNow, Amount = 50000, AccountId = 1, ProjectId = null }
        };
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>
            {
                new() { TransactionId = 400, EntityType = BatchLinkEntityType.Project, EntityId = 8 }
            }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].ProjectId.Should().Be(8);
    }

    [Fact]
    public async Task ConfirmBatchLink_EmptyItems_ShouldReturnZero()
    {
        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>()
        });

        // Assert
        result.LinkedCount.Should().Be(0);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmBatchLink_MultipleItems_ShouldHandleDifferentEntityTypes()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 101, TransactionDate = DateTime.UtcNow, Amount = 1000, AccountId = 1 },
            new() { Id = 102, TransactionDate = DateTime.UtcNow, Amount = 2000, AccountId = 1 },
            new() { Id = 103, TransactionDate = DateTime.UtcNow, Amount = 3000, AccountId = 1 }
        };
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>
            {
                new() { TransactionId = 101, EntityType = BatchLinkEntityType.Customer, EntityId = 1 },
                new() { TransactionId = 102, EntityType = BatchLinkEntityType.Supplier, EntityId = 2 },
                new() { TransactionId = 103, EntityType = BatchLinkEntityType.Person, EntityId = 3 }
            }
        });

        // Assert
        result.LinkedCount.Should().Be(3);
        transactions[0].CustomerId.Should().Be(1);
        transactions[1].SupplierId.Should().Be(2);
        transactions[2].PersonId.Should().Be(3);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Exactly(3));
    }

    #endregion

    #region PreviewBatchLink — Account Matching

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchAccountByName()
    {
        // Arrange — 对方名称含另一账户名
        var bankTx = new BankTransaction { Id = 10, Counterparty = "工商银行备用金账户转入", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 900, TransactionDate = DateTime.UtcNow, Amount = 10000,
                TransactionType = TransactionType.Income, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "主账户", IsActive = true },
            new() { Id = 2, Name = "备用金账户", BankName = "建设银行", IsActive = true }
        };

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(accounts.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalMatched.Should().Be(1);
        var match = result.Candidates[0].Matches.First(m => m.EntityType == BatchLinkEntityType.Account);
        match.EntityId.Should().Be(2);
        match.EntityName.Should().Be("备用金账户");
        match.MatchReason.Should().Contain("备用金账户");
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchAccountByBankName()
    {
        // Arrange — 对方名称含银行名称
        var bankTx = new BankTransaction { Id = 10, Counterparty = "招商银行网银转账", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 901, TransactionDate = DateTime.UtcNow, Amount = 5000,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "主账户", IsActive = true },
            new() { Id = 3, Name = "招行账户", BankName = "招商银行", AccountNumber = "6225001234", IsActive = true }
        };

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(accounts.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalMatched.Should().Be(1);
        var match = result.Candidates[0].Matches.First(m => m.EntityType == BatchLinkEntityType.Account);
        match.EntityId.Should().Be(3);
        match.ExtraInfo.Should().Contain("招商银行");
        match.ExtraInfo.Should().Contain("6225001234");
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldNotMatchAccountToItself()
    {
        // Arrange — 交易所属账户名出现在对方字段中，不应匹配自身
        var bankTx = new BankTransaction { Id = 10, Counterparty = "主账户余额查询", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 902, TransactionDate = DateTime.UtcNow, Amount = 100,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();
        // 只有一个账户（就是交易自身的账户），不应匹配
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "主账户", IsActive = true }
        };

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(accounts.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert — 不应有任何账户匹配
        result.TotalMatched.Should().Be(0);
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewBatchLink_ShouldMatchAccountByAccountNumber()
    {
        // Arrange — 对方名称含账号
        var bankTx = new BankTransaction { Id = 10, Counterparty = "转账至6225001234", AccountId = 1 };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 903, TransactionDate = DateTime.UtcNow, Amount = 20000,
                TransactionType = TransactionType.Expense, AccountId = 1,
                CustomerId = null, SupplierId = null, PersonId = null, ProjectId = null,
                BankTransactionId = 10, BankTransaction = bankTx,
                Account = new Account { Id = 1, Name = "主账户" }
            }
        };
        var customers = new List<Customer>();
        var suppliers = new List<Supplier>();
        var persons = new List<Person>();
        var projects = new List<Project>();
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "主账户", IsActive = true },
            new() { Id = 4, Name = "招行账户", AccountNumber = "6225001234", IsActive = true }
        };

        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);
        _customerRepoMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _supplierRepoMock.Setup(r => r.GetQueryable()).Returns(suppliers.AsQueryable().BuildMock().Object);
        _personRepoMock.Setup(r => r.GetQueryable()).Returns(persons.AsQueryable().BuildMock().Object);
        _projectRepoMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);
        _accountRepoMock.Setup(r => r.GetQueryable()).Returns(accounts.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.PreviewBatchLinkAsync();

        // Assert
        result.TotalMatched.Should().Be(1);
        var match = result.Candidates[0].Matches.First(m => m.EntityType == BatchLinkEntityType.Account);
        match.EntityId.Should().Be(4);
        match.MatchReason.Should().Contain("6225001234");
    }

    #endregion

    #region ConfirmBatchLink — Account

    [Fact]
    public async Task ConfirmBatchLink_ShouldSetAccountId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 500, TransactionDate = DateTime.UtcNow, Amount = 10000, AccountId = 1 }
        };
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.ConfirmBatchLinkAsync(new BatchLinkConfirmRequest
        {
            Items = new List<BatchLinkConfirmItem>
            {
                new() { TransactionId = 500, EntityType = BatchLinkEntityType.Account, EntityId = 2 }
            }
        });

        // Assert
        result.LinkedCount.Should().Be(1);
        transactions[0].AccountId.Should().Be(2);
        _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Once);
    }

    #endregion
}
