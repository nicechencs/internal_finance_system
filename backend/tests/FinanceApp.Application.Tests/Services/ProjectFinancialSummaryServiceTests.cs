using FluentAssertions;
using FinanceApp.Application.Modules.Reporting.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class ProjectFinancialSummaryServiceTests : TestBase
{
    private readonly Mock<IRepository<Project>> _projectRepositoryMock = new();
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock = new();
    private readonly Mock<IRepository<PayableDetail>> _payableDetailRepositoryMock = new();
    private readonly Mock<ILogger<ProjectFinancialSummaryService>> _loggerMock = new();

    [Fact]
    public async Task GetProjectSummaryAsync_ShouldCalculateFromReceivablesAndPayableDetails()
    {
        // Arrange
        var projectId = 10L;
        var project = new Project
        {
            Id = projectId,
            ContractAmount = 100000m,
            ReceivedAmount = 1m,
            TotalCost = 1m,
            ProfitAmount = 1m,
            ProfitRate = 1m,
            IsDeleted = false
        };

        var receivables = new List<Receivable>
        {
            new() { Id = 1, ProjectId = projectId, ReceivedAmount = 30000m, RemainingAmount = 10000m, IsDeleted = false },
            new() { Id = 2, ProjectId = projectId, ReceivedAmount = 20000m, RemainingAmount = 5000m, IsDeleted = false }
        };

        var payable = new Payable { Id = 1, ProjectId = projectId, IsDeleted = false };
        var otherProjectPayable = new Payable { Id = 2, ProjectId = 99L, IsDeleted = false };
        var payableDetails = new List<PayableDetail>
        {
            new() { Id = 1, PayableId = payable.Id, Payable = payable, Amount = 12000m, IsDeleted = false },
            new() { Id = 2, PayableId = payable.Id, Payable = payable, Amount = 8000m, IsDeleted = false },
            new() { Id = 3, PayableId = payable.Id, Payable = payable, Amount = 5000m, IsDeleted = true },
            new() { Id = 4, PayableId = otherProjectPayable.Id, Payable = otherProjectPayable, Amount = 9000m, IsDeleted = false }
        };

        _projectRepositoryMock.Setup(x => x.GetQueryable()).Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(x => x.GetQueryable()).Returns(receivables.AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable()).Returns(payableDetails.AsQueryable().BuildMock().Object);

        var service = new ProjectFinancialSummaryService(
            _projectRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        // Act
        var result = await service.GetProjectSummaryAsync(projectId);

        // Assert
        result.ContractAmount.Should().Be(100000m);
        result.ReceivedAmount.Should().Be(50000m);
        result.ReceivableAmount.Should().Be(15000m);
        result.DirectCost.Should().Be(0m);
        result.AllocatedCost.Should().Be(0m);
        result.TotalCost.Should().Be(20000m);
        result.ProfitAmount.Should().Be(30000m);
        result.ProfitRate.Should().Be(30m);
    }

    [Fact]
    public async Task GetProjectSummaryAsync_WhenContractAmountIsZero_ShouldReturnZeroProfitRate()
    {
        // Arrange
        var projectId = 20L;
        var project = new Project
        {
            Id = projectId,
            ContractAmount = 0m,
            IsDeleted = false
        };

        var payable = new Payable { Id = 20, ProjectId = projectId, IsDeleted = false };
        var payableDetails = new List<PayableDetail>
        {
            new() { Id = 10, PayableId = payable.Id, Payable = payable, Amount = 5000m, IsDeleted = false }
        };

        _projectRepositoryMock.Setup(x => x.GetQueryable()).Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(x => x.GetQueryable()).Returns(new List<Receivable>().AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable()).Returns(payableDetails.AsQueryable().BuildMock().Object);

        var service = new ProjectFinancialSummaryService(
            _projectRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        // Act
        var result = await service.GetProjectSummaryAsync(projectId);

        // Assert
        result.ReceivedAmount.Should().Be(0m);
        result.TotalCost.Should().Be(5000m);
        result.ProfitAmount.Should().Be(-5000m);
        result.ProfitRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetProjectSummariesAsync_WithEmptyIds_ShouldReturnEmptyDictionary()
    {
        var service = new ProjectFinancialSummaryService(
            _projectRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        var result = await service.GetProjectSummariesAsync(Array.Empty<long>());

        result.Should().BeEmpty();
        _projectRepositoryMock.Verify(x => x.GetQueryable(), Times.Never);
    }

    [Fact]
    public async Task GetProjectSummariesAsync_WithMultipleProjects_ShouldMatchSingleItemResults()
    {
        var projectA = new Project
        {
            Id = 10L,
            ContractAmount = 100000m,
            IsDeleted = false
        };
        var projectB = new Project
        {
            Id = 20L,
            ContractAmount = 0m,
            IsDeleted = false
        };

        var receivables = new List<Receivable>
        {
            new() { Id = 1, ProjectId = 10L, ReceivedAmount = 30000m, RemainingAmount = 10000m, IsDeleted = false },
            new() { Id = 2, ProjectId = 10L, ReceivedAmount = 20000m, RemainingAmount = 5000m, IsDeleted = false }
        };

        var payableA = new Payable { Id = 1, ProjectId = 10L, IsDeleted = false };
        var payableB = new Payable { Id = 20, ProjectId = 20L, IsDeleted = false };
        var otherProjectPayable = new Payable { Id = 2, ProjectId = 99L, IsDeleted = false };
        var payableDetails = new List<PayableDetail>
        {
            new() { Id = 1, PayableId = payableA.Id, Payable = payableA, Amount = 12000m, IsDeleted = false },
            new() { Id = 2, PayableId = payableA.Id, Payable = payableA, Amount = 8000m, IsDeleted = false },
            new() { Id = 3, PayableId = payableA.Id, Payable = payableA, Amount = 5000m, IsDeleted = true },
            new() { Id = 4, PayableId = otherProjectPayable.Id, Payable = otherProjectPayable, Amount = 9000m, IsDeleted = false },
            new() { Id = 10, PayableId = payableB.Id, Payable = payableB, Amount = 5000m, IsDeleted = false }
        };

        _projectRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Project> { projectA, projectB }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(payableDetails.AsQueryable().BuildMock().Object);

        var service = new ProjectFinancialSummaryService(
            _projectRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        var batch = await service.GetProjectSummariesAsync(new[] { 10L, 20L });

        _projectRepositoryMock.Verify(x => x.GetQueryable(), Times.Once);
        _receivableRepositoryMock.Verify(x => x.GetQueryable(), Times.Once);
        _payableDetailRepositoryMock.Verify(x => x.GetQueryable(), Times.Once);

        var singleA = await service.GetProjectSummaryAsync(10L);
        var singleB = await service.GetProjectSummaryAsync(20L);

        batch.Should().HaveCount(2);

        batch[10].ContractAmount.Should().Be(100000m);
        batch[10].ReceivedAmount.Should().Be(50000m);
        batch[10].ReceivableAmount.Should().Be(15000m);
        batch[10].DirectCost.Should().Be(0m);
        batch[10].AllocatedCost.Should().Be(0m);
        batch[10].TotalCost.Should().Be(20000m);
        batch[10].ProfitAmount.Should().Be(30000m);
        batch[10].ProfitRate.Should().Be(30m);

        batch[20].ReceivedAmount.Should().Be(0m);
        batch[20].TotalCost.Should().Be(5000m);
        batch[20].ProfitAmount.Should().Be(-5000m);
        batch[20].ProfitRate.Should().Be(0m);

        batch[10].Should().BeEquivalentTo(singleA);
        batch[20].Should().BeEquivalentTo(singleB);
    }

    [Fact]
    public async Task GetProjectSummaryAsync_WhenProjectMissing_ShouldReturnFallback()
    {
        _projectRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Project>().AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);
        _payableDetailRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(new List<PayableDetail>().AsQueryable().BuildMock().Object);

        var service = new ProjectFinancialSummaryService(
            _projectRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());

        var result = await service.GetProjectSummaryAsync(99L);

        result.ProjectId.Should().Be(99L);
        result.ContractAmount.Should().Be(0m);
        result.ReceivedAmount.Should().Be(0m);
        result.TotalCost.Should().Be(0m);
        result.ProfitAmount.Should().Be(0m);
        result.ProfitRate.Should().Be(0m);
    }
}
