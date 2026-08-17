using FluentAssertions;
using FinanceApp.Application.Modules.FinanceSettlement.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Tests.Services;

public class SettlementCandidateCompatibilityTests
{
    [Fact]
    public void IsReceivableCompatible_ShouldRejectCounterpartyMismatch()
    {
        var transaction = new Transaction { Id = 1, CustomerId = 11, TransactionType = TransactionType.Income };
        var receivable = new Receivable
        {
            Id = 2,
            CustomerId = 99,
            RemainingAmount = 100,
            Status = ReceivableStatus.Pending
        };

        SettlementCandidateCompatibility.IsReceivableCompatible(transaction, receivable).Should().BeFalse();
    }

    [Fact]
    public void IsReceivableCompatible_ShouldRejectCustomerSupplierMutex()
    {
        var transaction = new Transaction { Id = 1, CustomerId = 11, TransactionType = TransactionType.Income };
        var receivable = new Receivable
        {
            Id = 2,
            SupplierId = 22,
            RemainingAmount = 100,
            Status = ReceivableStatus.Pending
        };

        SettlementCandidateCompatibility.IsReceivableCompatible(transaction, receivable).Should().BeFalse();
    }

    [Fact]
    public void IsReceivableCompatible_ShouldRejectAlreadyBoundDocument()
    {
        var transaction = new Transaction { Id = 8, CustomerId = 11, TransactionType = TransactionType.Income };
        var receivable = new Receivable
        {
            Id = 2,
            CustomerId = 11,
            RemainingAmount = 100,
            Status = ReceivableStatus.Pending,
            Details = new List<ReceivableDetail>
            {
                new() { TransactionId = 8, Amount = 50 }
            }
        };

        SettlementCandidateCompatibility.IsReceivableCompatible(transaction, receivable).Should().BeFalse();
    }

    [Fact]
    public void IsReceivableCompatible_ShouldAllowSameCustomerAndOpenDocument()
    {
        var transaction = new Transaction { Id = 1, CustomerId = 11, ProjectId = 3, TransactionType = TransactionType.Income };
        var receivable = new Receivable
        {
            Id = 2,
            CustomerId = 11,
            ProjectId = 3,
            RemainingAmount = 100,
            Status = ReceivableStatus.Partial
        };

        SettlementCandidateCompatibility.IsReceivableCompatible(transaction, receivable).Should().BeTrue();
        SettlementCandidateCompatibility.IsPreferredReceivable(transaction, receivable).Should().BeTrue();
        SettlementCandidateCompatibility.ScoreReceivable(transaction, receivable).Should().Be(5);
    }

    [Fact]
    public void IsPayableCompatible_ShouldAllowCrossProjectWhenCounterpartyMatches()
    {
        var transaction = new Transaction { Id = 1, SupplierId = 22, ProjectId = 3, TransactionType = TransactionType.Expense };
        var payable = new Payable
        {
            Id = 9,
            SupplierId = 22,
            ProjectId = 8,
            RemainingAmount = 80,
            Status = PayableStatus.Pending
        };

        SettlementCandidateCompatibility.IsPayableCompatible(transaction, payable).Should().BeTrue();
        SettlementCandidateCompatibility.IsPreferredPayable(transaction, payable).Should().BeFalse();
    }
}
