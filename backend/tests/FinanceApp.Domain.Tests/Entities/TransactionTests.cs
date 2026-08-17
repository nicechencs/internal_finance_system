using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    public void Transaction_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var transaction = new Transaction();

        // Assert
        transaction.Status.Should().Be(TransactionStatus.Confirmed);
        transaction.IsAllocated.Should().BeFalse();
        transaction.Allocations.Should().NotBeNull().And.BeEmpty();
        transaction.IsDeleted.Should().BeFalse();
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Transaction_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var transactionDate = new DateTime(2026, 3, 13);
        var transaction = new Transaction
        {
            TransactionDate = transactionDate,
            Amount = 5000m,
            TransactionType = TransactionType.Expense,
            CategoryId = 1,
            AccountId = 1,
            ProjectId = 1,
            CustomerId = null,
            SupplierId = 1,
            PersonId = null,
            Description = "采购办公用品",
            Status = TransactionStatus.Confirmed,
            IsAllocated = false,
            CreatedBy = 1
        };

        // Assert
        transaction.TransactionDate.Should().Be(transactionDate);
        transaction.Amount.Should().Be(5000m);
        transaction.TransactionType.Should().Be(TransactionType.Expense);
        transaction.CategoryId.Should().Be(1);
        transaction.AccountId.Should().Be(1);
        transaction.ProjectId.Should().Be(1);
        transaction.CustomerId.Should().BeNull();
        transaction.SupplierId.Should().Be(1);
        transaction.PersonId.Should().BeNull();
        transaction.Description.Should().Be("采购办公用品");
        transaction.Status.Should().Be(TransactionStatus.Confirmed);
        transaction.IsAllocated.Should().BeFalse();
        transaction.CreatedBy.Should().Be(1);
    }

    [Theory]
    [InlineData(TransactionType.Income)]
    [InlineData(TransactionType.Expense)]
    public void Transaction_ShouldSupportDifferentTransactionTypes(TransactionType transactionType)
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            TransactionType = transactionType,
            AccountId = 1
        };

        // Assert
        transaction.TransactionType.Should().Be(transactionType);
    }

    [Theory]
    [InlineData(TransactionStatus.Pending)]
    [InlineData(TransactionStatus.Confirmed)]
    [InlineData(TransactionStatus.Cancelled)]
    public void Transaction_ShouldSupportDifferentStatuses(TransactionStatus status)
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Status = status,
            AccountId = 1
        };

        // Assert
        transaction.Status.Should().Be(status);
    }

    [Fact]
    public void Transaction_ShouldSupportAllocations()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = 1,
            Amount = 10000m,
            AccountId = 1,
            IsAllocated = true
        };

        var allocation1 = new TransactionAllocation
        {
            TransactionId = 1,
            ProjectId = 1,
            Amount = 6000m,
            AllocationRate = 0.6m,
            Description = "项目A分摊"
        };

        var allocation2 = new TransactionAllocation
        {
            TransactionId = 1,
            ProjectId = 2,
            Amount = 4000m,
            AllocationRate = 0.4m,
            Description = "项目B分摊"
        };

        // Act
        transaction.Allocations.Add(allocation1);
        transaction.Allocations.Add(allocation2);

        // Assert
        transaction.IsAllocated.Should().BeTrue();
        transaction.Allocations.Should().HaveCount(2);
        transaction.Allocations.Sum(a => a.Amount).Should().Be(10000m);
        transaction.Allocations.Should().Contain(allocation1);
        transaction.Allocations.Should().Contain(allocation2);
    }

    [Fact]
    public void Transaction_ShouldAllowNullableRelationships()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            AccountId = 1,
            BankTransactionId = null,
            CategoryId = null,
            ProjectId = null,
            CustomerId = null,
            SupplierId = null,
            PersonId = null,
            CreatedBy = null
        };

        // Assert
        transaction.BankTransactionId.Should().BeNull();
        transaction.CategoryId.Should().BeNull();
        transaction.ProjectId.Should().BeNull();
        transaction.CustomerId.Should().BeNull();
        transaction.SupplierId.Should().BeNull();
        transaction.PersonId.Should().BeNull();
        transaction.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void Transaction_ShouldSupportSoftDelete()
    {
        // Arrange
        var transaction = new Transaction
        {
            AccountId = 1,
            Amount = 1000m
        };

        // Act
        transaction.IsDeleted = true;
        transaction.DeletedAt = DateTime.UtcNow;

        // Assert
        transaction.IsDeleted.Should().BeTrue();
        transaction.DeletedAt.Should().NotBeNull();
    }
}
