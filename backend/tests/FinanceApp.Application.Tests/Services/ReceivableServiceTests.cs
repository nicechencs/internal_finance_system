using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
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

public class ReceivableServiceTests : TestBase
{
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IRepository<ReceivableDetail>> _detailRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IRepository<Customer>> _customerRepositoryMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepositoryMock;
    private readonly Mock<IRepository<Person>> _personRepositoryMock;
    private readonly Mock<IRepository<PayableDetail>> _payableDetailRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IProjectFinancialRecalculationService> _recalculationServiceMock;
    private readonly Mock<ILogger<ReceivableService>> _loggerMock;
    private readonly ReceivableService _service;

    public ReceivableServiceTests()
    {
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _detailRepositoryMock = new Mock<IRepository<ReceivableDetail>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _customerRepositoryMock = new Mock<IRepository<Customer>>();
        _supplierRepositoryMock = new Mock<IRepository<Supplier>>();
        _personRepositoryMock = new Mock<IRepository<Person>>();
        _payableDetailRepositoryMock = new Mock<IRepository<PayableDetail>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _recalculationServiceMock = new Mock<IProjectFinancialRecalculationService>();
        _loggerMock = new Mock<ILogger<ReceivableService>>();
        _detailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<ReceivableDetail>().AsQueryable().BuildMock().Object);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>().AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<PayableDetail>().AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);
        var bindingService = new SettlementTransactionBindingService(
            _transactionRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        var transactionAllocationHelper = new TransactionAllocationHelper(
            _transactionRepositoryMock.Object,
            UnitOfWorkMock.Object,
            new Mock<ILogger<TransactionAllocationHelper>>().Object);

        _service = new ReceivableService(
            _receivableRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _personRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
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
        var receivables = new List<Receivable>
        {
            new() { Id = 1, TotalAmount = 50000, CreatedAt = DateTime.UtcNow, Project = new Project { Id = 1, Name = "项目1" } },
            new() { Id = 2, TotalAmount = 80000, CreatedAt = DateTime.UtcNow.AddDays(-1), Project = new Project { Id = 2, Name = "项目2" } }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);

        var result = await _service.GetPagedAsync(new PageRequest { Page = 1, PageSize = 10 });

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithCustomerCounterparty_ShouldCreateReceivable()
    {
        var project = new Project { Id = 1, Name = "项目1" };
        var customer = new Customer { Id = 1, Name = "客户1" };
        var request = new CreateReceivableRequest { ProjectId = 1, CustomerId = 1, TotalAmount = 50000 };
        Receivable? created = null;

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _receivableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Receivable>()))
            .ReturnsAsync((Receivable r) =>
            {
                r.Id = 1;
                r.Project = project;
                r.Customer = customer;
                created = r;
                return r;
            });
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Receivable> { created! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        created.Should().NotBeNull();
        created!.CustomerId.Should().Be(1);
        created.SupplierId.Should().BeNull();
        created.PersonId.Should().BeNull();
        result.CustomerName.Should().Be("客户1");
    }

    [Fact]
    public async Task CreateAsync_WithSupplierCounterparty_ShouldCreateReceivable()
    {
        var project = new Project { Id = 1, Name = "项目1" };
        var supplier = new Supplier { Id = 2, Name = "供应商1" };
        var request = new CreateReceivableRequest { ProjectId = 1, SupplierId = 2, TotalAmount = 50000 };
        Receivable? created = null;

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
        _supplierRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(supplier);
        _receivableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Receivable>()))
            .ReturnsAsync((Receivable r) =>
            {
                r.Id = 1;
                r.Project = project;
                r.Supplier = supplier;
                created = r;
                return r;
            });
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Receivable> { created! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        created.Should().NotBeNull();
        created!.CustomerId.Should().BeNull();
        created.SupplierId.Should().Be(2);
        created.PersonId.Should().BeNull();
        result.SupplierName.Should().Be("供应商1");
    }

    [Fact]
    public async Task CreateAsync_WithPersonCounterparty_ShouldCreateReceivable()
    {
        var project = new Project { Id = 1, Name = "项目1" };
        var person = new Person { Id = 3, Name = "张三", PersonType = PersonType.Employee };
        var request = new CreateReceivableRequest { ProjectId = 1, PersonId = 3, TotalAmount = 50000 };
        Receivable? created = null;

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
        _personRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(person);
        _receivableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Receivable>()))
            .ReturnsAsync((Receivable r) =>
            {
                r.Id = 1;
                r.Project = project;
                r.Person = person;
                created = r;
                return r;
            });
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Receivable> { created! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        created.Should().NotBeNull();
        created!.CustomerId.Should().BeNull();
        created.SupplierId.Should().BeNull();
        created.PersonId.Should().Be(3);
        result.PersonName.Should().Be("张三");
    }

    [Fact]
    public async Task CreateAsync_WithoutCounterparty_ShouldThrowValidationException()
    {
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1, Name = "项目1" });

        var act = () => _service.CreateAsync(new CreateReceivableRequest { ProjectId = 1, TotalAmount = 50000 });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须选择一个对方*");
    }

    [Fact]
    public async Task CreateAsync_WithMultipleCounterparties_ShouldThrowValidationException()
    {
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1, Name = "项目1" });

        var act = () => _service.CreateAsync(new CreateReceivableRequest
        {
            ProjectId = 1,
            CustomerId = 1,
            SupplierId = 2,
            TotalAmount = 50000
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*只能选择一个对方*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowSwitchingToSupplierCounterparty()
    {
        var project = new Project { Id = 1, Name = "项目1" };
        var supplier = new Supplier { Id = 2, Name = "供应商1" };
        var receivable = new Receivable
        {
            Id = 1,
            ProjectId = 1,
            CustomerId = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            Project = project,
            Customer = new Customer { Id = 1, Name = "客户1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(receivable);
        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
        _supplierRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(supplier);

        var result = await _service.UpdateAsync(1, new UpdateReceivableRequest
        {
            ProjectId = 1,
            SupplierId = 2,
            TotalAmount = 50000
        });

        receivable.CustomerId.Should().BeNull();
        receivable.SupplierId.Should().Be(2);
        receivable.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task ReceivePaymentAsync_WithNullTransactionId_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 1, Name = "项目1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest { Amount = 20000, PaymentDate = DateTime.UtcNow });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须关联交易记录*");
    }

    [Fact]
    public async Task ReceivePaymentAsync_WithZeroTransactionId_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 1, Name = "椤圭洰1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 20000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 0
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须关联交易记录*");
    }

    [Fact]
    public async Task ReceivePaymentAsync_WithNegativeTransactionId_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 1, Name = "项目1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 20000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = -1
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*必须关联交易记录*");
    }

    [Fact]
    public async Task ReceivePaymentAsync_WithValidRequest_ShouldUpdateReceivable()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            ProjectId = 1,
            Project = new Project { Id = 1, Name = "项目1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1, Name = "项目1", ReceivedAmount = 0, ReceivableAmount = 50000 });
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>())).ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 98,
                    Amount = 20000m,
                    TransactionType = TransactionType.Income,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                    ProjectId = 1,
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        await _service.ReceivePaymentAsync(1, new ReceivePaymentRequest { Amount = 20000, PaymentDate = DateTime.UtcNow, TransactionId = 98 });

        receivable.ReceivedAmount.Should().Be(20000);
        receivable.RemainingAmount.Should().Be(30000);
        receivable.Status.Should().Be(ReceivableStatus.Partial);
    }

    [Fact]
    public async Task ReceivePaymentAsync_WithExpenseTransactionId_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 1, Name = "椤圭洰1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>()))
            .ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 99,
                    Amount = 20000m,
                    TransactionType = TransactionType.Expense,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "璐︽埛", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 20000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 99
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenTransactionAlreadyLinkedToPayable_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 1, Name = "椤圭洰1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>()))
            .ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 100,
                    Amount = 40000m,
                    TransactionType = TransactionType.Income,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "璐︽埛", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<PayableDetail>
            {
                new() { Id = 1, PayableId = 20, TransactionId = 100, Amount = 5000m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 20000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 100
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenTransactionAlreadyLinkedToSameReceivable_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 10000,
            RemainingAmount = 40000,
            Status = ReceivableStatus.Partial,
            IsDeleted = false,
            ProjectId = 1,
            Project = new Project { Id = 1, Name = "项目1" },
            Details =
            [
                new ReceivableDetail
                {
                    Id = 1,
                    ReceivableId = 1,
                    TransactionId = 102,
                    Amount = 10000m,
                    PaymentDate = DateTime.UtcNow
                }
            ]
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 102
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*已关联到当前应收记录*");
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenUniqueIndexRejectsDuplicateBinding_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 10000,
            RemainingAmount = 40000,
            Status = ReceivableStatus.Partial,
            IsDeleted = false,
            ProjectId = 1,
            CustomerId = 2,
            Project = new Project { Id = 1, Name = "项目1" },
            Customer = new Customer { Id = 2, Name = "客户1" },
            Details = []
        };

        var transaction = new Transaction
        {
            Id = 102,
            TransactionType = TransactionType.Income,
            Amount = 15000m,
            ProjectId = 1,
            CustomerId = 2,
            IsDeleted = false
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        UnitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "duplicate settlement binding",
                new Exception("duplicate key value violates unique constraint \"ux_receivable_details_receivable_transaction\"")));

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 5000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 102
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*已关联到当前应收记录*");

        UnitOfWorkMock.Verify(u => u.ClearChangeTracker(), Times.Once);
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenTransactionLinkedAmountWouldExceedTransactionAmount_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 1, Name = "椤圭洰1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>()))
            .ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 101,
                    Amount = 25000m,
                    TransactionType = TransactionType.Income,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "璐︽埛", AccountType = AccountType.Bank },
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);
        _detailRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<ReceivableDetail>
            {
                new() { Id = 1, ReceivableId = 30, TransactionId = 101, Amount = 10000m, PaymentDate = DateTime.UtcNow }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 20000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 101
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenTransactionContextIsBlank_ShouldBackfillProjectAndCustomer()
    {
        var receivable = new Receivable
        {
            Id = 1,
            ProjectId = 7,
            CustomerId = 5,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            Project = new Project { Id = 7, Name = "项目1" }
        };

        var transaction = new Transaction
        {
            Id = 102,
            Amount = 18000m,
            TransactionType = TransactionType.Income,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
            IsDeleted = false
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(7))
            .ReturnsAsync(new Project { Id = 7, Name = "项目1", ReceivedAmount = 0, ReceivableAmount = 50000 });
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>()))
            .ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);

        await _service.ReceivePaymentAsync(1, new ReceivePaymentRequest
        {
            Amount = 12000,
            PaymentDate = DateTime.UtcNow,
            TransactionId = 102
        });

        transaction.ProjectId.Should().Be(7);
        transaction.CustomerId.Should().Be(5);
        receivable.ReceivedAmount.Should().Be(12000);
        receivable.RemainingAmount.Should().Be(38000);
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenPersonIdMismatch_ShouldAllowWithoutException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            PersonId = 10,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            ProjectId = 1,
            Project = new Project { Id = 1, Name = "项目1" }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1, Name = "项目1", ReceivedAmount = 0, ReceivableAmount = 50000 });
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>())).ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Id = 200,
                    Amount = 18000m,
                    TransactionType = TransactionType.Income,
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
                    ProjectId = 1,
                    PersonId = 20,
                    IsDeleted = false
                }
            }.AsQueryable().BuildMock().Object);

        await _service.ReceivePaymentAsync(1, new ReceivePaymentRequest { Amount = 12000, PaymentDate = DateTime.UtcNow, TransactionId = 200 });

        // 人员不一致不阻止提交，交易的人员 ID 保持原值不被覆盖
        receivable.ReceivedAmount.Should().Be(12000);
        receivable.RemainingAmount.Should().Be(38000);
    }

    [Fact]
    public async Task ReceivePaymentAsync_WhenTransactionHasNoPersonId_ShouldBackfillFromReceivable()
    {
        var receivable = new Receivable
        {
            Id = 1,
            PersonId = 10,
            TotalAmount = 50000,
            ReceivedAmount = 0,
            RemainingAmount = 50000,
            Status = ReceivableStatus.Pending,
            IsDeleted = false,
            ProjectId = 1,
            Project = new Project { Id = 1, Name = "项目1" }
        };

        var transaction = new Transaction
        {
            Id = 201,
            Amount = 18000m,
            TransactionType = TransactionType.Income,
            ProjectId = 1,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank },
            IsDeleted = false
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1, Name = "项目1", ReceivedAmount = 0, ReceivableAmount = 50000 });
        _detailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ReceivableDetail>())).ReturnsAsync((ReceivableDetail d) => d);
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);

        await _service.ReceivePaymentAsync(1, new ReceivePaymentRequest { Amount = 12000, PaymentDate = DateTime.UtcNow, TransactionId = 201 });

        // 交易无人员时，应从应收款回填
        transaction.PersonId.Should().Be(10);
        receivable.ReceivedAmount.Should().Be(12000);
    }

    [Fact]
    public async Task DeleteAsync_WithReceivedAmount_ShouldThrowValidationException()
    {
        var receivable = new Receivable
        {
            Id = 1,
            TotalAmount = 50000,
            ReceivedAmount = 20000,
            RemainingAmount = 30000,
            Status = ReceivableStatus.Partial
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task GetTrendAsync_WithCustomDateRange_ShouldReturnCorrectMonths()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 3, 31);
        var receivables = new List<Receivable>
        {
            new() { Id = 1, TotalAmount = 10000, CreatedAt = new DateTime(2026, 1, 15) },
            new() { Id = 2, TotalAmount = 20000, CreatedAt = new DateTime(2026, 2, 10) },
            new() { Id = 3, TotalAmount = 30000, CreatedAt = new DateTime(2026, 3, 5) }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);

        var result = await _service.GetTrendAsync(start, end);

        result.Months.Should().ContainInOrder("2026-01", "2026-02", "2026-03");
        result.Amounts.Should().ContainInOrder(10000, 20000, 30000);
    }

    [Fact]
    public async Task GetAgingAsync_ShouldCategorizeByDueDateBuckets()
    {
        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new() { Id = 1, RemainingAmount = 10000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(5) },
            new() { Id = 2, RemainingAmount = 20000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(-15) },
            new() { Id = 3, RemainingAmount = 30000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(-45) },
            new() { Id = 4, RemainingAmount = 40000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(-75) },
            new() { Id = 5, RemainingAmount = 50000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(-100) }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);

        var result = await _service.GetAgingAsync();

        result.Amounts.Should().ContainInOrder(10000, 20000, 30000, 40000, 50000);
    }

    [Fact]
    public async Task GetReceivableSummaryAsync_ShouldReturnCorrectSummary()
    {
        // Arrange
        // 构造混合状态的应收款数据：2 条 Pending、1 条 Partial、1 条 Settled，其中 2 条已逾期（未结清）
        var now = DateTime.UtcNow;
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                TotalAmount     = 50000,
                ReceivedAmount  = 0,
                RemainingAmount = 50000,
                Status  = ReceivableStatus.Pending,
                DueDate = now.AddDays(-3)   // 已逾期
            },
            new()
            {
                Id = 2,
                TotalAmount     = 80000,
                ReceivedAmount  = 0,
                RemainingAmount = 80000,
                Status  = ReceivableStatus.Pending,
                DueDate = now.AddDays(20)   // 未到期
            },
            new()
            {
                Id = 3,
                TotalAmount     = 60000,
                ReceivedAmount  = 30000,
                RemainingAmount = 30000,
                Status  = ReceivableStatus.Partial,
                DueDate = now.AddDays(-7)   // 已逾期
            },
            new()
            {
                Id = 4,
                TotalAmount     = 100000,
                ReceivedAmount  = 100000,
                RemainingAmount = 0,
                Status  = ReceivableStatus.Settled,
                DueDate = now.AddDays(-2)   // 已结清，不计入逾期
            }
        };

        _receivableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act — Admin 全权限，ApplyPermissionFilter 不做过滤，全部 4 条均参与聚合
        var result = await _service.GetReceivableSummaryAsync();

        // Assert：金额聚合
        result.TotalReceivable.Should().Be(290000);   // 50000+80000+60000+100000
        result.TotalReceived.Should().Be(130000);      // 0+0+30000+100000
        result.TotalRemaining.Should().Be(160000);     // 50000+80000+30000+0

        // Assert：状态计数
        result.PendingCount.Should().Be(2);
        result.PartialCount.Should().Be(1);
        result.SettledCount.Should().Be(1);

        // Assert：逾期计数（未结清 && DueDate < now）—— Id=1 和 Id=3
        result.OverdueCount.Should().Be(2);
    }

    [Fact]
    public async Task GetReceivableSummaryAsync_ShouldNotTreatDueTodayAsOverdue()
    {
        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                TotalAmount = 10000,
                ReceivedAmount = 0,
                RemainingAmount = 10000,
                Status = ReceivableStatus.Pending,
                DueDate = today
            }
        };

        _receivableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        var result = await _service.GetReceivableSummaryAsync();

        result.OverdueCount.Should().Be(0);
    }

    [Fact]
    public async Task GetReceivableSummaryAsync_WithNoData_ShouldReturnZeros()
    {
        // Arrange — 空数据集
        _receivableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetReceivableSummaryAsync();

        // Assert：所有字段应为 0
        result.TotalReceivable.Should().Be(0);
        result.TotalReceived.Should().Be(0);
        result.TotalRemaining.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.PartialCount.Should().Be(0);
        result.SettledCount.Should().Be(0);
        result.OverdueCount.Should().Be(0);
    }

    [Fact]
    public async Task GetReceivableSummaryAsync_WithViewerPermission_ShouldOnlyAggregateOwnedRecords()
    {
        // Arrange — 模拟 Viewer（UserId=2）只能看到自己创建的记录
        var viewerDataPermissionService = new CreatedByFilterDataPermissionService(userId: 2);
        var viewerCurrentUserService = new Mock<ICurrentUserService>();
        viewerCurrentUserService.Setup(x => x.UserId).Returns(2L);
        viewerCurrentUserService.Setup(x => x.Role).Returns(UserRole.Viewer);
        viewerCurrentUserService.Setup(x => x.IsAdmin).Returns(false);
        viewerCurrentUserService.Setup(x => x.IsViewer).Returns(true);
        var viewerBindingService = new SettlementTransactionBindingService(
            _transactionRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            viewerCurrentUserService.Object,
            viewerDataPermissionService);

        var viewerTransactionAllocationHelper = new TransactionAllocationHelper(
            _transactionRepositoryMock.Object,
            UnitOfWorkMock.Object,
            new Mock<ILogger<TransactionAllocationHelper>>().Object);

        var viewerService = new ReceivableService(
            _receivableRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _personRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
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

        // CreatedBy=2 的记录：Id=1（50000），CreatedBy=1 的记录：Id=2（80000，应被过滤掉）
        var receivables = new List<Receivable>
        {
            new() { Id = 1, TotalAmount = 50000, ReceivedAmount = 0, RemainingAmount = 50000,
                    Status = ReceivableStatus.Pending, CreatedBy = 2 },
            new() { Id = 2, TotalAmount = 80000, ReceivedAmount = 0, RemainingAmount = 80000,
                    Status = ReceivableStatus.Pending, CreatedBy = 1 }
        };

        _receivableRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await viewerService.GetReceivableSummaryAsync();

        // Assert：只聚合 CreatedBy=2 的 Id=1 记录
        result.TotalReceivable.Should().Be(50000);
        result.PendingCount.Should().Be(1);
        result.PartialCount.Should().Be(0);
        result.SettledCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ShouldReturnReceivablesForProject()
    {
        // Arrange
        var projectId = 1L;
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                ProjectId = projectId,
                TotalAmount = 50000,
                ReceivedAmount = 0,
                RemainingAmount = 50000,
                Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(30),
                Project = new Project { Id = projectId, Name = "项目1" },
                Customer = new Customer { Id = 1, Name = "客户1" },
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                ProjectId = projectId,
                TotalAmount = 30000,
                ReceivedAmount = 15000,
                RemainingAmount = 15000,
                Status = ReceivableStatus.Partial,
                DueDate = DateTime.UtcNow.AddDays(60),
                Project = new Project { Id = projectId, Name = "项目1" },
                Customer = new Customer { Id = 1, Name = "客户1" },
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                ProjectId = 2L,
                TotalAmount = 20000,
                ReceivedAmount = 0,
                RemainingAmount = 20000,
                Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(45),
                Project = new Project { Id = 2, Name = "项目2" },
                Customer = new Customer { Id = 2, Name = "客户2" },
                IsDeleted = false
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByProjectIdAsync(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(r => r.ProjectId == projectId).Should().BeTrue();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ShouldApplyPermissionFilter()
    {
        // Arrange
        var projectId = 1L;
        var viewerDataPermissionService = new CreatedByFilterDataPermissionService(userId: 2);
        var viewerCurrentUserService = new Mock<ICurrentUserService>();
        viewerCurrentUserService.Setup(x => x.UserId).Returns(2L);
        viewerCurrentUserService.Setup(x => x.Role).Returns(UserRole.Viewer);
        viewerCurrentUserService.Setup(x => x.IsAdmin).Returns(false);
        viewerCurrentUserService.Setup(x => x.IsViewer).Returns(true);

        var viewerBindingService = new SettlementTransactionBindingService(
            _transactionRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            viewerCurrentUserService.Object,
            viewerDataPermissionService);

        var viewerTransactionAllocationHelper = new TransactionAllocationHelper(
            _transactionRepositoryMock.Object,
            UnitOfWorkMock.Object,
            new Mock<ILogger<TransactionAllocationHelper>>().Object);

        var viewerService = new ReceivableService(
            _receivableRepositoryMock.Object,
            _detailRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _personRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
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

        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                ProjectId = projectId,
                CreatedBy = 2,
                DueDate = DateTime.UtcNow.AddDays(30),
                TotalAmount = 50000,
                ReceivedAmount = 0,
                RemainingAmount = 50000,
                Status = ReceivableStatus.Pending,
                Project = new Project { Id = projectId, Name = "椤圭洰1" },
                Customer = new Customer { Id = 1, Name = "瀹㈡埛1" }
            },
            new()
            {
                Id = 2,
                ProjectId = projectId,
                CreatedBy = 1,
                DueDate = DateTime.UtcNow.AddDays(60),
                TotalAmount = 30000,
                ReceivedAmount = 0,
                RemainingAmount = 30000,
                Status = ReceivableStatus.Pending,
                Project = new Project { Id = projectId, Name = "椤圭洰1" },
                Customer = new Customer { Id = 1, Name = "瀹㈡埛1" }
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await viewerService.GetByProjectIdAsync(projectId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ShouldPopulateTags()
    {
        // Arrange
        var projectId = 1L;
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                ProjectId = projectId,
                DueDate = DateTime.UtcNow.AddDays(30),
                TotalAmount = 50000,
                ReceivedAmount = 0,
                RemainingAmount = 50000,
                Status = ReceivableStatus.Pending,
                Project = new Project { Id = projectId, Name = "椤圭洰1" },
                Customer = new Customer { Id = 1, Name = "瀹㈡埛1" }
            },
            new()
            {
                Id = 2,
                ProjectId = projectId,
                DueDate = DateTime.UtcNow.AddDays(60),
                TotalAmount = 30000,
                ReceivedAmount = 0,
                RemainingAmount = 30000,
                Status = ReceivableStatus.Pending,
                Project = new Project { Id = projectId, Name = "椤圭洰1" },
                Customer = new Customer { Id = 1, Name = "瀹㈡埛1" }
            }
        };

        var tagBindings = new List<TagBinding>
        {
            new()
            {
                Id = 1,
                OwnerType = TagScope.Receivable,
                OwnerId = 1,
                TagId = 101,
                Tag = new Tag { Id = 101, Name = "tag-b", Color = "#222222", SortOrder = 2 }
            },
            new()
            {
                Id = 2,
                OwnerType = TagScope.Receivable,
                OwnerId = 1,
                TagId = 100,
                Tag = new Tag { Id = 100, Name = "tag-a", Color = "#111111", SortOrder = 1 }
            },
            new()
            {
                Id = 3,
                OwnerType = TagScope.Receivable,
                OwnerId = 2,
                TagId = 102,
                Tag = new Tag { Id = 102, Name = "tag-c", Color = "#333333", SortOrder = 1 }
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(tagBindings.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByProjectIdAsync(projectId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Tags.Select(tag => tag.TagId).Should().Equal(100, 101);
        result[1].Tags.Select(tag => tag.TagId).Should().Equal(102);
    }

    #region GetBy Entity Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnReceivablesForCustomer()
    {
        // Arrange
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1, CustomerId = 1, ProjectId = 10, TotalAmount = 50000,
                ReceivedAmount = 0, RemainingAmount = 50000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Project = new Project { Id = 10, Name = "项目1" },
                Customer = new Customer { Id = 1, Name = "客户1" }
            },
            new()
            {
                Id = 2, CustomerId = 1, ProjectId = 20, TotalAmount = 30000,
                ReceivedAmount = 15000, RemainingAmount = 15000, Status = ReceivableStatus.Partial,
                DueDate = DateTime.UtcNow.Date.AddDays(60),
                Project = new Project { Id = 20, Name = "项目2" },
                Customer = new Customer { Id = 1, Name = "客户1" }
            },
            new()
            {
                Id = 3, CustomerId = 2, ProjectId = 30, TotalAmount = 20000,
                ReceivedAmount = 0, RemainingAmount = 20000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(45),
                Project = new Project { Id = 30, Name = "项目3" },
                Customer = new Customer { Id = 2, Name = "客户2" }
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByCustomerIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(r => r.CustomerId == 1).Should().BeTrue();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1, CustomerId = 2, ProjectId = 10, TotalAmount = 50000,
                ReceivedAmount = 0, RemainingAmount = 50000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Project = new Project { Id = 10, Name = "项目1" },
                Customer = new Customer { Id = 2, Name = "客户2" }
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByCustomerIdAsync(999);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySupplierIdAsync_ShouldReturnReceivablesForSupplier()
    {
        // Arrange
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1, SupplierId = 1, ProjectId = 10, TotalAmount = 40000,
                ReceivedAmount = 0, RemainingAmount = 40000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(20),
                Project = new Project { Id = 10, Name = "项目1" },
                Supplier = new Supplier { Id = 1, Name = "供应商1" }
            },
            new()
            {
                Id = 2, SupplierId = 2, ProjectId = 20, TotalAmount = 60000,
                ReceivedAmount = 0, RemainingAmount = 60000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(40),
                Project = new Project { Id = 20, Name = "项目2" },
                Supplier = new Supplier { Id = 2, Name = "供应商2" }
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetBySupplierIdAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].SupplierId.Should().Be(1);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnReceivablesForPerson()
    {
        // Arrange
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1, PersonId = 3, ProjectId = 10, TotalAmount = 25000,
                ReceivedAmount = 10000, RemainingAmount = 15000, Status = ReceivableStatus.Partial,
                DueDate = DateTime.UtcNow.Date.AddDays(10),
                Project = new Project { Id = 10, Name = "项目1" },
                Person = new Person { Id = 3, Name = "张三", PersonType = PersonType.Employee }
            },
            new()
            {
                Id = 2, PersonId = 3, ProjectId = 20, TotalAmount = 15000,
                ReceivedAmount = 0, RemainingAmount = 15000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(50),
                Project = new Project { Id = 20, Name = "项目2" },
                Person = new Person { Id = 3, Name = "张三", PersonType = PersonType.Employee }
            },
            new()
            {
                Id = 3, PersonId = 4, ProjectId = 30, TotalAmount = 35000,
                ReceivedAmount = 0, RemainingAmount = 35000, Status = ReceivableStatus.Pending,
                DueDate = DateTime.UtcNow.Date.AddDays(15),
                Project = new Project { Id = 30, Name = "项目3" },
                Person = new Person { Id = 4, Name = "李四", PersonType = PersonType.Employee }
            }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetByPersonIdAsync(3);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.PersonId == 3).Should().BeTrue();
    }

    #endregion

    #region GetStatisticsAsync

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectCounts()
    {
        var receivables = new List<Receivable>
        {
            new() { Id = 1, Status = ReceivableStatus.Pending, TotalAmount = 1000, ReceivedAmount = 0, RemainingAmount = 1000, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Status = ReceivableStatus.Pending, TotalAmount = 2000, ReceivedAmount = 0, RemainingAmount = 2000, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Status = ReceivableStatus.Partial, TotalAmount = 3000, ReceivedAmount = 1500, RemainingAmount = 1500, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Status = ReceivableStatus.Settled, TotalAmount = 4000, ReceivedAmount = 4000, RemainingAmount = 0, CreatedAt = DateTime.UtcNow }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);
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
        var receivables = new List<Receivable>
        {
            new() { Id = 1, Status = ReceivableStatus.Pending, TotalAmount = 10000, ReceivedAmount = 0, RemainingAmount = 10000, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Status = ReceivableStatus.Partial, TotalAmount = 20000, ReceivedAmount = 8000, RemainingAmount = 12000, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Status = ReceivableStatus.Settled, TotalAmount = 5000, ReceivedAmount = 5000, RemainingAmount = 0, CreatedAt = DateTime.UtcNow }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.TotalAmount.Should().Be(35000);
        result.ReceivedAmount.Should().Be(13000);
        result.RemainingAmount.Should().Be(22000);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCalculateOverdueAmount()
    {
        var pastDate = DateTime.UtcNow.Date.AddDays(-10);
        var futureDate = DateTime.UtcNow.Date.AddDays(10);

        var receivables = new List<Receivable>
        {
            // Overdue: past due date, not settled, remaining > 0
            new() { Id = 1, Status = ReceivableStatus.Pending, TotalAmount = 5000, ReceivedAmount = 0, RemainingAmount = 5000, DueDate = pastDate, CreatedAt = DateTime.UtcNow },
            // Overdue: past due date, partial, remaining > 0
            new() { Id = 2, Status = ReceivableStatus.Partial, TotalAmount = 8000, ReceivedAmount = 3000, RemainingAmount = 5000, DueDate = pastDate, CreatedAt = DateTime.UtcNow },
            // NOT overdue: settled even though past due
            new() { Id = 3, Status = ReceivableStatus.Settled, TotalAmount = 3000, ReceivedAmount = 3000, RemainingAmount = 0, DueDate = pastDate, CreatedAt = DateTime.UtcNow },
            // NOT overdue: future due date
            new() { Id = 4, Status = ReceivableStatus.Pending, TotalAmount = 7000, ReceivedAmount = 0, RemainingAmount = 7000, DueDate = futureDate, CreatedAt = DateTime.UtcNow },
            // NOT overdue: no due date
            new() { Id = 5, Status = ReceivableStatus.Pending, TotalAmount = 2000, ReceivedAmount = 0, RemainingAmount = 2000, DueDate = null, CreatedAt = DateTime.UtcNow }
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.OverdueAmount.Should().Be(10000); // 5000 + 5000
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyData_ShouldReturnZeros()
    {
        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable>().AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync(new PageRequest());

        result.TotalCount.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.PartialCount.Should().Be(0);
        result.SettledCount.Should().Be(0);
        result.TotalAmount.Should().Be(0);
        result.ReceivedAmount.Should().Be(0);
        result.RemainingAmount.Should().Be(0);
        result.OverdueAmount.Should().Be(0);
    }

    #endregion
}
