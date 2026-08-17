using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TransferServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<Account>> _accountRepositoryMock;
    private readonly Mock<ITransactionQueryService> _queryServiceMock;
    private readonly Mock<IFixedDepositService> _fixedDepositServiceMock;
    private readonly Mock<ILogger<TransferService>> _loggerMock;
    private readonly TransferService _service;
    private readonly List<Transaction> _createdTransactions = new();

    public TransferServiceTests()
    {
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _accountRepositoryMock = new Mock<IRepository<Account>>();
        _queryServiceMock = new Mock<ITransactionQueryService>();
        _fixedDepositServiceMock = new Mock<IFixedDepositService>();
        _loggerMock = new Mock<ILogger<TransferService>>();

        UnitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ITransactionScope?)null);

        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) =>
            {
                t.Id = _createdTransactions.Count + 1;
                _createdTransactions.Add(t);
                return t;
            });

        _transactionRepositoryMock.Setup(x => x.Update(It.IsAny<Transaction>()));
        _accountRepositoryMock.Setup(x => x.Update(It.IsAny<Account>()));

        _queryServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) =>
            {
                var t = _createdTransactions.FirstOrDefault(t => t.Id == id);
                return t != null
                    ? new TransactionDto { Id = t.Id, Amount = t.Amount, AccountId = t.AccountId }
                    : null!;
            });

        _service = new TransferService(
            _transactionRepositoryMock.Object,
            _accountRepositoryMock.Object,
            UnitOfWorkMock.Object,
            _queryServiceMock.Object,
            AuditLogServiceMock.Object,
            _fixedDepositServiceMock.Object,
            _loggerMock.Object);
    }

    private (Account from, Account to) SetupAccounts(
        AccountType fromType = AccountType.Bank,
        AccountType toType = AccountType.Bank,
        decimal fromBalance = 100000m,
        decimal toBalance = 50000m)
    {
        var fromAccount = new Account
        {
            Id = 1, Name = "活期账户", AccountType = fromType, CurrentBalance = fromBalance
        };
        var toAccount = new Account
        {
            Id = 2, Name = "定期账户", AccountType = toType, CurrentBalance = toBalance
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        return (fromAccount, toAccount);
    }

    // ========== 基础转账测试 ==========

    [Fact]
    public async Task CreateTransferAsync_BankToBank_ShouldNotTriggerFixedDepositLinkage()
    {
        // Arrange
        var (fromAccount, toAccount) = SetupAccounts();

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 5000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.FixedDepositLinkage.Should().BeNull();
        _fixedDepositServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateFixedDepositRequest>()), Times.Never);
        _fixedDepositServiceMock.Verify(x => x.GetByAccountAsync(It.IsAny<long>()), Times.Never);
    }

    // ========== 转入定期账户 → 自动创建存单 ==========

    [Fact]
    public async Task CreateTransferAsync_ToFixedDeposit_WithTermParams_ShouldAutoCreateRecord()
    {
        // Arrange
        var (fromAccount, toAccount) = SetupAccounts(toType: AccountType.FixedDeposit);

        _fixedDepositServiceMock.Setup(x => x.CreateAsync(It.IsAny<CreateFixedDepositRequest>()))
            .ReturnsAsync((CreateFixedDepositRequest req) => new FixedDepositDto
            {
                Id = 100,
                AccountId = req.AccountId,
                Principal = req.Principal,
                TermMonths = req.TermMonths,
                InterestRate = req.InterestRate,
                Status = "Active",
                DepositTransactionId = req.DepositTransactionId ?? 0
            });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 50000m,
            TransactionDate = new DateTime(2026, 4, 1),
            TermMonths = 12,
            InterestRate = 1.65m
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().NotBeNull();
        result.FixedDepositLinkage!.Action.Should().Be("Created");
        result.FixedDepositLinkage.FixedDepositId.Should().Be(100);
        result.FixedDepositLinkage.Message.Should().Contain("50,000.00");
        result.FixedDepositLinkage.Message.Should().Contain("12 个月");

        // 验证传给 FixedDepositService 的参数
        _fixedDepositServiceMock.Verify(x => x.CreateAsync(It.Is<CreateFixedDepositRequest>(r =>
            r.AccountId == 2L &&
            r.Principal == 50000m &&
            r.TermMonths == 12 &&
            r.InterestRate == 1.65m &&
            r.DepositTransactionId == 2 && // inTransaction.Id = 2 (第二笔创建的交易)
            r.Notes!.Contains("转账自动创建")
        )), Times.Once);

        // 转账余额仍然正确更新
        fromAccount.CurrentBalance.Should().Be(50000m); // 100000 - 50000
        toAccount.CurrentBalance.Should().Be(100000m);  // 50000 + 50000
    }

    [Fact]
    public async Task CreateTransferAsync_ToFixedDeposit_WithoutTermParams_ShouldSkipAutoCreate()
    {
        // Arrange
        var (_, _) = SetupAccounts(toType: AccountType.FixedDeposit);

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 30000m,
            TransactionDate = DateTime.Now
            // 未提供 TermMonths 和 InterestRate
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().BeNull();
        _fixedDepositServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateFixedDepositRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransferAsync_ToFixedDeposit_OnlyTermMonths_ShouldSkipAutoCreate()
    {
        // Arrange: 只提供期限不提供利率
        var (_, _) = SetupAccounts(toType: AccountType.FixedDeposit);

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 30000m,
            TransactionDate = DateTime.Now,
            TermMonths = 12
            // 未提供 InterestRate
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().BeNull();
        _fixedDepositServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateFixedDepositRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransferAsync_ToFixedDeposit_CreateFails_ShouldNotAffectTransfer()
    {
        // Arrange
        var (fromAccount, toAccount) = SetupAccounts(toType: AccountType.FixedDeposit);

        _fixedDepositServiceMock.Setup(x => x.CreateAsync(It.IsAny<CreateFixedDepositRequest>()))
            .ThrowsAsync(new Exception("模拟创建失败"));

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 50000m,
            TransactionDate = DateTime.Now,
            TermMonths = 6,
            InterestRate = 1.45m
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert — 转账成功，联动为空
        result.Should().NotBeNull();
        result.OutTransaction.Should().NotBeNull();
        result.InTransaction.Should().NotBeNull();
        result.FixedDepositLinkage.Should().BeNull();

        // 余额已更新（转账本身成功）
        fromAccount.CurrentBalance.Should().Be(50000m);
        toAccount.CurrentBalance.Should().Be(100000m);
    }

    // ========== 从定期账户转出 → 自动支取 ==========

    [Fact]
    public async Task CreateTransferAsync_FromFixedDeposit_WithMatchingRecord_ShouldAutoWithdraw()
    {
        // Arrange
        var (fromAccount, toAccount) = SetupAccounts(
            fromType: AccountType.FixedDeposit,
            fromBalance: 50000m);

        var activeRecord = new FixedDepositDto
        {
            Id = 10,
            AccountId = 1L,
            Principal = 50000m,
            Status = "Active",
            MaturityDate = DateTime.UtcNow.AddDays(-1),
            TermMonths = 12,
            InterestRate = 1.65m
        };

        _fixedDepositServiceMock.Setup(x => x.GetByAccountAsync(1L))
            .ReturnsAsync(new List<FixedDepositDto> { activeRecord });

        _fixedDepositServiceMock.Setup(x => x.WithdrawAsync(10L, It.IsAny<WithdrawFixedDepositRequest>()))
            .ReturnsAsync(new FixedDepositDto
            {
                Id = 10,
                Status = "Withdrawn",
                ActualInterest = 825m,
                Principal = 50000m
            });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 50000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().NotBeNull();
        result.FixedDepositLinkage!.Action.Should().Be("Withdrawn");
        result.FixedDepositLinkage.FixedDepositId.Should().Be(10);
        result.FixedDepositLinkage.Message.Should().Contain("50,000.00");
        result.FixedDepositLinkage.Message.Should().Contain("825.00");

        _fixedDepositServiceMock.Verify(x => x.WithdrawAsync(10L, It.Is<WithdrawFixedDepositRequest>(r =>
            r.TransactionId == 1 // outTransaction.Id = 1
        )), Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_FromFixedDeposit_AmountMismatch_ShouldSkipWithdraw()
    {
        // Arrange: 转出金额与任何记录都不匹配（差额超过容差）
        var (_, _) = SetupAccounts(fromType: AccountType.FixedDeposit, fromBalance: 200000m);

        var record = new FixedDepositDto
        {
            Id = 10,
            AccountId = 1L,
            Principal = 100000m, // 差额 70000，远超 min(1%, 100) = 100
            Status = "Active"
        };

        _fixedDepositServiceMock.Setup(x => x.GetByAccountAsync(1L))
            .ReturnsAsync(new List<FixedDepositDto> { record });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 30000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().BeNull();
        _fixedDepositServiceMock.Verify(x => x.WithdrawAsync(It.IsAny<long>(), It.IsAny<WithdrawFixedDepositRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransferAsync_FromFixedDeposit_NoActiveRecords_ShouldSkipWithdraw()
    {
        // Arrange
        var (_, _) = SetupAccounts(fromType: AccountType.FixedDeposit);

        var withdrawnRecord = new FixedDepositDto
        {
            Id = 10,
            AccountId = 1L,
            Principal = 100000m,
            Status = "Withdrawn"
        };

        _fixedDepositServiceMock.Setup(x => x.GetByAccountAsync(1L))
            .ReturnsAsync(new List<FixedDepositDto> { withdrawnRecord });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 100000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().BeNull();
        _fixedDepositServiceMock.Verify(x => x.WithdrawAsync(It.IsAny<long>(), It.IsAny<WithdrawFixedDepositRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransferAsync_FromFixedDeposit_WithdrawFails_ShouldNotAffectTransfer()
    {
        // Arrange
        var (fromAccount, toAccount) = SetupAccounts(fromType: AccountType.FixedDeposit);

        var record = new FixedDepositDto
        {
            Id = 10,
            AccountId = 1L,
            Principal = 100000m,
            Status = "Active"
        };

        _fixedDepositServiceMock.Setup(x => x.GetByAccountAsync(1L))
            .ReturnsAsync(new List<FixedDepositDto> { record });

        _fixedDepositServiceMock.Setup(x => x.WithdrawAsync(It.IsAny<long>(), It.IsAny<WithdrawFixedDepositRequest>()))
            .ThrowsAsync(new Exception("模拟支取失败"));

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 100000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert — 转账成功，联动为空
        result.Should().NotBeNull();
        result.FixedDepositLinkage.Should().BeNull();
        fromAccount.CurrentBalance.Should().Be(0m);
        toAccount.CurrentBalance.Should().Be(150000m);
    }

    [Fact]
    public async Task CreateTransferAsync_FromFixedDeposit_PicksClosestMatch()
    {
        // Arrange: 多条记录，应选金额最接近的
        var (_, _) = SetupAccounts(fromType: AccountType.FixedDeposit);

        var records = new List<FixedDepositDto>
        {
            new() { Id = 10, AccountId = 1L, Principal = 30000m, Status = "Active" },
            new() { Id = 11, AccountId = 1L, Principal = 50000m, Status = "Active" },  // 最接近
            new() { Id = 12, AccountId = 1L, Principal = 80000m, Status = "Matured" }
        };

        _fixedDepositServiceMock.Setup(x => x.GetByAccountAsync(1L))
            .ReturnsAsync(records);

        _fixedDepositServiceMock.Setup(x => x.WithdrawAsync(11L, It.IsAny<WithdrawFixedDepositRequest>()))
            .ReturnsAsync(new FixedDepositDto { Id = 11, Status = "Withdrawn", ActualInterest = 400m, Principal = 50000m });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 50000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert — 应选中 Id=11 的记录
        result.FixedDepositLinkage.Should().NotBeNull();
        result.FixedDepositLinkage!.FixedDepositId.Should().Be(11);
        _fixedDepositServiceMock.Verify(x => x.WithdrawAsync(11L, It.IsAny<WithdrawFixedDepositRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_FromFixedDeposit_MaturedStatus_ShouldAlsoMatch()
    {
        // Arrange: Matured 状态的记录也应被匹配
        var (_, _) = SetupAccounts(fromType: AccountType.FixedDeposit);

        var record = new FixedDepositDto
        {
            Id = 10,
            AccountId = 1L,
            Principal = 100000m,
            Status = "Matured"
        };

        _fixedDepositServiceMock.Setup(x => x.GetByAccountAsync(1L))
            .ReturnsAsync(new List<FixedDepositDto> { record });

        _fixedDepositServiceMock.Setup(x => x.WithdrawAsync(10L, It.IsAny<WithdrawFixedDepositRequest>()))
            .ReturnsAsync(new FixedDepositDto { Id = 10, Status = "Withdrawn", ActualInterest = 1650m, Principal = 100000m });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 100000m,
            TransactionDate = DateTime.Now
        };

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.FixedDepositLinkage.Should().NotBeNull();
        result.FixedDepositLinkage!.Action.Should().Be("Withdrawn");
    }

    // ========== 原有验证逻辑（确保不受联动影响） ==========

    [Fact]
    public async Task CreateTransferAsync_WithSameAccount_ShouldThrowValidationException()
    {
        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 1L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithZeroAmount_ShouldThrowValidationException()
    {
        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 0m,
            TransactionDate = DateTime.Now
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithInsufficientBalance_ShouldThrowValidationException()
    {
        SetupAccounts(fromBalance: 100m);

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 500m,
            TransactionDate = DateTime.Now
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithNonExistingAccount_ShouldThrowNotFoundException()
    {
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync((Account?)null);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(new Account { Id = 2 });

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateTransferAsync(request));
    }
}
