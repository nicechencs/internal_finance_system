using FluentAssertions;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Moq;
using Transaction = FinanceApp.Domain.Entities.Transaction;

namespace FinanceApp.Application.Tests.Services;

public class MasterDataReferenceGuardTests
{
    private readonly Mock<IRepository<Transaction>> _transactionRepoMock;
    private readonly Mock<IRepository<BankTransaction>> _bankTransactionRepoMock;
    private readonly Mock<IRepository<ImportBatch>> _importBatchRepoMock;
    private readonly Mock<IRepository<ClassificationRule>> _ruleRepoMock;
    private readonly Mock<IRepository<TransactionAllocation>> _allocationRepoMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepoMock;
    private readonly Mock<IRepository<Payable>> _payableRepoMock;
    private readonly Mock<IRepository<Project>> _projectRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly MasterDataReferenceGuard _guard;

    public MasterDataReferenceGuardTests()
    {
        _transactionRepoMock = MockHelpers.CreateEmptyRepoMock<Transaction>();
        _bankTransactionRepoMock = MockHelpers.CreateEmptyRepoMock<BankTransaction>();
        _importBatchRepoMock = MockHelpers.CreateEmptyRepoMock<ImportBatch>();
        _ruleRepoMock = MockHelpers.CreateEmptyRepoMock<ClassificationRule>();
        _allocationRepoMock = MockHelpers.CreateEmptyRepoMock<TransactionAllocation>();
        _receivableRepoMock = MockHelpers.CreateEmptyRepoMock<Receivable>();
        _payableRepoMock = MockHelpers.CreateEmptyRepoMock<Payable>();
        _projectRepoMock = MockHelpers.CreateEmptyRepoMock<Project>();
        _categoryRepoMock = MockHelpers.CreateEmptyRepoMock<Category>();

        _guard = new MasterDataReferenceGuard(
            _transactionRepoMock.Object,
            _bankTransactionRepoMock.Object,
            _importBatchRepoMock.Object,
            _ruleRepoMock.Object,
            _allocationRepoMock.Object,
            _receivableRepoMock.Object,
            _payableRepoMock.Object,
            _projectRepoMock.Object,
            _categoryRepoMock.Object);
    }

    #region HasAccountReferencesAsync Tests

    [Fact]
    public async Task HasAccountReferencesAsync_WithActiveTransaction_ReturnsTrue()
    {
        // Arrange
        var accountId = 1L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, AccountId = accountId, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_WithDeletedTransaction_ReturnsFalse()
    {
        // Arrange
        var accountId = 1L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, AccountId = accountId, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_NoReferences_ReturnsFalse()
    {
        // Arrange — 默认所有仓库返回空集合，无需额外设置

        // Act
        var result = await _guard.HasAccountReferencesAsync(999L);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_WithActiveBankTransaction_ReturnsTrue()
    {
        // Arrange
        var accountId = 1L;
        MockHelpers.SetupRepo(_bankTransactionRepoMock,
            new BankTransaction { Id = 20, AccountId = accountId, IsDeleted = false, Amount = 200, TransactionDate = DateTime.UtcNow, UniqueHash = "hash1" });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_WithDeletedBankTransaction_ReturnsFalse()
    {
        // Arrange
        var accountId = 1L;
        MockHelpers.SetupRepo(_bankTransactionRepoMock,
            new BankTransaction { Id = 20, AccountId = accountId, IsDeleted = true, Amount = 200, TransactionDate = DateTime.UtcNow, UniqueHash = "hash1" });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_WithActiveImportBatch_ReturnsTrue()
    {
        // Arrange
        var accountId = 1L;
        MockHelpers.SetupRepo(_importBatchRepoMock,
            new ImportBatch { Id = 30, AccountId = accountId, IsDeleted = false, FileName = "test.xlsx" });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_AllReferencesDeleted_ReturnsFalse()
    {
        // Arrange — 所有关联实体都已软删除
        var accountId = 1L;

        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, AccountId = accountId, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_bankTransactionRepoMock,
            new BankTransaction { Id = 20, AccountId = accountId, IsDeleted = true, Amount = 200, TransactionDate = DateTime.UtcNow, UniqueHash = "hash1" });
        MockHelpers.SetupRepo(_importBatchRepoMock,
            new ImportBatch { Id = 30, AccountId = accountId, IsDeleted = true, FileName = "test.xlsx" });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccountReferencesAsync_DifferentAccountId_ReturnsFalse()
    {
        // Arrange — 存在活跃交易，但关联的是另一个账户
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, AccountId = 2L, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasAccountReferencesAsync(1L);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasCategoryReferencesAsync Tests

    [Fact]
    public async Task HasCategoryReferencesAsync_WithActiveTransaction_ReturnsTrue()
    {
        // Arrange
        var categoryId = 5L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CategoryId = categoryId, AccountId = 1, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_WithDeletedTransaction_ReturnsFalse()
    {
        // Arrange
        var categoryId = 5L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CategoryId = categoryId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(categoryId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_WithActiveRule_ReturnsTrue()
    {
        // Arrange
        var categoryId = 5L;
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, CategoryId = categoryId, IsDeleted = false, RuleName = "TestRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_WithChildCategory_ReturnsTrue()
    {
        // Arrange — 有活跃的子分类引用
        var parentCategoryId = 5L;
        MockHelpers.SetupRepo(_categoryRepoMock,
            new Category { Id = 6, ParentId = parentCategoryId, Name = "子分类", IsDeleted = false });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(parentCategoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_WithDeletedChildCategory_ReturnsFalse()
    {
        // Arrange — 子分类已软删除
        var parentCategoryId = 5L;
        MockHelpers.SetupRepo(_categoryRepoMock,
            new Category { Id = 6, ParentId = parentCategoryId, Name = "已删除子分类", IsDeleted = true });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(parentCategoryId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_NoReferences_ReturnsFalse()
    {
        // Arrange — 默认空集合

        // Act
        var result = await _guard.HasCategoryReferencesAsync(999L);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_AllReferencesDeleted_ReturnsFalse()
    {
        // Arrange — 交易、规则、子分类全部软删除
        var categoryId = 5L;

        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CategoryId = categoryId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, CategoryId = categoryId, IsDeleted = true, RuleName = "TestRule", MatchValue = "test" });
        MockHelpers.SetupRepo(_categoryRepoMock,
            new Category { Id = 6, ParentId = categoryId, Name = "已删除子分类", IsDeleted = true });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(categoryId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasCustomerReferencesAsync Tests

    [Fact]
    public async Task HasCustomerReferencesAsync_WithActiveProject_ReturnsTrue()
    {
        // Arrange
        var customerId = 3L;
        MockHelpers.SetupRepo(_projectRepoMock,
            new Project { Id = 50, CustomerId = customerId, Name = "测试项目", IsDeleted = false });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_WithActiveTransaction_ReturnsTrue()
    {
        // Arrange
        var customerId = 3L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CustomerId = customerId, AccountId = 1, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_WithActiveReceivable_ReturnsTrue()
    {
        // Arrange
        var customerId = 3L;
        MockHelpers.SetupRepo(_receivableRepoMock,
            new Receivable { Id = 60, CustomerId = customerId, ProjectId = 1, IsDeleted = false, TotalAmount = 1000 });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_WithActivePayable_ReturnsTrue()
    {
        // Arrange
        var customerId = 3L;
        MockHelpers.SetupRepo(_payableRepoMock,
            new Payable { Id = 70, CustomerId = customerId, IsDeleted = false, TotalAmount = 500 });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_WithActiveRule_ReturnsTrue()
    {
        // Arrange
        var customerId = 3L;
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, CustomerId = customerId, IsDeleted = false, RuleName = "TestRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_AllReferencesDeleted_ReturnsFalse()
    {
        // Arrange — 所有引用都已软删除
        var customerId = 3L;

        MockHelpers.SetupRepo(_projectRepoMock,
            new Project { Id = 50, CustomerId = customerId, Name = "已删除项目", IsDeleted = true });
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CustomerId = customerId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_receivableRepoMock,
            new Receivable { Id = 60, CustomerId = customerId, ProjectId = 1, IsDeleted = true, TotalAmount = 1000 });
        MockHelpers.SetupRepo(_payableRepoMock,
            new Payable { Id = 70, CustomerId = customerId, IsDeleted = true, TotalAmount = 500 });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, CustomerId = customerId, IsDeleted = true, RuleName = "TestRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_NoReferences_ReturnsFalse()
    {
        // Arrange — 默认空集合

        // Act
        var result = await _guard.HasCustomerReferencesAsync(999L);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasSupplierReferencesAsync Tests

    [Fact]
    public async Task HasSupplierReferencesAsync_WithActiveTransaction_ReturnsTrue()
    {
        // Arrange
        var supplierId = 4L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, SupplierId = supplierId, AccountId = 1, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasSupplierReferencesAsync(supplierId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSupplierReferencesAsync_WithActiveReceivable_ReturnsTrue()
    {
        // Arrange
        var supplierId = 4L;
        MockHelpers.SetupRepo(_receivableRepoMock,
            new Receivable { Id = 60, SupplierId = supplierId, ProjectId = 1, IsDeleted = false, TotalAmount = 1000 });

        // Act
        var result = await _guard.HasSupplierReferencesAsync(supplierId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSupplierReferencesAsync_AllReferencesDeleted_ReturnsFalse()
    {
        // Arrange
        var supplierId = 4L;

        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, SupplierId = supplierId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_receivableRepoMock,
            new Receivable { Id = 60, SupplierId = supplierId, ProjectId = 1, IsDeleted = true, TotalAmount = 1000 });
        MockHelpers.SetupRepo(_payableRepoMock,
            new Payable { Id = 70, SupplierId = supplierId, IsDeleted = true, TotalAmount = 500 });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, SupplierId = supplierId, IsDeleted = true, RuleName = "TestRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasSupplierReferencesAsync(supplierId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasPersonReferencesAsync Tests

    [Fact]
    public async Task HasPersonReferencesAsync_WithActiveTransaction_ReturnsTrue()
    {
        // Arrange
        var personId = 7L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, PersonId = personId, AccountId = 1, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasPersonReferencesAsync(personId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPersonReferencesAsync_WithActiveAllocation_ReturnsTrue()
    {
        // Arrange
        var personId = 7L;
        MockHelpers.SetupRepo(_allocationRepoMock,
            new TransactionAllocation { Id = 80, PersonId = personId, TransactionId = 1, IsDeleted = false, Amount = 50 });

        // Act
        var result = await _guard.HasPersonReferencesAsync(personId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPersonReferencesAsync_AllReferencesDeleted_ReturnsFalse()
    {
        // Arrange
        var personId = 7L;

        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, PersonId = personId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_allocationRepoMock,
            new TransactionAllocation { Id = 80, PersonId = personId, TransactionId = 1, IsDeleted = true, Amount = 50 });
        MockHelpers.SetupRepo(_receivableRepoMock,
            new Receivable { Id = 60, PersonId = personId, ProjectId = 1, IsDeleted = true, TotalAmount = 1000 });
        MockHelpers.SetupRepo(_payableRepoMock,
            new Payable { Id = 70, PersonId = personId, IsDeleted = true, TotalAmount = 500 });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, PersonId = personId, IsDeleted = true, RuleName = "TestRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasPersonReferencesAsync(personId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasProjectReferencesAsync Tests

    [Fact]
    public async Task HasProjectReferencesAsync_WithActiveTransaction_ReturnsTrue()
    {
        // Arrange
        var projectId = 8L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, ProjectId = projectId, AccountId = 1, IsDeleted = false, Amount = 100, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasProjectReferencesAsync(projectId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasProjectReferencesAsync_WithActiveAllocation_ReturnsTrue()
    {
        // Arrange
        var projectId = 8L;
        MockHelpers.SetupRepo(_allocationRepoMock,
            new TransactionAllocation { Id = 80, ProjectId = projectId, TransactionId = 1, IsDeleted = false, Amount = 50 });

        // Act
        var result = await _guard.HasProjectReferencesAsync(projectId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasProjectReferencesAsync_AllReferencesDeleted_ReturnsFalse()
    {
        // Arrange
        var projectId = 8L;

        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, ProjectId = projectId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_allocationRepoMock,
            new TransactionAllocation { Id = 80, ProjectId = projectId, TransactionId = 1, IsDeleted = true, Amount = 50 });
        MockHelpers.SetupRepo(_receivableRepoMock,
            new Receivable { Id = 60, ProjectId = projectId, IsDeleted = true, TotalAmount = 1000 });
        MockHelpers.SetupRepo(_payableRepoMock,
            new Payable { Id = 70, ProjectId = projectId, IsDeleted = true, TotalAmount = 500 });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, ProjectId = projectId, IsDeleted = true, RuleName = "TestRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasProjectReferencesAsync(projectId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Soft Delete Filter Verification (Cross-cutting)

    [Fact]
    public async Task HasAccountReferencesAsync_MixedDeletedAndActive_ReturnsTrue()
    {
        // Arrange — 同一账户有已删除和未删除的交易，只要有一个未删除就返回 true
        var accountId = 1L;
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, AccountId = accountId, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow },
            new Transaction { Id = 11, AccountId = accountId, IsDeleted = false, Amount = 200, TransactionDate = DateTime.UtcNow });

        // Act
        var result = await _guard.HasAccountReferencesAsync(accountId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCategoryReferencesAsync_OnlyChildCategoryActive_ReturnsTrue()
    {
        // Arrange — 交易和规则都已删除，但子分类未删除，应返回 true
        var categoryId = 5L;

        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CategoryId = categoryId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, CategoryId = categoryId, IsDeleted = true, RuleName = "TestRule", MatchValue = "test" });
        MockHelpers.SetupRepo(_categoryRepoMock,
            new Category { Id = 6, ParentId = categoryId, Name = "活跃子分类", IsDeleted = false });

        // Act
        var result = await _guard.HasCategoryReferencesAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCustomerReferencesAsync_OnlyRuleActive_ReturnsTrue()
    {
        // Arrange — 项目和交易都已删除，但规则未删除
        var customerId = 3L;

        MockHelpers.SetupRepo(_projectRepoMock,
            new Project { Id = 50, CustomerId = customerId, Name = "已删除项目", IsDeleted = true });
        MockHelpers.SetupRepo(_transactionRepoMock,
            new Transaction { Id = 10, CustomerId = customerId, AccountId = 1, IsDeleted = true, Amount = 100, TransactionDate = DateTime.UtcNow });
        MockHelpers.SetupRepo(_ruleRepoMock,
            new ClassificationRule { Id = 40, CustomerId = customerId, IsDeleted = false, RuleName = "ActiveRule", MatchValue = "test" });

        // Act
        var result = await _guard.HasCustomerReferencesAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
