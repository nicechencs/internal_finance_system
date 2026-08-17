using FluentAssertions;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Tests.Entities;

public class TransactionAllocationTests
{
    [Fact]
    public void TransactionAllocation_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var allocation = new TransactionAllocation();

        // Assert
        allocation.IsDeleted.Should().BeFalse();
        allocation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TransactionAllocation_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var allocation = new TransactionAllocation
        {
            TransactionId = 1,
            ProjectId = 1,
            PersonId = null,
            Amount = 6000m,
            AllocationRate = 0.6m,
            Description = "项目A分摊60%"
        };

        // Assert
        allocation.TransactionId.Should().Be(1);
        allocation.ProjectId.Should().Be(1);
        allocation.PersonId.Should().BeNull();
        allocation.Amount.Should().Be(6000m);
        allocation.AllocationRate.Should().Be(0.6m);
        allocation.Description.Should().Be("项目A分摊60%");
    }

    [Fact]
    public void TransactionAllocation_ShouldSupportProjectAllocation()
    {
        // Arrange & Act
        var allocation = new TransactionAllocation
        {
            TransactionId = 1,
            ProjectId = 1,
            Amount = 5000m,
            AllocationRate = 0.5m
        };

        // Assert
        allocation.ProjectId.Should().Be(1);
        allocation.PersonId.Should().BeNull();
    }

    [Fact]
    public void TransactionAllocation_ShouldSupportPersonAllocation()
    {
        // Arrange & Act
        var allocation = new TransactionAllocation
        {
            TransactionId = 1,
            PersonId = 1,
            Amount = 3000m,
            AllocationRate = 0.3m
        };

        // Assert
        allocation.PersonId.Should().Be(1);
        allocation.ProjectId.Should().BeNull();
    }

    [Fact]
    public void TransactionAllocation_ShouldCalculateAllocationRate()
    {
        // Arrange
        decimal totalAmount = 10000m;
        decimal allocationAmount = 3000m;
        decimal expectedRate = 0.3m;

        // Act
        var allocation = new TransactionAllocation
        {
            TransactionId = 1,
            Amount = allocationAmount,
            AllocationRate = allocationAmount / totalAmount
        };

        // Assert
        allocation.AllocationRate.Should().Be(expectedRate);
    }

    [Fact]
    public void TransactionAllocation_ShouldAllowNullableFields()
    {
        // Arrange & Act
        var allocation = new TransactionAllocation
        {
            TransactionId = 1,
            ProjectId = null,
            PersonId = null,
            Amount = 1000m,
            AllocationRate = null,
            Description = null
        };

        // Assert
        allocation.ProjectId.Should().BeNull();
        allocation.PersonId.Should().BeNull();
        allocation.AllocationRate.Should().BeNull();
        allocation.Description.Should().BeNull();
    }
}