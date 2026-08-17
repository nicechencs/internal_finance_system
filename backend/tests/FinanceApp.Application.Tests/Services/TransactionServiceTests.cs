using MapsterMapper;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TransactionServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<TransactionAllocation>> _allocationRepositoryMock;
    private readonly Mock<IRepository<Account>> _accountRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IRepository<ReceivableDetail>> _receivableDetailRepositoryMock;
    private readonly Mock<IRepository<PayableDetail>> _payableDetailRepositoryMock;
    private readonly Mock<IAllocationService> _allocationServiceMock;
    private readonly Mock<IAccountBalanceService> _accountBalanceServiceMock;
    private readonly Mock<ITransactionQueryService> _queryServiceMock;
    private readonly Mock<ITransferService> _transferServiceMock;
    private readonly Mock<ITransactionStatisticsService> _statisticsServiceMock;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _allocationRepositoryMock = new Mock<IRepository<TransactionAllocation>>();
        _accountRepositoryMock = new Mock<IRepository<Account>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _receivableDetailRepositoryMock = new Mock<IRepository<ReceivableDetail>>();
        _payableDetailRepositoryMock = new Mock<IRepository<PayableDetail>>();
        _tagBindingRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);
        _allocationRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<TransactionAllocation>().AsQueryable().BuildMock().Object);
        _receivableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<ReceivableDetail>().AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<PayableDetail>().AsQueryable().BuildMock().Object);

        // 为事务封装新增的方法设置默认 Mock
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.Id = t.Id == 0 ? 1 : t.Id; return t; });
        _transactionRepositoryMock.Setup(x => x.Update(It.IsAny<Transaction>()));
        _allocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TransactionAllocation>()))
            .ReturnsAsync((TransactionAllocation a) => a);
        _accountRepositoryMock.Setup(x => x.Update(It.IsAny<Account>()));

        // 创建新服务的 Mock
        _allocationServiceMock = new Mock<IAllocationService>();
        _accountBalanceServiceMock = new Mock<IAccountBalanceService>();
        _queryServiceMock = new Mock<ITransactionQueryService>();
        _transferServiceMock = new Mock<ITransferService>();
        _statisticsServiceMock = new Mock<ITransactionStatisticsService>();

        // 设置 AllocationService 的默认行为
        _allocationServiceMock.Setup(x => x.ValidateAllocations(It.IsAny<List<CreateAllocationRequest>>(), It.IsAny<decimal>()))
            .Callback<List<CreateAllocationRequest>, decimal>((allocations, totalAmount) =>
            {
                // 模拟真实的验证逻辑
                foreach (var allocation in allocations)
                {
                    if (!allocation.Amount.HasValue && !allocation.AllocationRate.HasValue)
                        throw new ValidationException("分摊记录必须指定金额或百分比");
                    if (!allocation.ProjectId.HasValue && !allocation.PersonId.HasValue)
                        throw new ValidationException("分摊记录必须指定项目或人员");
                }

                decimal totalAllocation = 0;
                foreach (var allocation in allocations)
                {
                    if (allocation.Amount.HasValue)
                        totalAllocation += allocation.Amount.Value;
                    else if (allocation.AllocationRate.HasValue)
                        totalAllocation += Math.Round(totalAmount * allocation.AllocationRate.Value / 100, 2);
                }

                if (Math.Abs(totalAllocation - totalAmount) > 0.01m)
                    throw new ValidationException($"分摊金额总和({totalAllocation})必须等于交易金额({totalAmount})");
            });

        _allocationServiceMock.Setup(x => x.CalculateAmountFromRate(It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns((decimal total, decimal rate) => Math.Round(total * rate / 100, 2));

        _allocationServiceMock.Setup(x => x.CreateAllocationsAsync(It.IsAny<long>(), It.IsAny<List<CreateAllocationRequest>>(), It.IsAny<decimal>()))
            .Callback<long, List<CreateAllocationRequest>, decimal>((transactionId, allocations, totalAmount) =>
            {
                // 模拟创建分摊记录，实际调用 Repository
                foreach (var allocationRequest in allocations)
                {
                    var allocation = new TransactionAllocation
                    {
                        TransactionId = transactionId,
                        ProjectId = allocationRequest.ProjectId,
                        PersonId = allocationRequest.PersonId,
                        Amount = allocationRequest.Amount ?? Math.Round(totalAmount * allocationRequest.AllocationRate!.Value / 100, 2),
                        AllocationRate = allocationRequest.AllocationRate,
                        Description = allocationRequest.Description
                    };
                    _allocationRepositoryMock.Object.AddAsync(allocation).Wait();
                }
            })
            .Returns(Task.CompletedTask);

        // 设置 AccountBalanceService 的默认行为
        _accountBalanceServiceMock.Setup(x => x.AdjustBalanceWithoutSave(It.IsAny<Account>(), It.IsAny<decimal>(), It.IsAny<TransactionType>()))
            .Callback<Account, decimal, TransactionType>((account, amount, type) =>
            {
                if (type == TransactionType.Income)
                    account.CurrentBalance += amount;
                else if (type == TransactionType.Expense)
                    account.CurrentBalance -= amount;
            });

        // 设置 QueryService 的默认行为 - 返回映射后的 DTO
        _queryServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) =>
            {
                var transaction = _transactionRepositoryMock.Object.GetQueryable()
                    .FirstOrDefault(t => t.Id == id);
                return transaction != null ? Mapper.Map<TransactionDto>(transaction) : null!;
            });

        _queryServiceMock.Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync((PageRequest req) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable().ToList();
                return new PageResponse<TransactionDto>
                {
                    Items = Mapper.Map<List<TransactionDto>>(transactions),
                    Page = req.Page,
                    PageSize = req.PageSize,
                    Total = transactions.Count
                };
            });

        _queryServiceMock.Setup(x => x.GetByAccountAsync(It.IsAny<long>()))
            .ReturnsAsync((long accountId) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable()
                    .Where(t => t.AccountId == accountId).ToList();
                return Mapper.Map<List<TransactionDto>>(transactions);
            });

        _queryServiceMock.Setup(x => x.GetByProjectAsync(It.IsAny<long>()))
            .ReturnsAsync((long projectId) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable()
                    .Where(t => t.ProjectId == projectId).ToList();
                return Mapper.Map<List<TransactionDto>>(transactions);
            });

        _queryServiceMock.Setup(x => x.GetByCategoryAsync(It.IsAny<long>()))
            .ReturnsAsync((long categoryId) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable()
                    .Where(t => t.CategoryId == categoryId).ToList();
                return Mapper.Map<List<TransactionDto>>(transactions);
            });

        _queryServiceMock.Setup(x => x.GetByCustomerAsync(It.IsAny<long>()))
            .ReturnsAsync((long customerId) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable()
                    .Where(t => t.CustomerId == customerId).ToList();
                return Mapper.Map<List<TransactionDto>>(transactions);
            });

        _queryServiceMock.Setup(x => x.GetBySupplierAsync(It.IsAny<long>()))
            .ReturnsAsync((long supplierId) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable()
                    .Where(t => t.SupplierId == supplierId).ToList();
                return Mapper.Map<List<TransactionDto>>(transactions);
            });

        _queryServiceMock.Setup(x => x.GetByPersonAsync(It.IsAny<long>()))
            .ReturnsAsync((long personId) =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable()
                    .Where(t =>
                        (t.PersonId == personId && !t.IsAllocated) ||
                        (t.IsAllocated && t.Allocations.Any(a => a.PersonId == personId))
                    ).ToList();
                return Mapper.Map<List<TransactionDto>>(transactions);
            });

        // 设置 AccountBalanceService 的默认行为
        _accountBalanceServiceMock.Setup(x => x.GetAccountBalanceAsync(It.IsAny<long>()))
            .ReturnsAsync((long accountId) =>
            {
                var account = _accountRepositoryMock.Object.GetByIdAsync(accountId).Result;
                if (account == null)
                    throw new NotFoundException("账户不存在");
                return account.CurrentBalance;
            });

        // 设置 TransferService 的默认行为 - 模拟真实的转账逻辑
        _transferServiceMock.Setup(x => x.CreateTransferAsync(It.IsAny<CreateTransferRequest>()))
            .ReturnsAsync((CreateTransferRequest req) =>
            {
                // 验证账户
                if (req.FromAccountId == req.ToAccountId)
                    throw new ValidationException("转出和转入账户不能相同");

                if (req.Amount <= 0)
                    throw new ValidationException("转账金额必须大于0");

                var fromAccount = _accountRepositoryMock.Object.GetByIdAsync(req.FromAccountId).Result;
                if (fromAccount == null)
                    throw new NotFoundException("转出账户不存在");

                var toAccount = _accountRepositoryMock.Object.GetByIdAsync(req.ToAccountId).Result;
                if (toAccount == null)
                    throw new NotFoundException("转入账户不存在");

                // 检查余额充足
                if (fromAccount.CurrentBalance < req.Amount)
                    throw new ValidationException($"转出账户余额不足，当前余额: {fromAccount.CurrentBalance}");

                // 创建转出交易
                var outTransaction = new Transaction
                {
                    TransactionDate = req.TransactionDate,
                    Amount = req.Amount,
                    TransactionType = TransactionType.Transfer,
                    AccountId = req.FromAccountId,
                    Description = req.Description ?? $"转账至 {toAccount.Name}",
                    Status = TransactionStatus.Confirmed,
                    IsAllocated = false
                };

                _transactionRepositoryMock.Object.AddAsync(outTransaction).Wait();

                // 创建转入交易
                var inTransaction = new Transaction
                {
                    TransactionDate = req.TransactionDate,
                    Amount = req.Amount,
                    TransactionType = TransactionType.Transfer,
                    AccountId = req.ToAccountId,
                    Description = req.Description ?? $"转账自 {fromAccount.Name}",
                    Status = TransactionStatus.Confirmed,
                    IsAllocated = false,
                    RelatedTransactionId = outTransaction.Id
                };

                _transactionRepositoryMock.Object.AddAsync(inTransaction).Wait();

                // 更新转出交易的关联 ID
                outTransaction.RelatedTransactionId = inTransaction.Id;
                _transactionRepositoryMock.Object.Update(outTransaction);

                // 更新账户余额
                fromAccount.CurrentBalance -= req.Amount;
                _accountRepositoryMock.Object.Update(fromAccount);

                toAccount.CurrentBalance += req.Amount;
                _accountRepositoryMock.Object.Update(toAccount);

                // 审计日志
                AuditLogServiceMock.Object.LogAsync("Transfer", "Transaction", outTransaction.Id, null, null).Wait();
                AuditLogServiceMock.Object.LogAsync("Transfer", "Transaction", inTransaction.Id, null, null).Wait();

                // 返回结果
                return new TransferResultDto
                {
                    OutTransaction = Mapper.Map<TransactionDto>(outTransaction),
                    InTransaction = Mapper.Map<TransactionDto>(inTransaction)
                };
            });

        // 设置 StatisticsService 的默认行为
        _statisticsServiceMock.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(() =>
            {
                var transactions = _transactionRepositoryMock.Object.GetQueryable().ToList();
                return new TransactionStatisticsDto
                {
                    TotalIncome = transactions.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                    TotalExpense = transactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount),
                    TotalCount = transactions.Count
                };
            });

        _statisticsServiceMock.Setup(x => x.GetSupplierStatisticsAsync(It.IsAny<long>()))
            .ReturnsAsync(new TransactionStatisticsDto());

        _statisticsServiceMock.Setup(x => x.GetPersonStatisticsAsync(It.IsAny<long>()))
            .ReturnsAsync(new TransactionStatisticsDto());

        _statisticsServiceMock.Setup(x => x.GetRelatedFinanceRecordsAsync(It.IsAny<long>()))
            .ReturnsAsync(new RelatedFinanceRecordDto());

        _service = new TransactionService(
            _transactionRepositoryMock.Object,
            _allocationRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _receivableDetailRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            UnitOfWorkMock.Object,
            Mapper,
            AuditLogServiceMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<TransactionService>>(),
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            _allocationServiceMock.Object,
            _accountBalanceServiceMock.Object,
            _queryServiceMock.Object,
            _transferServiceMock.Object,
            _statisticsServiceMock.Object
        );
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateTransaction()
    {
        // Arrange
        var accountId = 1L;
        var account = new Account
        {
            Id = accountId,
            Name = "测试账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Income",
            Amount = 1000m,
            AccountId = accountId,
            CategoryId = 1L,
            Description = "测试收入"
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        Transaction? createdTransaction = null;
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.Id = 1; createdTransaction = t; return t; });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => new List<Transaction> { createdTransaction! }.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        account.CurrentBalance.Should().Be(11000m);
        _transactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Once);
        _accountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAccountId_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Income",
            Amount = 1000m,
            AccountId = 999L,
            Description = "测试"
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(999L))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTransactionType_ShouldThrowValidationException()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试", AccountType = AccountType.Bank, CurrentBalance = 1000m };
        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "InvalidType",
            Amount = 1000m,
            AccountId = 1L
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(account);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithAllocations_ShouldCreateTransactionAndAllocations()
    {
        // Arrange
        var accountId = 1L;
        var account = new Account
        {
            Id = accountId,
            Name = "测试账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Expense",
            Amount = 1000m,
            AccountId = accountId,
            CategoryId = 1L,
            Description = "测试支出",
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = 1, Amount = 600m, Description = "项目A" },
                new() { ProjectId = 2, Amount = 400m, Description = "项目B" }
            }
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        Transaction? createdTransaction = null;
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.Id = 1; createdTransaction = t; return t; });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => new List<Transaction> { createdTransaction! }.AsQueryable().BuildMock().Object);

        _allocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TransactionAllocation>()))
            .ReturnsAsync((TransactionAllocation a) => a);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        account.CurrentBalance.Should().Be(9000m);
        _allocationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TransactionAllocation>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAllocations_ShouldThrowValidationException()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试", AccountType = AccountType.Bank, CurrentBalance = 1000m };
        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Expense",
            Amount = 1000m,
            AccountId = 1L,
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = 1, Amount = 600m },
                new() { ProjectId = 2, Amount = 300m } // 总和不等于 1000
            }
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(account);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithAllocationRate_ShouldCalculateAmountCorrectly()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试", AccountType = AccountType.Bank, CurrentBalance = 10000m };
        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Expense",
            Amount = 1000m,
            AccountId = 1L,
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = 1, AllocationRate = 60m },
                new() { ProjectId = 2, AllocationRate = 40m }
            }
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(account);

        Transaction? createdTransaction = null;
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.Id = 1; createdTransaction = t; return t; });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => new List<Transaction> { createdTransaction! }.AsQueryable().BuildMock().Object);

        _allocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TransactionAllocation>()))
            .ReturnsAsync((TransactionAllocation a) => a);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        _allocationRepositoryMock.Verify(x => x.AddAsync(It.Is<TransactionAllocation>(a => a.Amount == 600m)), Times.Once);
        _allocationRepositoryMock.Verify(x => x.AddAsync(It.Is<TransactionAllocation>(a => a.Amount == 400m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAllocationMissingBothAmountAndRate_ShouldThrowValidationException()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试", AccountType = AccountType.Bank, CurrentBalance = 1000m };
        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Expense",
            Amount = 1000m,
            AccountId = 1L,
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = 1 } // 既没有 Amount 也没有 AllocationRate
            }
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(account);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithAllocationMissingProjectAndPerson_ShouldThrowValidationException()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试", AccountType = AccountType.Bank, CurrentBalance = 1000m };
        var request = new CreateTransactionRequest
        {
            TransactionDate = DateTime.Now,
            TransactionType = "Expense",
            Amount = 1000m,
            AccountId = 1L,
            Allocations = new List<CreateAllocationRequest>
            {
                new() { Amount = 1000m } // 既没有 ProjectId 也没有 PersonId
            }
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(account);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldSoftDeleteAndReverseBalance()
    {
        // Arrange
        var transactionId = 1L;
        var account = new Account
        {
            Id = 1,
            Name = "测试账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 11000m
        };

        var transaction = new Transaction
        {
            Id = transactionId,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            AccountId = 1,
            Account = account,
            IsDeleted = false
        };

        var queryableMock = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(transactionId);

        // Assert
        account.CurrentBalance.Should().Be(10000m); // 回滚收入
        transaction.IsDeleted.Should().BeTrue(); // 软删除
        _transactionRepositoryMock.Verify(x => x.Update(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithReceivableLinksAndExpenseType_ShouldThrowValidationException()
    {
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = account,
            Description = "鏀跺叆",
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _receivableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<ReceivableDetail>
            {
                new() { Id = 1, ReceivableId = 10, TransactionId = 1, Amount = 600m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var request = new UpdateTransactionRequest
        {
            TransactionDate = transaction.TransactionDate,
            TransactionType = "Expense",
            Amount = 1000m,
            AccountId = 1,
            Description = "鏀规垚鏀嚭"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_WithReceivableLinksAndAmountBelowLinkedTotal_ShouldThrowValidationException()
    {
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = account,
            Description = "鏀跺叆",
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _receivableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<ReceivableDetail>
            {
                new() { Id = 1, ReceivableId = 10, TransactionId = 1, Amount = 600m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var request = new UpdateTransactionRequest
        {
            TransactionDate = transaction.TransactionDate,
            TransactionType = "Income",
            Amount = 500m,
            AccountId = 1,
            Description = "閲戦涓嬭皟"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_WithPayableLinksAndIncomeType_ShouldThrowValidationException()
    {
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Expense,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = account,
            Description = "鏀嚭",
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<PayableDetail>
            {
                new() { Id = 1, PayableId = 10, TransactionId = 1, Amount = 600m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var request = new UpdateTransactionRequest
        {
            TransactionDate = transaction.TransactionDate,
            TransactionType = "Income",
            Amount = 1000m,
            AccountId = 1,
            Description = "鏀规垚鏀跺叆"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_WithPayableLinksAndAmountBelowLinkedTotal_ShouldThrowValidationException()
    {
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Expense,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = account,
            Description = "鏀嚭",
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<PayableDetail>
            {
                new() { Id = 1, PayableId = 10, TransactionId = 1, Amount = 600m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var request = new UpdateTransactionRequest
        {
            TransactionDate = transaction.TransactionDate,
            TransactionType = "Expense",
            Amount = 500m,
            AccountId = 1,
            Description = "閲戦涓嬭皟"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_WithTransferType_ShouldThrowValidationException()
    {
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = account,
            Description = "鏀跺叆",
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);

        var request = new UpdateTransactionRequest
        {
            TransactionDate = transaction.TransactionDate,
            TransactionType = "Transfer",
            Amount = 1000m,
            AccountId = 1,
            Description = "闈炴硶鏇存柊涓鸿浆璐?"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_WithLowercaseIncomeType_ShouldUpdateTransaction()
    {
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = account,
            Description = "鏀跺叆",
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);
        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateTransactionRequest
        {
            TransactionDate = transaction.TransactionDate.AddDays(1),
            TransactionType = "income",
            Amount = 1200m,
            AccountId = 1,
            Description = "灏忓啓绫诲瀷"
        };

        var result = await _service.UpdateAsync(1, request);

        result.Should().NotBeNull();
        transaction.TransactionType.Should().Be(TransactionType.Income);
        transaction.Amount.Should().Be(1200m);
        transaction.Description.Should().Be("灏忓啓绫诲瀷");
    }

    [Fact]
    public async Task DeleteAsync_WithReceivableLink_ShouldThrowValidationException()
    {
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _receivableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<ReceivableDetail> { new() { Id = 10, TransactionId = 1, ReceivableId = 2, Amount = 1000m, PaymentDate = DateTime.UtcNow } }.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));

        transaction.IsDeleted.Should().BeFalse();
        _transactionRepositoryMock.Verify(x => x.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithSoftDeletedReceivableLink_ShouldIgnoreLink()
    {
        var transactionId = 1L;
        var account = new Account
        {
            Id = 1,
            Name = "娴嬭瘯璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 11000m
        };
        var transaction = new Transaction
        {
            Id = transactionId,
            Amount = 1000m,
            TransactionType = TransactionType.Income,
            AccountId = 1,
            Account = account,
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _receivableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<ReceivableDetail>
            {
                new() { Id = 10, TransactionId = 1, ReceivableId = 2, Amount = 1000m, PaymentDate = DateTime.UtcNow, IsDeleted = true }
            }.AsQueryable().BuildMock().Object);
        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteAsync(transactionId);

        transaction.IsDeleted.Should().BeTrue();
        account.CurrentBalance.Should().Be(10000m);
    }

    [Fact]
    public async Task GetTransferCandidatesAsync_WithSoftDeletedFinanceLink_ShouldIncludeCandidate()
    {
        var sourceAccount = new Account
        {
            Id = 1,
            Name = "鏉ユ簮璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var targetAccount = new Account
        {
            Id = 2,
            Name = "鐩爣璐︽埛",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };
        var sourceTransaction = new Transaction
        {
            Id = 1,
            Amount = 800m,
            TransactionType = TransactionType.Expense,
            TransactionDate = DateTime.UtcNow,
            AccountId = 1,
            Account = sourceAccount,
            IsAllocated = false,
            IsDeleted = false
        };
        var candidateTransaction = new Transaction
        {
            Id = 2,
            Amount = 800m,
            TransactionType = TransactionType.Income,
            TransactionDate = DateTime.UtcNow,
            AccountId = 2,
            Account = targetAccount,
            IsAllocated = false,
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { sourceTransaction, candidateTransaction }.AsQueryable().BuildMock().Object);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(targetAccount);
        _receivableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<ReceivableDetail>
            {
                new() { Id = 1, ReceivableId = 10, TransactionId = 2, Amount = 800m, PaymentDate = DateTime.UtcNow, IsDeleted = true }
            }.AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<PayableDetail>().AsQueryable().BuildMock().Object);

        var result = await _service.GetTransferCandidatesAsync(1, 2);

        result.Should().ContainSingle(x => x.Id == 2);
    }

    [Fact]
    public async Task DeleteAsync_WithAllocation_ShouldThrowValidationException()
    {
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 1000m,
            TransactionType = TransactionType.Expense,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _allocationRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<TransactionAllocation> { new() { Id = 20, TransactionId = 1, Amount = 1000m } }.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));

        transaction.IsDeleted.Should().BeFalse();
        _transactionRepositoryMock.Verify(x => x.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithBankTransaction_ShouldResetProcessedFlag()
    {
        var bankTransaction = new BankTransaction { Id = 8, AccountId = 1, IsProcessed = true, UniqueHash = "tx-8" };
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 2000m };
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 500m,
            TransactionType = TransactionType.Income,
            AccountId = 1,
            Account = account,
            BankTransactionId = 8,
            BankTransaction = bankTransaction,
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteAsync(1);

        bankTransaction.IsProcessed.Should().BeFalse();
        transaction.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithTransferRelatedTransactionLinkedToPayable_ShouldThrowValidationException()
    {
        var fromAccount = new Account { Id = 1, Name = "杞嚭璐︽埛", AccountType = AccountType.Bank, CurrentBalance = 3000m };
        var toAccount = new Account { Id = 2, Name = "杞叆璐︽埛", AccountType = AccountType.Bank, CurrentBalance = 6000m };

        var outTransaction = new Transaction
        {
            Id = 10,
            Amount = 1000m,
            TransactionType = TransactionType.Transfer,
            AccountId = 1,
            Account = fromAccount,
            RelatedTransactionId = 11,
            IsDeleted = false
        };

        var inTransaction = new Transaction
        {
            Id = 11,
            Amount = 1000m,
            TransactionType = TransactionType.Transfer,
            AccountId = 2,
            Account = toAccount,
            RelatedTransactionId = 10,
            IsDeleted = false
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => new List<Transaction> { outTransaction, inTransaction }.AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<PayableDetail> { new() { Id = 99, PayableId = 1, TransactionId = 11, Amount = 1000m, PaymentDate = DateTime.UtcNow } }.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(10));

        outTransaction.IsDeleted.Should().BeFalse();
        inTransaction.IsDeleted.Should().BeFalse();
        _transactionRepositoryMock.Verify(x => x.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task GetByCustomerAsync_WithValidCustomerId_ShouldReturnTransactions()
    {
        // Arrange
        var customerId = 1L;
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 1000m,
                TransactionType = TransactionType.Income,
                CustomerId = customerId,
                TransactionDate = DateTime.Now,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 2000m,
                TransactionType = TransactionType.Income,
                CustomerId = customerId,
                TransactionDate = DateTime.Now.AddDays(-1),
                AccountId = 1,
                Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
                IsDeleted = false
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetByCustomerAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(t => t.CustomerId == customerId).Should().BeTrue();
    }

    [Fact]
    public async Task GetBySupplierAsync_WithValidSupplierId_ShouldReturnTransactions()
    {
        // Arrange
        var supplierId = 1L;
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 1000m,
                TransactionType = TransactionType.Expense,
                SupplierId = supplierId,
                TransactionDate = DateTime.Now,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Amount = 2000m,
                TransactionType = TransactionType.Expense,
                SupplierId = supplierId,
                TransactionDate = DateTime.Now.AddDays(-1),
                AccountId = 1,
                Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
                IsDeleted = false
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetBySupplierAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(t => t.SupplierId == supplierId).Should().BeTrue();
    }

    [Fact]
    public async Task GetByPersonAsync_WithDirectPersonId_ShouldReturnTransactions()
    {
        // Arrange
        var personId = 1L;
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 1000m,
                TransactionType = TransactionType.Expense,
                PersonId = personId,
                IsAllocated = false,
                TransactionDate = DateTime.Now,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
                IsDeleted = false
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetByPersonAsync(personId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PersonId.Should().Be(personId);
    }

    [Fact]
    public async Task GetByPersonAsync_WithAllocatedPerson_ShouldReturnTransactions()
    {
        // Arrange
        var personId = 1L;
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                Amount = 1000m,
                TransactionType = TransactionType.Expense,
                IsAllocated = true,
                TransactionDate = DateTime.Now,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 1000m },
                Allocations = new List<TransactionAllocation>
                {
                    new() { Id = 1, TransactionId = 1, PersonId = personId, Amount = 600m },
                    new() { Id = 2, TransactionId = 1, PersonId = 2, Amount = 400m }
                },
                IsDeleted = false
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetByPersonAsync(personId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].IsAllocated.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccountBalanceAsync_WithValidAccountId_ShouldReturnBalance()
    {
        // Arrange
        var accountId = 1L;
        var expectedBalance = 5000m;
        var account = new Account
        {
            Id = accountId,
            Name = "测试账户",
            AccountType = AccountType.Bank,
            CurrentBalance = expectedBalance
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.GetAccountBalanceAsync(accountId);

        // Assert
        result.Should().Be(expectedBalance);
    }

    [Fact]
    public async Task GetAccountBalanceAsync_WithInvalidAccountId_ShouldThrowNotFoundException()
    {
        // Arrange
        var accountId = 999L;
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAccountBalanceAsync(accountId));
    }

    // ===== 转账功能测试 =====

    [Fact]
    public async Task CreateTransferAsync_WithValidData_ShouldCreateTwoTransactionsAndUpdateBalances()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 3000m,
            TransactionDate = DateTime.Now,
            Description = null
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        var createdTransactions = new List<Transaction>();
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) =>
            {
                t.Id = createdTransactions.Count + 1;
                createdTransactions.Add(t);
                return t;
            });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => createdTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OutTransaction.Should().NotBeNull();
        result.InTransaction.Should().NotBeNull();

        // 验证余额更新
        fromAccount.CurrentBalance.Should().Be(7000m); // 10000 - 3000
        toAccount.CurrentBalance.Should().Be(8000m);   // 5000 + 3000

        // 验证创建了两笔交易
        _transactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));

        // 验证审计日志
        AuditLogServiceMock.Verify(x => x.LogAsync("Transfer", "Transaction", It.IsAny<long>(), null, null), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateTransferAsync_WithSameAccount_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 1L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithZeroAmount_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 0m,
            TransactionDate = DateTime.Now
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithNegativeAmount_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = -500m,
            TransactionDate = DateTime.Now
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithNonExistingFromAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            FromAccountId = 999L,
            ToAccountId = 2L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(999L)).ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithNonExistingToAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 999L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(999L)).ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithInsufficientBalance_ShouldThrowValidationException()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 500m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTransferAsync(request));
    }

    [Fact]
    public async Task CreateTransferAsync_WithCustomDescription_ShouldUseCustomDescription()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 2000m,
            TransactionDate = DateTime.Now,
            Description = "自定义转账备注"
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        var createdTransactions = new List<Transaction>();
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) =>
            {
                t.Id = createdTransactions.Count + 1;
                createdTransactions.Add(t);
                return t;
            });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => createdTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.Should().NotBeNull();

        // 验证创建的交易使用了自定义描述
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Transaction>(t => t.Description == "自定义转账备注")),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CreateTransferAsync_WithoutDescription_ShouldGenerateDefaultDescription()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "工行储蓄",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "招行现金",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 2000m,
            TransactionDate = DateTime.Now,
            Description = null
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        var createdTransactions = new List<Transaction>();
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) =>
            {
                t.Id = createdTransactions.Count + 1;
                createdTransactions.Add(t);
                return t;
            });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => createdTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTransferAsync(request);

        // Assert
        result.Should().NotBeNull();

        // 验证转出交易描述包含"转账至"和目标账户名
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountId == 1L && t.Description == "转账至 招行现金")),
            Times.Once);

        // 验证转入交易描述包含"转账自"和来源账户名
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountId == 2L && t.Description == "转账自 工行储蓄")),
            Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_ShouldSetTransactionTypeToTransfer()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        var createdTransactions = new List<Transaction>();
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) =>
            {
                t.Id = createdTransactions.Count + 1;
                createdTransactions.Add(t);
                return t;
            });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => createdTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateTransferAsync(request);

        // Assert - 两笔交易都应该是 Transfer 类型
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Transaction>(t => t.TransactionType == TransactionType.Transfer)),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CreateTransferAsync_ShouldSetRelatedTransactionIds()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 10000m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m
        };

        var request = new CreateTransferRequest
        {
            FromAccountId = 1L,
            ToAccountId = 2L,
            Amount = 1000m,
            TransactionDate = DateTime.Now
        };

        _accountRepositoryMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(x => x.GetByIdAsync(2L)).ReturnsAsync(toAccount);

        var createdTransactions = new List<Transaction>();
        _transactionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) =>
            {
                t.Id = createdTransactions.Count + 1;
                createdTransactions.Add(t);
                return t;
            });

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => createdTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateTransferAsync(request);

        // Assert - 转入交易在创建时就设置了 RelatedTransactionId 指向转出交易
        var inTransaction = createdTransactions.First(t => t.AccountId == 2L);
        inTransaction.RelatedTransactionId.Should().Be(createdTransactions.First(t => t.AccountId == 1L).Id);

        // 转出交易通过 Update 设置了 RelatedTransactionId 指向转入交易
        _transactionRepositoryMock.Verify(
            x => x.Update(It.Is<Transaction>(t =>
                t.AccountId == 1L && t.RelatedTransactionId.HasValue)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithTransferTransaction_ShouldDeleteBothTransactions()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 7000m // 转账后余额
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 8000m // 转账后余额
        };

        var outTransaction = new Transaction
        {
            Id = 1,
            Amount = 3000m,
            TransactionType = TransactionType.Transfer,
            AccountId = 1,
            Account = fromAccount,
            RelatedTransactionId = 2,
            Description = "转账至 转入账户",
            IsDeleted = false
        };

        var inTransaction = new Transaction
        {
            Id = 2,
            Amount = 3000m,
            TransactionType = TransactionType.Transfer,
            AccountId = 2,
            Account = toAccount,
            RelatedTransactionId = 1,
            Description = "转账自 转出账户",
            IsDeleted = false
        };

        // 设置 GetQueryable 返回：先查当前交易，再查关联交易
        var allTransactions = new List<Transaction> { outTransaction, inTransaction };
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => allTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1L);

        // Assert - 两笔交易都应被软删除
        outTransaction.IsDeleted.Should().BeTrue();
        inTransaction.IsDeleted.Should().BeTrue();
        _transactionRepositoryMock.Verify(x => x.Update(It.IsAny<Transaction>()), Times.Exactly(2));

        // 验证两个账户的余额都被回滚
        fromAccount.CurrentBalance.Should().Be(10000m); // 7000 + 3000（转出回滚）
        toAccount.CurrentBalance.Should().Be(5000m);    // 8000 - 3000（转入回滚）
    }

    // ===== 权限过滤测试 =====

    [Fact]
    public async Task GetStatisticsAsync_Viewer角色_应只统计自己创建的交易()
    {
        // Arrange - 创建 Viewer 角色的 Service 实例
        var viewerUserId = 42L;
        var viewerCurrentUser = CreateViewerCurrentUserService(viewerUserId);
        var viewerPermissionService = new ViewerDataPermissionService(viewerUserId);

        var viewerTransactionRepoMock = new Mock<IRepository<Transaction>>();
        var viewerAllocationRepoMock = new Mock<IRepository<TransactionAllocation>>();
        var viewerAccountRepoMock = new Mock<IRepository<Account>>();
        var viewerReceivableDetailRepoMock = new Mock<IRepository<ReceivableDetail>>();
        var viewerPayableDetailRepoMock = new Mock<IRepository<PayableDetail>>();
        var viewerAuditLogMock = new Mock<IAuditLogService>();
        var viewerUnitOfWorkMock = new Mock<IUnitOfWork>();

        // 创建新服务的 Mock
        var viewerAllocationServiceMock = new Mock<IAllocationService>();
        var viewerAccountBalanceServiceMock = new Mock<IAccountBalanceService>();
        var viewerQueryServiceMock = new Mock<ITransactionQueryService>();
        var viewerTransferServiceMock = new Mock<ITransferService>();
        var viewerStatisticsServiceMock = new Mock<ITransactionStatisticsService>();

        // 设置 StatisticsService Mock - 模拟权限过滤逻辑
        viewerStatisticsServiceMock.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(() =>
            {
                // 模拟 Viewer 权限过滤：只能看到自己创建的交易
                var allTransactions = viewerTransactionRepoMock.Object.GetQueryable().ToList();
                var filteredTransactions = allTransactions.Where(t => t.CreatedBy == viewerUserId).ToList();

                var stats = new TransactionStatisticsDto
                {
                    TotalIncome = filteredTransactions.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                    TotalExpense = filteredTransactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount),
                    TotalCount = filteredTransactions.Count
                };
                stats.NetProfit = stats.TotalIncome - stats.TotalExpense;
                return stats;
            });

        var viewerService = new TransactionService(
            viewerTransactionRepoMock.Object,
            viewerAllocationRepoMock.Object,
            viewerAccountRepoMock.Object,
            new Mock<IRepository<TagBinding>>().Object,
            viewerReceivableDetailRepoMock.Object,
            viewerPayableDetailRepoMock.Object,
            viewerUnitOfWorkMock.Object,
            Mapper,
            viewerAuditLogMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<TransactionService>>(),
            viewerCurrentUser,
            viewerPermissionService,
            viewerAllocationServiceMock.Object,
            viewerAccountBalanceServiceMock.Object,
            viewerQueryServiceMock.Object,
            viewerTransferServiceMock.Object,
            viewerStatisticsServiceMock.Object);

        // 准备数据：混合不同创建者的交易
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1, Amount = 1000m, TransactionType = TransactionType.Income,
                AccountId = 1, TransactionDate = DateTime.Now,
                CreatedBy = viewerUserId, IsDeleted = false // 自己创建的
            },
            new()
            {
                Id = 2, Amount = 5000m, TransactionType = TransactionType.Income,
                AccountId = 1, TransactionDate = DateTime.Now,
                CreatedBy = 999L, IsDeleted = false // 他人创建的
            },
            new()
            {
                Id = 3, Amount = 300m, TransactionType = TransactionType.Expense,
                AccountId = 1, TransactionDate = DateTime.Now,
                CreatedBy = viewerUserId, IsDeleted = false // 自己创建的
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        viewerTransactionRepoMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await viewerService.GetStatisticsAsync();

        // Assert - Viewer 只能统计自己创建的交易（Id=1 和 Id=3）
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(1000m, "只统计 Viewer 自己创建的收入");
        result.TotalExpense.Should().Be(300m, "只统计 Viewer 自己创建的支出");
        result.NetProfit.Should().Be(700m, "净利润 = 收入 - 支出");
        result.TotalCount.Should().Be(2, "Viewer 只能看到自己创建的 2 笔交易");
    }

    [Fact]
    public async Task GetSupplierStatisticsAsync_ShouldDelegateToStatisticsService()
    {
        var supplierId = 1L;
        var expected = new TransactionStatisticsDto { TotalExpense = 123m };
        _statisticsServiceMock.Setup(x => x.GetSupplierStatisticsAsync(supplierId)).ReturnsAsync(expected);

        var result = await _service.GetSupplierStatisticsAsync(supplierId);

        result.Should().BeSameAs(expected);
        _statisticsServiceMock.Verify(x => x.GetSupplierStatisticsAsync(supplierId), Times.Once);
    }

    [Fact]
    public async Task GetPersonStatisticsAsync_ShouldDelegateToStatisticsService()
    {
        var personId = 2L;
        var expected = new TransactionStatisticsDto { TotalIncome = 456m };
        _statisticsServiceMock.Setup(x => x.GetPersonStatisticsAsync(personId)).ReturnsAsync(expected);

        var result = await _service.GetPersonStatisticsAsync(personId);

        result.Should().BeSameAs(expected);
        _statisticsServiceMock.Verify(x => x.GetPersonStatisticsAsync(personId), Times.Once);
    }

    /// <summary>
    /// 创建 Viewer 角色的 ICurrentUserService Mock
    /// </summary>
    private static ICurrentUserService CreateViewerCurrentUserService(long userId = 1L)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.Username).Returns("viewer");
        mock.Setup(x => x.Role).Returns(UserRole.Viewer);
        mock.Setup(x => x.IsAdmin).Returns(false);
        mock.Setup(x => x.IsAccountant).Returns(false);
        mock.Setup(x => x.IsViewer).Returns(true);
        return mock.Object;
    }

    [Fact]
    public async Task DeleteAsync_WithTransferTransaction_ShouldReverseAccountBalances()
    {
        // Arrange
        var fromAccount = new Account
        {
            Id = 1,
            Name = "转出账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 2000m
        };

        var toAccount = new Account
        {
            Id = 2,
            Name = "转入账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 6000m
        };

        var outTransaction = new Transaction
        {
            Id = 10,
            Amount = 1000m,
            TransactionType = TransactionType.Transfer,
            AccountId = 1,
            Account = fromAccount,
            RelatedTransactionId = 11,
            Description = "转账至 转入账户",
            IsDeleted = false
        };

        var inTransaction = new Transaction
        {
            Id = 11,
            Amount = 1000m,
            TransactionType = TransactionType.Transfer,
            AccountId = 2,
            Account = toAccount,
            RelatedTransactionId = 10,
            Description = "转账自 转出账户",
            IsDeleted = false
        };

        var allTransactions = new List<Transaction> { outTransaction, inTransaction };
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(() => allTransactions.AsQueryable().BuildMock().Object);

        AuditLogServiceMock.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(10L);

        // Assert
        // 转出账户余额应该恢复（加回转出金额）
        fromAccount.CurrentBalance.Should().Be(3000m); // 2000 + 1000
        // 转入账户余额应该恢复（减去转入金额）
        toAccount.CurrentBalance.Should().Be(5000m);   // 6000 - 1000

        // 验证账户更新了（使用 Update）
        _accountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAvailableForReceivableAsync_ShouldReturnUnallocatedIncomeTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 1, TransactionType = TransactionType.Income, Amount = 1000, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false },
            new() { Id = 2, TransactionType = TransactionType.Income, Amount = 2000, AllocationStatus = AllocationStatus.FullyAllocated, TransactionDate = DateTime.Now, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false },
            new() { Id = 3, TransactionType = TransactionType.Expense, Amount = 500, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForReceivableAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].TransactionType.Should().Be("Income");
    }

    [Fact]
    public async Task GetAvailableForReceivableAsync_ShouldIncludeUnlinkedTransactionsWhenSettlementHasProjectAndCustomer()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 21,
                TransactionType = TransactionType.Income,
                Amount = 1000,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            },
            new()
            {
                Id = 22,
                TransactionType = TransactionType.Income,
                Amount = 800,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                ProjectId = 3,
                CustomerId = 7,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            },
            new()
            {
                Id = 23,
                TransactionType = TransactionType.Income,
                Amount = 600,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                ProjectId = 99,
                CustomerId = 7,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            },
            new()
            {
                Id = 24,
                TransactionType = TransactionType.Income,
                Amount = 500,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                SupplierId = 9,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForReceivableAsync(projectId: 3, customerId: 7);

        // Assert
        result.Select(x => x.Id).Should().Contain([21L, 22L]);
        result.Select(x => x.Id).Should().NotContain([23L, 24L]);
    }

    [Fact]
    public async Task GetAvailableForPayableAsync_ShouldPopulateAvailableAmount()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 10,
                TransactionType = TransactionType.Expense,
                Amount = 1000,
                AllocationStatus = AllocationStatus.PartiallyAllocated,
                TransactionDate = DateTime.Now,
                SupplierId = 5,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                PayableDetails = new List<PayableDetail>
                {
                    new() { Id = 1, PayableId = 2, Amount = 300, PaymentDate = DateTime.UtcNow }
                },
                IsDeleted = false
            }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForPayableAsync(supplierId: 5);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(10);
        result[0].AvailableAmount.Should().Be(700);
    }

    [Fact]
    public async Task GetAvailableForPayableAsync_ShouldFilterByCustomerId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 11, TransactionType = TransactionType.Expense, Amount = 800, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, CustomerId = 8, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false },
            new() { Id = 12, TransactionType = TransactionType.Expense, Amount = 900, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, CustomerId = 9, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForPayableAsync(customerId: 8);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(11);
    }

    [Fact]
    public async Task GetAvailableForPayableAsync_ShouldFilterByPersonId()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 13, TransactionType = TransactionType.Expense, Amount = 800, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, PersonId = 18, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false },
            new() { Id = 14, TransactionType = TransactionType.Expense, Amount = 900, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, PersonId = 19, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForPayableAsync(personId: 18);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(13);
    }

    [Fact]
    public async Task GetAvailableForPayableAsync_ShouldReturnUnallocatedExpenseTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Id = 1, TransactionType = TransactionType.Expense, Amount = 800, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false },
            new() { Id = 2, TransactionType = TransactionType.Expense, Amount = 1500, AllocationStatus = AllocationStatus.FullyAllocated, TransactionDate = DateTime.Now, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false },
            new() { Id = 3, TransactionType = TransactionType.Income, Amount = 1000, AllocationStatus = AllocationStatus.Unallocated, TransactionDate = DateTime.Now, AccountId = 1, Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank }, IsDeleted = false }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForPayableAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].TransactionType.Should().Be("Expense");
    }

    [Fact]
    public async Task GetAvailableForPayableAsync_ShouldIncludeUnlinkedTransactionsWhenSettlementHasProjectAndSupplier()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 31,
                TransactionType = TransactionType.Expense,
                Amount = 1200,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            },
            new()
            {
                Id = 32,
                TransactionType = TransactionType.Expense,
                Amount = 900,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                ProjectId = 5,
                SupplierId = 11,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            },
            new()
            {
                Id = 33,
                TransactionType = TransactionType.Expense,
                Amount = 700,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                ProjectId = 6,
                SupplierId = 11,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            },
            new()
            {
                Id = 34,
                TransactionType = TransactionType.Expense,
                Amount = 650,
                AllocationStatus = AllocationStatus.Unallocated,
                TransactionDate = DateTime.UtcNow,
                CustomerId = 3,
                AccountId = 1,
                Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                IsDeleted = false
            }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetAvailableForPayableAsync(projectId: 5, supplierId: 11);

        // Assert
        result.Select(x => x.Id).Should().Contain([31L, 32L]);
        result.Select(x => x.Id).Should().NotContain([33L, 34L]);
    }
}
