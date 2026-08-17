using FluentAssertions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MapsterMapper;

namespace FinanceApp.Application.Tests.Services;

public class FixedDepositServiceTests : TestBase
{
    private readonly Mock<IRepository<FixedDepositRecord>> _repositoryMock;
    private readonly Mock<IRepository<Account>> _accountRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<FixedDepositService>> _loggerMock;
    private readonly FixedDepositService _service;

    public FixedDepositServiceTests()
    {
        _repositoryMock = new Mock<IRepository<FixedDepositRecord>>();
        _accountRepositoryMock = new Mock<IRepository<Account>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<FixedDepositService>>();

        _tagBindingRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        _service = new FixedDepositService(
            _repositoryMock.Object,
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            UnitOfWorkMock.Object,
            AuditLogServiceMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            _mapperMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithFixedDepositAccount_ShouldCreateRecord()
    {
        var account = new Account
        {
            Id = 1,
            Name = "定期账户",
            AccountType = AccountType.FixedDeposit,
            IsActive = true
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FixedDepositRecord>()))
            .ReturnsAsync((FixedDepositRecord entity) =>
            {
                entity.Id = 1;
                return entity;
            });

        var result = await _service.CreateAsync(new CreateFixedDepositRequest
        {
            AccountId = 1,
            Principal = 10000m,
            TermMonths = 6,
            InterestRate = 2.5m,
            DepositDate = new DateTime(2026, 3, 1)
        });

        result.Id.Should().Be(1);
        result.AccountId.Should().Be(1);
        result.Principal.Should().Be(10000m);
        result.MaturityDate.Should().Be(new DateTime(2026, 9, 1));
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task CreateAsync_WithDepositTransactionId_ShouldLinkTransaction()
    {
        var account = new Account
        {
            Id = 1,
            Name = "定期账户",
            AccountType = AccountType.FixedDeposit,
            IsActive = true
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FixedDepositRecord>()))
            .ReturnsAsync((FixedDepositRecord entity) =>
            {
                entity.Id = 2;
                return entity;
            });

        var result = await _service.CreateAsync(new CreateFixedDepositRequest
        {
            AccountId = 1,
            Principal = 50000m,
            TermMonths = 12,
            InterestRate = 1.65m,
            DepositDate = new DateTime(2026, 4, 1),
            DepositTransactionId = 99
        });

        result.Id.Should().Be(2);
        result.DepositTransactionId.Should().Be(99);
        result.Principal.Should().Be(50000m);

        _repositoryMock.Verify(x => x.AddAsync(It.Is<FixedDepositRecord>(r =>
            r.DepositTransactionId == 99 &&
            r.Principal == 50000m &&
            r.TermMonths == 12
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutDepositTransactionId_ShouldDefaultToZero()
    {
        var account = new Account
        {
            Id = 1,
            Name = "定期账户",
            AccountType = AccountType.FixedDeposit,
            IsActive = true
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FixedDepositRecord>()))
            .ReturnsAsync((FixedDepositRecord entity) =>
            {
                entity.Id = 3;
                return entity;
            });

        var result = await _service.CreateAsync(new CreateFixedDepositRequest
        {
            AccountId = 1,
            Principal = 20000m,
            TermMonths = 6,
            InterestRate = 1.45m
        });

        result.DepositTransactionId.Should().Be(0);

        _repositoryMock.Verify(x => x.AddAsync(It.Is<FixedDepositRecord>(r =>
            r.DepositTransactionId == 0
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonFixedDepositAccount_ShouldThrow()
    {
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Account
        {
            Id = 1,
            Name = "活期账户",
            AccountType = AccountType.Bank,
            IsActive = true
        });

        var action = () => _service.CreateAsync(new CreateFixedDepositRequest
        {
            AccountId = 1,
            Principal = 10000m,
            TermMonths = 6,
            InterestRate = 2.5m
        });

        await action.Should().ThrowAsync<FinanceApp.Application.Common.ValidationException>();
    }

    [Fact]
    public async Task GetAllAsync_WithMaturedStatus_ShouldReturnOverdueActiveRecords()
    {
        var account = new Account
        {
            Id = 1,
            Name = "定期账户",
            AccountType = AccountType.FixedDeposit,
            IsActive = true
        };

        var overdueRecord = new FixedDepositRecord
        {
            Id = 10,
            AccountId = 1,
            Account = account,
            Principal = 10000m,
            DepositDate = DateTime.UtcNow.AddMonths(-6),
            MaturityDate = DateTime.UtcNow.AddDays(-3),
            TermMonths = 3,
            InterestRate = 2.5m,
            Status = FixedDepositStatus.Active,
            IsDeleted = false
        };

        var activeRecord = new FixedDepositRecord
        {
            Id = 11,
            AccountId = 1,
            Account = account,
            Principal = 15000m,
            DepositDate = DateTime.UtcNow.AddDays(-10),
            MaturityDate = DateTime.UtcNow.AddMonths(2),
            TermMonths = 6,
            InterestRate = 2.8m,
            Status = FixedDepositStatus.Active,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<FixedDepositRecord> { overdueRecord, activeRecord }.AsQueryable().BuildMock().Object);

        var result = await _service.GetAllAsync(new GetFixedDepositsRequest
        {
            Status = "Matured"
        });

        result.Should().ContainSingle();
        result[0].Id.Should().Be(10);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldExcludeOverdueRecordsFromActiveCounts()
    {
        var records = new List<FixedDepositRecord>
        {
            new()
            {
                Id = 10,
                AccountId = 1,
                Principal = 10000m,
                DepositDate = DateTime.UtcNow.AddMonths(-6),
                MaturityDate = DateTime.UtcNow.AddDays(-1),
                TermMonths = 3,
                InterestRate = 2.5m,
                Status = FixedDepositStatus.Active,
                IsDeleted = false
            },
            new()
            {
                Id = 11,
                AccountId = 1,
                Principal = 15000m,
                DepositDate = DateTime.UtcNow.AddDays(-5),
                MaturityDate = DateTime.UtcNow.AddDays(15),
                TermMonths = 1,
                InterestRate = 2.1m,
                Status = FixedDepositStatus.Active,
                IsDeleted = false
            },
            new()
            {
                Id = 12,
                AccountId = 1,
                Principal = 8000m,
                DepositDate = DateTime.UtcNow.AddMonths(-2),
                MaturityDate = DateTime.UtcNow.AddMonths(-1),
                TermMonths = 1,
                InterestRate = 1.9m,
                Status = FixedDepositStatus.Withdrawn,
                IsDeleted = false
            }
        };

        _repositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(records.AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new GetFixedDepositsRequest());

        result.TotalCount.Should().Be(3);
        result.ActiveCount.Should().Be(1);
        result.WithdrawnCount.Should().Be(1);
        result.UpcomingCount.Should().Be(1);
        result.ActivePrincipal.Should().Be(15000m);
    }

    [Fact]
    public async Task WithdrawAsync_WithoutActualInterest_ShouldCalculateExpectedInterestWhenMatured()
    {
        var account = new Account { Id = 1, Name = "定期账户", AccountType = AccountType.FixedDeposit, IsActive = true };
        var record = new FixedDepositRecord
        {
            Id = 10,
            AccountId = 1,
            Account = account,
            Principal = 12000m,
            DepositDate = new DateTime(2026, 1, 1),
            MaturityDate = new DateTime(2026, 7, 1),
            TermMonths = 6,
            InterestRate = 2.4m,
            Status = FixedDepositStatus.Active,
            IsDeleted = false
        };

        // 模拟关联的交易记录
        var transaction = new Transaction
        {
            Id = 100,
            AccountId = 1,
            Amount = 12144m,  // 本金 + 预期利息
            TransactionType = TransactionType.Expense,
            TransactionDate = new DateTime(2026, 7, 1),
            Status = TransactionStatus.Confirmed
        };

        var queryable = new List<FixedDepositRecord> { record }.AsQueryable().BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(queryable.Object);
        _transactionRepositoryMock.Setup(x => x.GetByIdAsync(100)).ReturnsAsync(transaction);

        var result = await _service.WithdrawAsync(10, new WithdrawFixedDepositRequest
        {
            WithdrawalDate = new DateTime(2026, 7, 1),
            TransactionId = 100
        });

        result.Status.Should().Be("Withdrawn");
        result.ActualInterest.Should().Be(144m);
        result.IsEarlyWithdrawal.Should().BeFalse();
    }

    [Fact]
    public async Task GetWithdrawalCandidatesAsync_ShouldPopulateTransactionTags()
    {
        var today = DateTime.UtcNow.Date;
        var account = new Account
        {
            Id = 1,
            Name = "Fixed Deposit Account",
            AccountType = AccountType.FixedDeposit,
            IsActive = true
        };

        var record = new FixedDepositRecord
        {
            Id = 10,
            AccountId = 1,
            Account = account,
            Principal = 12000m,
            DepositDate = today.AddMonths(-6),
            MaturityDate = today,
            TermMonths = 6,
            InterestRate = 2.4m,
            Status = FixedDepositStatus.Active,
            IsDeleted = false
        };

        var candidate = new Transaction
        {
            Id = 100,
            AccountId = 1,
            Account = account,
            Amount = 12144m,
            TransactionType = TransactionType.Expense,
            TransactionDate = today,
            Status = TransactionStatus.Confirmed,
            IsDeleted = false
        };

        var tag = new Tag
        {
            Id = 200,
            Scope = TagScope.Transaction,
            Name = "candidate-tag",
            Color = "#123456",
            SortOrder = 1
        };

        var binding = new TagBinding
        {
            Id = 1,
            OwnerType = TagScope.Transaction,
            OwnerId = 100,
            TagId = 200,
            Tag = tag,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<FixedDepositRecord> { record }.AsQueryable().BuildMock().Object);
        _transactionRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { candidate }.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<TagBinding> { binding }.AsQueryable().BuildMock().Object);

        var result = await _service.GetWithdrawalCandidatesAsync(10);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(100);
        result[0].Tags.Should().ContainSingle();
        result[0].Tags[0].TagId.Should().Be(200);
        result[0].Tags[0].TagName.Should().Be("candidate-tag");
        result[0].Tags[0].TagColor.Should().Be("#123456");
    }
}
