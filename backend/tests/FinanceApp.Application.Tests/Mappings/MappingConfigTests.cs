using FluentAssertions;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Tests.Mappings;

public class MappingConfigTests : TestBase
{
    [Fact]
    public void Should_Map_TransactionType_With_PascalCase()
    {
        var transaction = new Transaction
        {
            Id = 1,
            TransactionDate = new DateTime(2026, 3, 28),
            TransactionType = TransactionType.Income,
            Amount = 100m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "测试账户" }
        };

        var result = Mapper.Map<TransactionDto>(transaction);

        result.TransactionType.Should().Be("Income");
    }

    [Fact]
    public void Should_Map_TransferDirection_And_Status_With_PascalCase()
    {
        var transaction = new Transaction
        {
            Id = 2,
            TransactionDate = new DateTime(2026, 3, 28),
            TransactionType = TransactionType.Transfer,
            TransferDirection = TransferDirection.Out,
            Status = TransactionStatus.Confirmed,
            Amount = 100m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "测试账户" }
        };

        var result = Mapper.Map<TransactionDto>(transaction);

        result.TransferDirection.Should().Be("Out");
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public void Should_Map_Null_Project_StartDate_As_Null()
    {
        var project = new Project
        {
            Id = 1,
            Name = "Project Without Start Date",
            ProjectCode = "PRJ-NULL-START",
            Status = ProjectStatus.Active,
            ContractAmount = 1000m,
            StartDate = null
        };

        var result = Mapper.Map<ProjectDto>(project);

        ((DateTime?)result.StartDate).Should().BeNull();
    }
}
