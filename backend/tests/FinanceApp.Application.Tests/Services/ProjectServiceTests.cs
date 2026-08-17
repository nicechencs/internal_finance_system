using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class ProjectServiceTests : TestBase
{
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IRepository<Customer>> _customerRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<TransactionAllocation>> _allocationRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IProjectFinancialSummaryService> _financialSummaryServiceMock;
    private readonly Mock<IMasterDataReferenceGuard> _referenceGuardMock;
    private readonly Mock<IReceivableService> _receivableServiceMock;
    private readonly Mock<IProjectFinancialRecalculationService> _recalculationServiceMock;
    private readonly Mock<ILogger<ProjectService>> _loggerMock;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _customerRepositoryMock = new Mock<IRepository<Customer>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _allocationRepositoryMock = new Mock<IRepository<TransactionAllocation>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _financialSummaryServiceMock = new Mock<IProjectFinancialSummaryService>();
        _referenceGuardMock = new Mock<IMasterDataReferenceGuard>();
        _receivableServiceMock = new Mock<IReceivableService>();
        _recalculationServiceMock = new Mock<IProjectFinancialRecalculationService>();
        _loggerMock = new Mock<ILogger<ProjectService>>();
        _referenceGuardMock.Setup(g => g.HasProjectReferencesAsync(It.IsAny<long>())).ReturnsAsync(false);
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock().Object);
        _allocationRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TransactionAllocation>().AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Receivable>().AsQueryable().BuildMock().Object);
        _financialSummaryServiceMock.Setup(s => s.GetProjectSummaryAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new FinanceApp.Application.Modules.Reporting.Models.ProjectFinancialSummary { ProjectId = id });

        UnitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ITransactionScope?)null);

        _service = new ProjectService(
            _projectRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _allocationRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _financialSummaryServiceMock.Object,
            _referenceGuardMock.Object,
            _receivableServiceMock.Object,
            _recalculationServiceMock.Object,
            Mapper,
            _loggerMock.Object,
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "项目1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "项目2", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnProject()
    {
        var project = new Project { Id = 1, Name = "项目1", IsDeleted = false };
        var queryableMock = new List<Project> { project }.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Name.Should().Be("项目1");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldDelegateFinancialSummaryToService()
    {
        var projectId = 10L;
        var project = new Project
        {
            Id = projectId,
            Name = "项目10",
            ContractAmount = 100000m,
            IsDeleted = false
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _financialSummaryServiceMock.Setup(s => s.GetProjectSummaryAsync(projectId))
            .ReturnsAsync(new FinanceApp.Application.Modules.Reporting.Models.ProjectFinancialSummary
            {
                ProjectId = projectId,
                ContractAmount = 100000m,
                ReceivedAmount = 50000m,
                ReceivableAmount = 15000m,
                TotalCost = 20000m,
                ProfitAmount = 30000m,
                ProfitRate = 30m
            });

        var result = await _service.GetByIdAsync(projectId);

        result.ReceivedAmount.Should().Be(50000m);
        result.ReceivableAmount.Should().Be(15000m);
        result.TotalCost.Should().Be(20000m);
        result.ProfitAmount.Should().Be(30000m);
        result.ProfitRate.Should().Be(30m);
        _financialSummaryServiceMock.Verify(s => s.GetProjectSummaryAsync(projectId), Times.Once);
    }

    [Fact]
    public async Task GenerateProjectCodeAsync_ShouldReturnNextAvailableCode()
    {
        var year = DateTime.Now.Year;
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "项目1", ProjectCode = $"PRJ-{year}-001", IsDeleted = false },
            new() { Id = 2, Name = "项目2", ProjectCode = $"prj-{year}-002", IsDeleted = false },
            new() { Id = 3, Name = "项目3", ProjectCode = "CUSTOM-001", IsDeleted = false }
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);

        var result = await _service.GenerateProjectCodeAsync();

        result.Should().Be($"PRJ-{year}-003");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateProject()
    {
        var customer = new Customer { Id = 1, Name = "客户1" };
        var request = new CreateProjectRequest
        {
            Name = "新项目",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 100000,
            StartDate = DateTime.UtcNow
        };

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });

        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldAutoCreateDefaultReceivable()
    {
        var customer = new Customer { Id = 1, Name = "客户1" };
        var request = new CreateProjectRequest
        {
            Name = "新项目",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 100000,
            StartDate = DateTime.UtcNow
        };

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });

        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        _receivableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Receivable>()))
            .ReturnsAsync((Receivable r) =>
            {
                r.Id = 1;
                return r;
            });

        await _service.CreateAsync(request);

        _receivableRepositoryMock.Verify(r => r.AddAsync(It.Is<Receivable>(x =>
            x.ProjectId == 1 &&
            x.CustomerId == 1 &&
            x.TotalAmount == 100000m &&
            x.ReceivedAmount == 0m &&
            x.RemainingAmount == 100000m &&
            x.Status == ReceivableStatus.Pending &&
            x.Description == "项目合同应收款"
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyProjectCode_ShouldThrowValidationException()
    {
        var request = new CreateProjectRequest
        {
            Name = "新项目",
            ProjectCode = "   ",
            CustomerId = 1,
            ContractAmount = 100000,
            StartDate = DateTime.UtcNow
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        exception.Message.Should().Be("项目编号不能为空，请手动输入或点击一键生成");
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateProjectCode_ShouldThrowValidationException()
    {
        var customer = new Customer { Id = 1, Name = "客户1" };
        var existingProjects = new List<Project>
        {
            new() { Id = 10, Name = "已存在项目", ProjectCode = "PRJ-2026-001", CustomerId = 1, IsDeleted = false }
        };
        var request = new CreateProjectRequest
        {
            Name = "新项目",
            ProjectCode = "  prj-2026-001  ",
            CustomerId = 1,
            ContractAmount = 100000,
            StartDate = DateTime.UtcNow
        };

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(existingProjects.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingCustomer_ShouldThrowNotFoundException()
    {
        var request = new CreateProjectRequest { Name = "新项目", ProjectCode = "PRJ-2026-999", CustomerId = 999, ContractAmount = 100000 };
        _customerRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidStatus_ShouldThrowValidationException()
    {
        var project = new Project { Id = 1, Name = "项目1", IsDeleted = false };
        var request = new UpdateProjectRequest { Name = "项目1", ProjectCode = "PRJ-2026-001", Status = "InvalidStatus" };

        var queryableMock = new List<Project> { project }.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_WithSinglePartialReceivableAndChangedContractAmount_ShouldSyncReceivable()
    {
        var project = new Project
        {
            Id = 1,
            Name = "椤圭洰1",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 100000m,
            ReceivedAmount = 30000m,
            ReceivableAmount = 70000m,
            Status = ProjectStatus.Active,
            StartDate = DateTime.UtcNow.Date,
            IsDeleted = false
        };
        var receivable = new Receivable
        {
            Id = 11,
            ProjectId = 1,
            CustomerId = 1,
            TotalAmount = 100000m,
            ReceivedAmount = 30000m,
            RemainingAmount = 70000m,
            Status = ReceivableStatus.Partial,
            IsDeleted = false
        };
        var request = new UpdateProjectRequest
        {
            Name = "椤圭洰1",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 120000m,
            StartDate = project.StartDate ?? DateTime.UtcNow.Date,
            EndDate = null,
            Description = "updated",
            Status = "Active"
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        await _service.UpdateAsync(1, request);

        _receivableRepositoryMock.Verify(r => r.Update(It.Is<Receivable>(x =>
            x.Id == 11 &&
            x.TotalAmount == 120000m &&
            x.ReceivedAmount == 30000m &&
            x.RemainingAmount == 90000m &&
            x.Status == ReceivableStatus.Partial
        )), Times.Once);
        _recalculationServiceMock.Verify(r => r.RecalculateAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingReceivablesAndNullCustomer_ShouldThrowValidationException()
    {
        var project = new Project
        {
            Id = 1,
            Name = "椤圭洰1",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 100000m,
            ReceivedAmount = 0m,
            ReceivableAmount = 100000m,
            Status = ProjectStatus.Active,
            StartDate = DateTime.UtcNow.Date,
            IsDeleted = false
        };
        var receivable = new Receivable
        {
            Id = 11,
            ProjectId = 1,
            CustomerId = 1,
            TotalAmount = 100000m,
            ReceivedAmount = 0m,
            RemainingAmount = 100000m,
            Status = ReceivableStatus.Pending,
            IsDeleted = false
        };
        var request = new UpdateProjectRequest
        {
            Name = "椤圭洰1",
            ProjectCode = "PRJ-2026-001",
            CustomerId = null,
            ContractAmount = 100000m,
            StartDate = project.StartDate ?? DateTime.UtcNow.Date,
            EndDate = null,
            Description = "updated",
            Status = "Active"
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable> { receivable }.AsQueryable().BuildMock().Object);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));

        exception.Message.Should().Contain("客户");
        _receivableRepositoryMock.Verify(r => r.Update(It.IsAny<Receivable>()), Times.Never);
        _recalculationServiceMock.Verify(r => r.RecalculateAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithMultipleReceivablesAndChangedContractAmount_ShouldThrowValidationException()
    {
        var project = new Project
        {
            Id = 1,
            Name = "椤圭洰1",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 100000m,
            ReceivedAmount = 0m,
            ReceivableAmount = 100000m,
            Status = ProjectStatus.Active,
            StartDate = DateTime.UtcNow.Date,
            IsDeleted = false
        };
        var receivables = new List<Receivable>
        {
            new()
            {
                Id = 11,
                ProjectId = 1,
                CustomerId = 1,
                TotalAmount = 60000m,
                ReceivedAmount = 0m,
                RemainingAmount = 60000m,
                Status = ReceivableStatus.Pending,
                IsDeleted = false
            },
            new()
            {
                Id = 12,
                ProjectId = 1,
                CustomerId = 1,
                TotalAmount = 40000m,
                ReceivedAmount = 0m,
                RemainingAmount = 40000m,
                Status = ReceivableStatus.Pending,
                IsDeleted = false
            }
        };
        var request = new UpdateProjectRequest
        {
            Name = "椤圭洰1",
            ProjectCode = "PRJ-2026-001",
            CustomerId = 1,
            ContractAmount = 120000m,
            StartDate = project.StartDate ?? DateTime.UtcNow.Date,
            EndDate = null,
            Description = "updated",
            Status = "Active"
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));

        exception.Message.Should().Contain("多条应收");
        _receivableRepositoryMock.Verify(r => r.Update(It.IsAny<Receivable>()), Times.Never);
        _recalculationServiceMock.Verify(r => r.RecalculateAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyProjectCode_ShouldThrowValidationException()
    {
        var project = new Project { Id = 1, Name = "项目1", IsDeleted = false };
        var request = new UpdateProjectRequest
        {
            Name = "项目1",
            ProjectCode = " ",
            Status = "Active"
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));

        exception.Message.Should().Be("项目编号不能为空，请手动输入或点击一键生成");
        _projectRepositoryMock.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateProjectCode_ShouldThrowValidationException()
    {
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "项目1", ProjectCode = "PRJ-2026-001", CustomerId = 1, IsDeleted = false },
            new() { Id = 2, Name = "项目2", ProjectCode = "PRJ-2026-002", CustomerId = 1, IsDeleted = false }
        };
        var request = new UpdateProjectRequest
        {
            Name = "项目1",
            ProjectCode = "prj-2026-002",
            CustomerId = 1,
            ContractAmount = 100000,
            StartDate = DateTime.UtcNow,
            Status = "Active"
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(projects.AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, request));

        _projectRepositoryMock.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveProjectsAsync_ShouldReturnActiveProjects()
    {
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "活跃项目1", Status = ProjectStatus.Active, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "已完成项目", Status = ProjectStatus.Completed, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "活跃项目2", Status = ProjectStatus.Active, IsDeleted = false, CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetActiveProjectsAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Name.Contains("活跃项目"));
    }

    [Fact]
    public async Task BatchCreateAsync_WithValidItems_ShouldCreateAll()
    {
        var customer = new Customer { Id = 1, Name = "客户1" };
        var items = new List<CreateProjectRequest>
        {
            new() { Name = "项目A", ProjectCode = "PA", CustomerId = 1, ContractAmount = 100000, StartDate = DateTime.UtcNow },
            new() { Name = "项目B", ProjectCode = "PB", CustomerId = 1, ContractAmount = 200000, StartDate = DateTime.UtcNow }
        };

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = projects.Count + 1;
                projects.Add(p);
                return p;
            });
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        var result = await _service.BatchCreateAsync(items);

        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.SuccessItems.Should().HaveCount(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchCreateAsync_WithEmptyList_ShouldReturnEmptyResult()
    {
        var items = new List<CreateProjectRequest>();

        var result = await _service.BatchCreateAsync(items);

        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "项目1", ContractAmount = 100000m, ProfitAmount = 20000m, ReceivableAmount = 50000m, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "项目2", ContractAmount = 200000m, ProfitAmount = 30000m, ReceivableAmount = 80000m, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "项目3", ContractAmount = 150000m, ProfitAmount = 10000m, ReceivableAmount = 0m, IsDeleted = false, CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.TotalContractAmount.Should().Be(450000m); // 100000 + 200000 + 150000
        result.TotalProfit.Should().Be(60000m); // 20000 + 30000 + 10000
        result.TotalReceivable.Should().Be(130000m); // 50000 + 80000 + 0
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyData_ShouldReturnZeros()
    {
        // Arrange
        var projects = new List<Project>();
        var queryableMock = projects.AsQueryable().BuildMock();
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.TotalContractAmount.Should().Be(0m);
        result.TotalProfit.Should().Be(0m);
        result.TotalReceivable.Should().Be(0m);
    }

    [Fact]
    public async Task DeleteAsync_WithReferencedProject_ShouldArchiveInsteadOfDelete()
    {
        var project = new Project { Id = 1, Name = "椤圭洰1", Status = ProjectStatus.Active };
        _projectRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<Project> { project }.AsQueryable().BuildMock().Object);
        _referenceGuardMock.Setup(g => g.HasProjectReferencesAsync(1)).ReturnsAsync(true);

        await _service.DeleteAsync(1);

        project.Status.Should().Be(ProjectStatus.Cancelled);
        _projectRepositoryMock.Verify(r => r.Update(project), Times.Once);
        _projectRepositoryMock.Verify(r => r.Delete(It.IsAny<Project>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Project", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task InitializeReceivablesAsync_OnceMode_ShouldCreateSingleReceivable()
    {
        // Arrange
        var project = new Project
        {
            Id = 1,
            Name = "项目1",
            CustomerId = 10,
            ContractAmount = 100000m
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // 模拟空的 receivable 列表（项目未初始化）
        var emptyReceivables = new List<Receivable>().AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyReceivables.Object);

        _receivableServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateReceivableRequest>()))
            .ReturnsAsync(new ReceivableDto { Id = 1, TotalAmount = 100000m });

        var request = new InitializeReceivablesRequest
        {
            Mode = "once"
        };

        // Act
        await _service.InitializeReceivablesAsync(1, request);

        // Assert
        _receivableServiceMock.Verify(s => s.CreateAsync(It.Is<CreateReceivableRequest>(r =>
            r.ProjectId == 1 &&
            r.CustomerId == 10 &&
            r.TotalAmount == 100000m &&
            r.Description == "一次性收款"
        )), Times.Once);
    }

    [Fact]
    public async Task InitializeReceivablesAsync_InstallmentMode_ShouldCreateMultipleReceivables()
    {
        // Arrange
        var project = new Project
        {
            Id = 1,
            Name = "项目1",
            CustomerId = 10,
            ContractAmount = 100000m
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // 模拟空的 receivable 列表（项目未初始化）
        var emptyReceivables = new List<Receivable>().AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyReceivables.Object);

        var receivables = new List<Receivable>();
        _receivableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Receivable>()))
            .ReturnsAsync((Receivable r) =>
            {
                r.Id = receivables.Count + 1;
                receivables.Add(r);
                return r;
            });

        var request = new InitializeReceivablesRequest
        {
            Mode = "installment",
            Installments = new List<ReceivableInstallmentDto>
            {
                new() { Name = "第一期", Amount = 40000m, DueDate = DateTime.UtcNow.AddDays(30) },
                new() { Name = "第二期", Amount = 30000m, DueDate = DateTime.UtcNow.AddDays(60) },
                new() { Name = "第三期", Amount = 30000m, DueDate = DateTime.UtcNow.AddDays(90) }
            }
        };

        // Act
        await _service.InitializeReceivablesAsync(1, request);

        // Assert
        _receivableRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Receivable>()), Times.Exactly(3));
        receivables.Should().HaveCount(3);
        receivables[0].TotalAmount.Should().Be(40000m);
        receivables[0].Description.Should().Be("第一期");
        receivables[1].TotalAmount.Should().Be(30000m);
        receivables[2].TotalAmount.Should().Be(30000m);
    }

    [Fact]
    public async Task InitializeReceivablesAsync_InstallmentMode_ShouldValidateTotalAmount()
    {
        // Arrange
        var project = new Project
        {
            Id = 1,
            Name = "项目1",
            CustomerId = 10,
            ContractAmount = 100000m
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // 模拟空的 receivable 列表（项目未初始化）
        var emptyReceivables = new List<Receivable>().AsQueryable().BuildMock();
        _receivableRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyReceivables.Object);

        var request = new InitializeReceivablesRequest
        {
            Mode = "installment",
            Installments = new List<ReceivableInstallmentDto>
            {
                new() { Name = "第一期", Amount = 40000m, DueDate = DateTime.UtcNow.AddDays(30) },
                new() { Name = "第二期", Amount = 30000m, DueDate = DateTime.UtcNow.AddDays(60) }
                // 总额 70000，与合同金额 100000 不一致
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.InitializeReceivablesAsync(1, request));
    }

    #endregion

    #region CreateAsync Nullable Fields Tests

    [Fact]
    public async Task CreateAsync_WithNullCustomerId_ShouldCreateProjectWithoutCustomer()
    {
        var request = new CreateProjectRequest
        {
            Name = "无客户项目",
            ProjectCode = "PRJ-2026-100",
            CustomerId = null,
            ContractAmount = 50000,
            StartDate = DateTime.UtcNow
        };

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        _projectRepositoryMock.Verify(r => r.AddAsync(It.Is<Project>(p => p.CustomerId == null)), Times.Once);
        _customerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullStartDate_ShouldCreateProjectWithoutStartDate()
    {
        var customer = new Customer { Id = 1, Name = "客户1" };
        var request = new CreateProjectRequest
        {
            Name = "无日期项目",
            ProjectCode = "PRJ-2026-101",
            CustomerId = 1,
            ContractAmount = 80000,
            StartDate = null
        };

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        _projectRepositoryMock.Verify(r => r.AddAsync(It.Is<Project>(p => p.StartDate == null)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullCustomerIdAndStartDate_ShouldCreateProject()
    {
        var request = new CreateProjectRequest
        {
            Name = "最小项目",
            ProjectCode = "PRJ-2026-102",
            CustomerId = null,
            ContractAmount = 30000,
            StartDate = null,
            EndDate = null
        };

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        _projectRepositoryMock.Verify(r => r.AddAsync(It.Is<Project>(p =>
            p.CustomerId == null &&
            p.StartDate == null &&
            p.ContractAmount == 30000 &&
            p.Status == ProjectStatus.Active
        )), Times.Once);
        _customerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStatus_ShouldThrowValidationException()
    {
        var request = new CreateProjectRequest
        {
            Name = "项目",
            ProjectCode = "PRJ-2026-103",
            CustomerId = null,
            ContractAmount = 10000,
            Status = "InvalidStatus"
        };

        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Project>().AsQueryable().BuildMock().Object);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        exception.Message.Should().Be("无效的项目状态");
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Never);
    }

    [Theory]
    [InlineData("Active", ProjectStatus.Active)]
    [InlineData("Completed", ProjectStatus.Completed)]
    [InlineData("Cancelled", ProjectStatus.Cancelled)]
    [InlineData("active", ProjectStatus.Active)]
    [InlineData("COMPLETED", ProjectStatus.Completed)]
    public async Task CreateAsync_WithValidStatusString_ShouldParseCorrectly(string statusStr, ProjectStatus expected)
    {
        var request = new CreateProjectRequest
        {
            Name = "状态测试项目",
            ProjectCode = $"PRJ-STATUS-{statusStr}",
            CustomerId = null,
            ContractAmount = 10000,
            Status = statusStr
        };

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        await _service.CreateAsync(request);

        _projectRepositoryMock.Verify(r => r.AddAsync(It.Is<Project>(p => p.Status == expected)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithProjectEndDate_ShouldSetDefaultReceivableDueDate()
    {
        var customer = new Customer { Id = 1, Name = "Customer 1" };
        var projectEndDate = new DateTime(2026, 6, 30, 18, 45, 0, DateTimeKind.Utc);
        var request = new CreateProjectRequest
        {
            Name = "Project With End Date",
            ProjectCode = "PRJ-2026-002",
            CustomerId = 1,
            ContractAmount = 100000,
            StartDate = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
            EndDate = projectEndDate
        };

        _customerRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var projects = new List<Project>();
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) =>
            {
                p.Id = 1;
                projects.Add(p);
                return p;
            });
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => projects.AsQueryable().BuildMock().Object);

        _receivableRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Receivable>()))
            .ReturnsAsync((Receivable r) =>
            {
                r.Id = 1;
                return r;
            });

        await _service.CreateAsync(request);

        _receivableRepositoryMock.Verify(r => r.AddAsync(It.Is<Receivable>(x =>
            x.ProjectId == 1 &&
            x.DueDate == projectEndDate.Date
        )), Times.Once);
    }

    #endregion
}
