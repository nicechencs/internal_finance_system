using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class PayableServiceTests : TestBase
{
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock;
    private readonly Mock<IRepository<PayableDetail>> _detailRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepositoryMock;
    private readonly Mock<IRepository<Customer>> _customerRepositoryMock;
    private readonly Mock<IRepository<Person>> _personRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IRepository<ReceivableDetail>> _receivableDetailRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IRepository<PayableType>> _payableTypeRepositoryMock;
    private readonly Mock<IProjectFinancialRecalculationService> _recalculationServiceMock;
    private readonly Mock<ILogger<PayableService>> _loggerMock;
    private readonly PayableService _service;

    public PayableServiceTests()
    {
        _payableRepositoryMock = new Mock<IRepository<Payable>>();
        _detailRepositoryMock = new Mock<IRepository<PayableDetail>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _supplierRepositoryMock = new Mock<IRepository<Supplier>>();
        _customerRepositoryMock = new Mock<IRepository<Customer>>();
        _personRepositoryMock = new Mock<IRepository<Person>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _receivableDetailRepositoryMock = new Mock<IRepository<ReceivableDetail>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _payableTypeRepositoryMock = new Mock<IRepository<PayableType>>();
        _recalculationServiceMock = new Mock<IProjectFinancialRecalculationService>();
        _loggerMock = new Mock<ILogger<PayableService>>();
        _detailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<PayableDetail>().AsQueryable().BuildMock().Object);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>().AsQueryable().BuildMock().Object);
        _receivableDetailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<ReceivableDetail>().AsQueryable().BuildMock().Object);
        var bindingService = new SettlementTransactionBindingService(
            _transactionRepositoryMock.Object,
            _receivableDetailRepositoryMock.Object,
            _detailRepositoryMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        var transactionAllocationHelper = new TransactionAllocationHelper(
            _transactionRepositoryMock.Object,
            UnitOfWorkMock.Object,
            new Mock<ILogger<TransactionAllocationHelper>>().Object);

        _service = new PayableService(
            _payableRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _personRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _payableTypeRepositoryMock.Object,
            Mapper,
            _loggerMock.Object,
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object,
            bindingService,
            transactionAllocationHelper,
            _recalculationServiceMock.Object
        );
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var payables = new List<Payable>
        {
            new() { Id = 1, TotalAmount = 10000, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TotalAmount = 20000, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(payables.AsQueryable().BuildMock().Object);

        var result = await _service.GetPagedAsync(new PageRequest { Page = 1, PageSize = 10 });

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithSupplierCounterparty_ShouldCreatePayable()
    {
        var supplier = new Supplier { Id = 1, Name = "供应商1" };
        var request = new CreatePayableRequest { SupplierId = 1, TotalAmount = 10000 };
        Payable? created = null;

        _supplierRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
        _payableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Payable>()))
            .ReturnsAsync((Payable p) =>
            {
                p.Id = 1;
                p.Supplier = supplier;
                created = p;
                return p;
            });
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Payable> { created! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        created.Should().NotBeNull();
        created!.SupplierId.Should().Be(1);
        created.CustomerId.Should().BeNull();
        created.PersonId.Should().BeNull();
        result.SupplierName.Should().Be("供应商1");
    }

    [Fact]
    public async Task CreateAsync_WithCustomerCounterparty_ShouldCreatePayable()
    {
        var customer = new Customer { Id = 2, Name = "客户1" };
        var request = new CreatePayableRequest { CustomerId = 2, TotalAmount = 10000 };
        Payable? created = null;

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(customer);
        _payableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Payable>()))
            .ReturnsAsync((Payable p) =>
            {
                p.Id = 1;
                p.Customer = customer;
                created = p;
                return p;
            });
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Payable> { created! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        created.Should().NotBeNull();
        created!.SupplierId.Should().BeNull();
        created.CustomerId.Should().Be(2);
        created.PersonId.Should().BeNull();
        result.CustomerName.Should().Be("客户1");
    }

    [Fact]
    public async Task CreateAsync_WithPersonCounterparty_ShouldCreatePayable()
    {
        var person = new Person { Id = 3, Name = "李四", PersonType = PersonType.Employee };
        var request = new CreatePayableRequest { PersonId = 3, TotalAmount = 10000 };
        Payable? created = null;

        _personRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(person);
        _payableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Payable>()))
            .ReturnsAsync((Payable p) =>
            {
                p.Id = 1;
                p.Person = person;
                created = p;
                return p;
            });
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Payable> { created! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        created.Should().NotBeNull();
        created!.SupplierId.Should().BeNull();
        created.CustomerId.Should().BeNull();
        created.PersonId.Should().Be(3);
        result.PersonName.Should().Be("李四");
    }

    [Fact]
    public async Task CreateAsync_WithoutCounterparty_ShouldThrowValidationException()
    {
        var act = () => _service.CreateAsync(new CreatePayableRequest { TotalAmount = 10000 });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须选择一个对方*");
    }

    [Fact]
    public async Task CreateAsync_WithMultipleCounterparties_ShouldThrowValidationException()
    {
        var act = () => _service.CreateAsync(new CreatePayableRequest
        {
            SupplierId = 1,
            CustomerId = 2,
            TotalAmount = 10000
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*只能选择一个对方*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowSwitchingToCustomerCounterparty()
    {
        var customer = new Customer { Id = 2, Name = "客户1" };
        var payable = new Payable
        {
            Id = 1,
            SupplierId = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            Supplier = new Supplier { Id = 1, Name = "供应商1" }
        };

        _payableRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payable);
        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _customerRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(customer);

        await _service.UpdateAsync(1, new UpdatePayableRequest
        {
            CustomerId = 2,
            TotalAmount = 10000
        });

        payable.SupplierId.Should().BeNull();
        payable.CustomerId.Should().Be(2);
        payable.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task PayPaymentAsync_WithNullTransactionId_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest { Amount = 5000, PaymentDate = DateTime.UtcNow });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须关联交易记录*");
    }

    [Fact]
    public async Task PayPaymentAsync_WithZeroTransactionId_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 0
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须关联交易记录*");
    }

    [Fact]
    public async Task PayPaymentAsync_WithNegativeTransactionId_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = -1
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须关联交易记录*");
    }

    [Fact]
    public async Task PayPaymentAsync_WithValidRequest_ShouldUpdatePayable()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>())).ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 87,
                    Amount = 5000m,
                    TransactionType = TransactionType.Expense,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        await _service.PayPaymentAsync(1, new PayPaymentRequest { Amount = 5000, PaymentDate = DateTime.UtcNow, TransactionId = 87 });

        payable.PaidAmount.Should().Be(5000);
        payable.RemainingAmount.Should().Be(5000);
        payable.Status.Should().Be(PayableStatus.Partial);
    }

    [Fact]
    public async Task PayPaymentAsync_WithIncomeTransactionId_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 88,
                    Amount = 5000m,
                    TransactionType = TransactionType.Income,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "璐︽埛", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 88
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PayPaymentAsync_WhenTransactionAlreadyLinkedToReceivable_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 89,
                    Amount = 8000m,
                    TransactionType = TransactionType.Expense,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "璐︽埛", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);
        _receivableDetailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<ReceivableDetail>
            {
                new() { Id = 1, ReceivableId = 20, TransactionId = 89, Amount = 3000m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 89
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PayPaymentAsync_WhenTransactionAlreadyLinkedToSamePayable_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 3000,
            RemainingAmount = 7000,
            Status = PayableStatus.Partial,
            IsDeleted = false,
            Details =
            [
                new PayableDetail
                {
                    Id = 1,
                    PayableId = 1,
                    TransactionId = 92,
                    Amount = 3000m,
                    PaymentDate = DateTime.UtcNow
                }
            ]
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 2000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 92
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*已关联到当前应付记录*");
    }

    [Fact]
    public async Task PayPaymentAsync_WhenUniqueIndexRejectsDuplicateBinding_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 3000,
            RemainingAmount = 7000,
            Status = PayableStatus.Partial,
            IsDeleted = false,
            ProjectId = 1,
            SupplierId = 2,
            Project = new Project { Id = 1, Name = "项目1" },
            Supplier = new Supplier { Id = 2, Name = "供应商1" },
            Details = []
        };

        var transaction = new Transaction
        {
            Id = 92,
            TransactionType = TransactionType.Expense,
            Amount = 8000m,
            ProjectId = 1,
            SupplierId = 2,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        UnitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "duplicate settlement binding",
                new Exception("duplicate key value violates unique constraint \"ux_payable_details_payable_transaction\"")));

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 2000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 92
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*已关联到当前应付记录*");

        UnitOfWorkMock.Verify(u => u.ClearChangeTracker(), Times.Once);
    }

    [Fact]
    public async Task PayPaymentAsync_WhenTransactionLinkedAmountWouldExceedTransactionAmount_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 90,
                    Amount = 7000m,
                    TransactionType = TransactionType.Expense,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "璐︽埛", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<PayableDetail>
            {
                new() { Id = 1, PayableId = 30, TransactionId = 90, Amount = 3000m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 90
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PayPaymentAsync_WhenCounterpartyTypeDoesNotMatch_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            SupplierId = 5,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 91,
                    Amount = 5000m,
                    TransactionType = TransactionType.Expense,
                    CustomerId = 9,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "账户", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 3000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 91
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*对手方类型*");
    }

    [Fact(Skip = "旧规则：未关联项目的交易不再被拒绝，改由新测试覆盖允许并回填上下文")]
    public async Task PayPaymentAsync_WhenTransactionContextIsBlank_ShouldBackfillProjectAndSupplier()
    {
        var payable = new Payable
        {
            Id = 1,
            ProjectId = 7,
            SupplierId = 5,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 92,
                    Amount = 5000m,
                    TransactionType = TransactionType.Expense,
                    SupplierId = 5,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "账户", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 3000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 92
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*未关联项目*");
    }

    [Fact]
    public async Task PayPaymentAsync_WhenProjectAndSupplierAreUnlinkedOnTransaction_ShouldAllowAndBackfillContext()
    {
        var payable = new Payable
        {
            Id = 1,
            ProjectId = 7,
            SupplierId = 5,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        var transaction = new Transaction
        {
            Id = 192,
            Amount = 5000m,
            TransactionType = TransactionType.Expense,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "账户", AccountType = AccountType.Bank },
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);

        await _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 3000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 192
        });

        transaction.ProjectId.Should().Be(7);
        transaction.SupplierId.Should().Be(5);
        payable.PaidAmount.Should().Be(3000);
        payable.RemainingAmount.Should().Be(7000);
    }

    [Fact]
    public async Task PayPaymentAsync_WhenPersonIdMismatch_ShouldAllowWithoutException()
    {
        var payable = new Payable
        {
            Id = 1,
            PersonId = 10,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        var transaction = new Transaction
        {
            Id = 200,
            Amount = 5000m,
            TransactionType = TransactionType.Expense,
            PersonId = 20, // 不同的人员 ID
            AccountId = 1,
            Account = new Account { Id = 1, Name = "账户", AccountType = AccountType.Bank },
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);

        await _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 3000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 200
        });

        // 人员不一致不阻止提交，交易的人员 ID 保持原值不被覆盖
        transaction.PersonId.Should().Be(20);
        payable.PaidAmount.Should().Be(3000);
        payable.RemainingAmount.Should().Be(7000);
    }

    [Fact]
    public async Task PayPaymentAsync_WhenTransactionHasNoPersonId_ShouldBackfillFromPayable()
    {
        var payable = new Payable
        {
            Id = 1,
            PersonId = 10,
            TotalAmount = 10000,
            PaidAmount = 0,
            RemainingAmount = 10000,
            Status = PayableStatus.Pending,
            IsDeleted = false
        };

        var transaction = new Transaction
        {
            Id = 201,
            Amount = 5000m,
            TransactionType = TransactionType.Expense,
            PersonId = null, // 交易无人员
            AccountId = 1,
            Account = new Account { Id = 1, Name = "账户", AccountType = AccountType.Bank },
            IsDeleted = false
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PayableDetail>()))
            .ReturnsAsync((PayableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);

        await _service.PayPaymentAsync(1, new PayPaymentRequest
        {
            Amount = 3000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 201
        });

        // 交易无人员时，应从应付款回填
        transaction.PersonId.Should().Be(10);
        payable.PaidAmount.Should().Be(3000);
    }

    [Fact]
    public async Task DeleteAsync_WithPaidAmount_ShouldThrowValidationException()
    {
        var payable = new Payable
        {
            Id = 1,
            TotalAmount = 10000,
            PaidAmount = 5000,
            RemainingAmount = 5000,
            Status = PayableStatus.Partial
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable> { payable }.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task GetTrendAsync_WithCustomDateRange_ShouldReturnCorrectMonths()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 3, 31);
        var payables = new List<Payable>
        {
            new() { Id = 1, TotalAmount = 5000, CreatedAt = new DateTime(2026, 1, 10) },
            new() { Id = 2, TotalAmount = 8000, CreatedAt = new DateTime(2026, 2, 20) },
            new() { Id = 3, TotalAmount = 12000, CreatedAt = new DateTime(2026, 3, 1) }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(payables.AsQueryable().BuildMock().Object);

        var result = await _service.GetTrendAsync(start, end);

        result.Months.Should().ContainInOrder("2026-01", "2026-02", "2026-03");
        result.Amounts.Should().ContainInOrder(5000, 8000, 12000);
    }

    [Fact]
    public async Task GetAgingAsync_ShouldCategorizeByDueDateBuckets()
    {
        var today = DateTime.UtcNow.Date;
        var payables = new List<Payable>
        {
            new() { Id = 1, RemainingAmount = 10000, Status = PayableStatus.Pending, DueDate = today.AddDays(5) },
            new() { Id = 2, RemainingAmount = 20000, Status = PayableStatus.Pending, DueDate = today.AddDays(-15) },
            new() { Id = 3, RemainingAmount = 30000, Status = PayableStatus.Pending, DueDate = today.AddDays(-45) },
            new() { Id = 4, RemainingAmount = 40000, Status = PayableStatus.Pending, DueDate = today.AddDays(-75) },
            new() { Id = 5, RemainingAmount = 50000, Status = PayableStatus.Pending, DueDate = today.AddDays(-100) }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(payables.AsQueryable().BuildMock().Object);

        var result = await _service.GetAgingAsync();

        result.Amounts.Should().ContainInOrder(10000, 20000, 30000, 40000, 50000);
    }

    [Fact]
    public async Task GetPayableSummaryAsync_ShouldReturnCorrectSummary()
    {
        // Arrange
        // 构造混合状态的应付款数据：2 条 Pending、1 条 Partial、1 条 Settled，其中 2 条已逾期（未结清）
        var now = DateTime.UtcNow;
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1,
                TotalAmount  = 10000,
                PaidAmount   = 0,
                RemainingAmount = 10000,
                Status  = PayableStatus.Pending,
                DueDate = now.AddDays(-5)   // 已逾期
            },
            new()
            {
                Id = 2,
                TotalAmount  = 20000,
                PaidAmount   = 0,
                RemainingAmount = 20000,
                Status  = PayableStatus.Pending,
                DueDate = now.AddDays(30)   // 未到期
            },
            new()
            {
                Id = 3,
                TotalAmount  = 30000,
                PaidAmount   = 15000,
                RemainingAmount = 15000,
                Status  = PayableStatus.Partial,
                DueDate = now.AddDays(-10)  // 已逾期
            },
            new()
            {
                Id = 4,
                TotalAmount  = 40000,
                PaidAmount   = 40000,
                RemainingAmount = 0,
                Status  = PayableStatus.Settled,
                DueDate = now.AddDays(-1)   // 已结清，不计入逾期
            }
        };

        _payableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        // Act — Admin 全权限，ApplyPermissionFilter 不做过滤，全部 4 条均参与聚合
        var result = await _service.GetPayableSummaryAsync();

        // Assert：金额聚合
        result.TotalPayable.Should().Be(100000);    // 10000+20000+30000+40000
        result.TotalPaid.Should().Be(55000);        // 0+0+15000+40000
        result.TotalRemaining.Should().Be(45000);   // 10000+20000+15000+0

        // Assert：状态计数
        result.PendingCount.Should().Be(2);
        result.PartialCount.Should().Be(1);
        result.SettledCount.Should().Be(1);

        // Assert：逾期计数（未结清 && DueDate < now）—— Id=1 和 Id=3
        result.OverdueCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPayableSummaryAsync_ShouldNotTreatDueTodayAsOverdue()
    {
        var today = DateTime.UtcNow.Date;
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1,
                TotalAmount = 10000,
                PaidAmount = 0,
                RemainingAmount = 10000,
                Status = PayableStatus.Pending,
                DueDate = today
            }
        };

        _payableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        var result = await _service.GetPayableSummaryAsync();

        result.OverdueCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPayableSummaryAsync_WithNoData_ShouldReturnZeros()
    {
        // Arrange — 空数据集
        _payableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(new List<Payable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetPayableSummaryAsync();

        // Assert：所有字段应为 0
        result.TotalPayable.Should().Be(0);
        result.TotalPaid.Should().Be(0);
        result.TotalRemaining.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.PartialCount.Should().Be(0);
        result.SettledCount.Should().Be(0);
        result.OverdueCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPayableSummaryAsync_WithViewerPermission_ShouldOnlyAggregateOwnedRecords()
    {
        // Arrange — 模拟 Viewer（UserId=2）只能看到自己创建的记录
        // IDataPermissionService 过滤：只保留 CreatedBy == 2 的数据
        var viewerDataPermissionService = new CreatedByFilterDataPermissionService(userId: 2);
        var viewerCurrentUserService = new Mock<ICurrentUserService>();
        viewerCurrentUserService.Setup(x => x.UserId).Returns(2L);
        viewerCurrentUserService.Setup(x => x.Role).Returns(UserRole.Viewer);
        viewerCurrentUserService.Setup(x => x.IsAdmin).Returns(false);
        viewerCurrentUserService.Setup(x => x.IsViewer).Returns(true);
        var viewerBindingService = new SettlementTransactionBindingService(
            _transactionRepositoryMock.Object,
            _receivableDetailRepositoryMock.Object,
            _detailRepositoryMock.Object,
            viewerCurrentUserService.Object,
            viewerDataPermissionService);

        var viewerTransactionAllocationHelper = new TransactionAllocationHelper(
            _transactionRepositoryMock.Object,
            UnitOfWorkMock.Object,
            new Mock<ILogger<TransactionAllocationHelper>>().Object);

        var viewerService = new PayableService(
            _payableRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _personRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _payableTypeRepositoryMock.Object,
            Mapper,
            _loggerMock.Object,
            AuditLogServiceMock.Object,
            viewerCurrentUserService.Object,
            viewerDataPermissionService,
            UnitOfWorkMock.Object,
            viewerBindingService,
            viewerTransactionAllocationHelper,
            _recalculationServiceMock.Object
        );

        // CreatedBy=2 的记录：Id=1（10000），CreatedBy=1 的记录：Id=2（20000，应被过滤掉）
        var payables = new List<Payable>
        {
            new() { Id = 1, TotalAmount = 10000, PaidAmount = 0, RemainingAmount = 10000,
                    Status = PayableStatus.Pending, CreatedBy = 2 },
            new() { Id = 2, TotalAmount = 20000, PaidAmount = 0, RemainingAmount = 20000,
                    Status = PayableStatus.Pending, CreatedBy = 1 }
        };

        _payableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        // Act
        var result = await viewerService.GetPayableSummaryAsync();

        // Assert：只聚合 CreatedBy=2 的 Id=1 记录
        result.TotalPayable.Should().Be(10000);
        result.PendingCount.Should().Be(1);
        result.PartialCount.Should().Be(0);
        result.SettledCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidPayableType_ShouldThrowNotFoundException()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "供应商1" };
        var request = new CreatePayableRequest
        {
            SupplierId = 1,
            TotalAmount = 10000,
            PayableTypeId = 999 // 不存在的业务类型
        };

        _supplierRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
        _payableTypeRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((PayableType?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInactivePayableType_ShouldThrowNotFoundException()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "供应商1" };
        var inactivePayableType = new PayableType
        {
            Id = 1,
            Name = "已停用类型",
            IsActive = false
        };

        var request = new CreatePayableRequest
        {
            SupplierId = 1,
            TotalAmount = 10000,
            PayableTypeId = 1
        };

        _supplierRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
        _payableTypeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inactivePayableType);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    #region GetBy Entity Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnPayablesForCustomer()
    {
        // Arrange
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1, CustomerId = 1, ProjectId = 10, TotalAmount = 10000,
                PaidAmount = 0, RemainingAmount = 10000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Customer = new Customer { Id = 1, Name = "客户1" }
            },
            new()
            {
                Id = 2, CustomerId = 1, ProjectId = 20, TotalAmount = 20000,
                PaidAmount = 5000, RemainingAmount = 15000, Status = PayableStatus.Partial,
                DueDate = DateTime.UtcNow.Date.AddDays(60),
                Customer = new Customer { Id = 1, Name = "客户1" }
            },
            new()
            {
                Id = 3, CustomerId = 2, ProjectId = 30, TotalAmount = 15000,
                PaidAmount = 0, RemainingAmount = 15000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(45),
                Customer = new Customer { Id = 2, Name = "客户2" }
            }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByCustomerIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(p => p.CustomerId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetBySupplierIdAsync_ShouldReturnPayablesForSupplier()
    {
        // Arrange
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1, SupplierId = 1, ProjectId = 10, TotalAmount = 30000,
                PaidAmount = 0, RemainingAmount = 30000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(20),
                Supplier = new Supplier { Id = 1, Name = "供应商1" }
            },
            new()
            {
                Id = 2, SupplierId = 2, ProjectId = 20, TotalAmount = 50000,
                PaidAmount = 0, RemainingAmount = 50000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(40),
                Supplier = new Supplier { Id = 2, Name = "供应商2" }
            }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetBySupplierIdAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].SupplierId.Should().Be(1);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnPayablesForPerson()
    {
        // Arrange
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1, PersonId = 3, ProjectId = 10, TotalAmount = 8000,
                PaidAmount = 2000, RemainingAmount = 6000, Status = PayableStatus.Partial,
                DueDate = DateTime.UtcNow.Date.AddDays(15),
                Person = new Person { Id = 3, Name = "张三", PersonType = PersonType.Employee }
            },
            new()
            {
                Id = 2, PersonId = 4, ProjectId = 20, TotalAmount = 12000,
                PaidAmount = 0, RemainingAmount = 12000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(25),
                Person = new Person { Id = 4, Name = "李四", PersonType = PersonType.Employee }
            }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByPersonIdAsync(3);

        // Assert
        result.Should().HaveCount(1);
        result[0].PersonId.Should().Be(3);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ShouldReturnPayablesForProject()
    {
        // Arrange
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1, ProjectId = 10, SupplierId = 1, TotalAmount = 25000,
                PaidAmount = 0, RemainingAmount = 25000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Supplier = new Supplier { Id = 1, Name = "供应商1" }
            },
            new()
            {
                Id = 2, ProjectId = 10, SupplierId = 2, TotalAmount = 35000,
                PaidAmount = 10000, RemainingAmount = 25000, Status = PayableStatus.Partial,
                DueDate = DateTime.UtcNow.Date.AddDays(60),
                Supplier = new Supplier { Id = 2, Name = "供应商2" }
            },
            new()
            {
                Id = 3, ProjectId = 20, SupplierId = 1, TotalAmount = 18000,
                PaidAmount = 0, RemainingAmount = 18000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(45),
                Supplier = new Supplier { Id = 1, Name = "供应商1" }
            }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByProjectIdAsync(10);

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.ProjectId == 10).Should().BeTrue();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var payables = new List<Payable>
        {
            new()
            {
                Id = 1, ProjectId = 10, SupplierId = 1, TotalAmount = 25000,
                PaidAmount = 0, RemainingAmount = 25000, Status = PayableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Supplier = new Supplier { Id = 1, Name = "供应商1" }
            }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByProjectIdAsync(999);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetStatisticsAsync

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectCounts()
    {
        var payables = new List<Payable>
        {
            new() { Id = 1, Status = PayableStatus.Pending, TotalAmount = 1000, PaidAmount = 0, RemainingAmount = 1000, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Status = PayableStatus.Pending, TotalAmount = 2000, PaidAmount = 0, RemainingAmount = 2000, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Status = PayableStatus.Partial, TotalAmount = 3000, PaidAmount = 1500, RemainingAmount = 1500, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Status = PayableStatus.Settled, TotalAmount = 4000, PaidAmount = 4000, RemainingAmount = 0, CreatedAt = DateTime.UtcNow }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.TotalCount.Should().Be(4);
        result.PendingCount.Should().Be(2);
        result.PartialCount.Should().Be(1);
        result.SettledCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCalculateAmountsCorrectly()
    {
        var payables = new List<Payable>
        {
            new() { Id = 1, Status = PayableStatus.Pending, TotalAmount = 10000, PaidAmount = 0, RemainingAmount = 10000, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Status = PayableStatus.Partial, TotalAmount = 20000, PaidAmount = 8000, RemainingAmount = 12000, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Status = PayableStatus.Settled, TotalAmount = 5000, PaidAmount = 5000, RemainingAmount = 0, CreatedAt = DateTime.UtcNow }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.TotalAmount.Should().Be(35000);
        result.PaidAmount.Should().Be(13000);
        result.RemainingAmount.Should().Be(22000);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCalculateOverdueAmount()
    {
        var pastDate = DateTime.UtcNow.Date.AddDays(-10);
        var futureDate = DateTime.UtcNow.Date.AddDays(10);

        var payables = new List<Payable>
        {
            // Overdue: past due date, not settled, remaining > 0
            new() { Id = 1, Status = PayableStatus.Pending, TotalAmount = 5000, PaidAmount = 0, RemainingAmount = 5000, DueDate = pastDate, CreatedAt = DateTime.UtcNow },
            // Overdue: past due date, partial, remaining > 0
            new() { Id = 2, Status = PayableStatus.Partial, TotalAmount = 8000, PaidAmount = 3000, RemainingAmount = 5000, DueDate = pastDate, CreatedAt = DateTime.UtcNow },
            // NOT overdue: settled even though past due
            new() { Id = 3, Status = PayableStatus.Settled, TotalAmount = 3000, PaidAmount = 3000, RemainingAmount = 0, DueDate = pastDate, CreatedAt = DateTime.UtcNow },
            // NOT overdue: future due date
            new() { Id = 4, Status = PayableStatus.Pending, TotalAmount = 7000, PaidAmount = 0, RemainingAmount = 7000, DueDate = futureDate, CreatedAt = DateTime.UtcNow },
            // NOT overdue: no due date
            new() { Id = 5, Status = PayableStatus.Pending, TotalAmount = 2000, PaidAmount = 0, RemainingAmount = 2000, DueDate = null, CreatedAt = DateTime.UtcNow }
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(payables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.OverdueAmount.Should().Be(10000); // 5000 + 5000
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyData_ShouldReturnZeros()
    {
        _payableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Payable>().AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.TotalCount.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.PartialCount.Should().Be(0);
        result.SettledCount.Should().Be(0);
        result.TotalAmount.Should().Be(0);
        result.PaidAmount.Should().Be(0);
        result.RemainingAmount.Should().Be(0);
        result.OverdueAmount.Should().Be(0);
    }

    #endregion
}
