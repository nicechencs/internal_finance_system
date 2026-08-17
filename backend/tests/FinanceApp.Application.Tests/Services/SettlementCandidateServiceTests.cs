using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class SettlementCandidateServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock = new();
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock = new();
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock = new();
    private readonly SettlementCandidateService _service;

    public SettlementCandidateServiceTests()
    {
        _service = new SettlementCandidateService(
            _transactionRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableRepositoryMock.Object,
            Mapper,
            new Mock<ILogger<SettlementCandidateService>>().Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());
    }

    [Fact]
    public async Task GetAvailableReceivablesForTransactionAsync_ShouldFilterConflictsAndBoundDocuments()
    {
        var transaction = new Transaction
        {
            Id = 10,
            TransactionType = TransactionType.Income,
            CustomerId = 21,
            ProjectId = 31,
            Amount = 1000m
        };
        var projectA = new Project { Id = 31, Name = "项目A" };
        var projectB = new Project { Id = 32, Name = "项目B" };
        var customer = new Customer { Id = 21, Name = "客户A" };

        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 1,
                ProjectId = 31,
                Project = projectA,
                CustomerId = 21,
                Customer = customer,
                RemainingAmount = 400,
                Status = ReceivableStatus.Pending,
                DueDate = new DateTime(2026, 9, 1)
            },
            new()
            {
                Id = 2,
                ProjectId = 32,
                Project = projectB,
                CustomerId = 99,
                RemainingAmount = 400,
                Status = ReceivableStatus.Pending
            },
            new()
            {
                Id = 3,
                ProjectId = 31,
                Project = projectA,
                CustomerId = 21,
                Customer = customer,
                RemainingAmount = 200,
                Status = ReceivableStatus.Pending,
                Details = new List<ReceivableDetail> { new() { TransactionId = 10, Amount = 50 } }
            },
            new()
            {
                Id = 4,
                ProjectId = 31,
                Project = projectA,
                RemainingAmount = 0,
                Status = ReceivableStatus.Settled
            }
        };

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        var result = await _service.GetAvailableReceivablesForTransactionAsync(10);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
        result[0].RemainingAmount.Should().Be(400);
    }

    [Fact]
    public async Task GetAvailableReceivablesForTransactionAsync_ShouldRejectExpenseTransaction()
    {
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>
            {
                new() { Id = 11, TransactionType = TransactionType.Expense, Amount = 100 }
            }.AsQueryable().BuildMock().Object);

        var act = () => _service.GetAvailableReceivablesForTransactionAsync(11);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*收入交易*");
    }

    [Fact]
    public async Task GetAvailablePayablesForTransactionAsync_ShouldReturnCompatibleOpenPayables()
    {
        var transaction = new Transaction
        {
            Id = 20,
            TransactionType = TransactionType.Expense,
            SupplierId = 41,
            Amount = 500m
        };
        var supplier = new Supplier { Id = 41, Name = "供应商A" };
        var payables = new List<Payable>
        {
            new()
            {
                Id = 7,
                SupplierId = 41,
                Supplier = supplier,
                RemainingAmount = 120,
                Status = PayableStatus.Partial,
                DueDate = new DateTime(2026, 8, 20)
            },
            new()
            {
                Id = 8,
                CustomerId = 21,
                RemainingAmount = 80,
                Status = PayableStatus.Pending
            }
        };

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction> { transaction }.AsQueryable().BuildMock().Object);
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        var result = await _service.GetAvailablePayablesForTransactionAsync(20);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(7);
    }
}
