using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<ILogger<ProjectsController>> _loggerMock;
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _loggerMock = new Mock<ILogger<ProjectsController>>();
        _controller = new ProjectsController(_projectServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateCode_ReturnsOkWithGeneratedCode()
    {
        _projectServiceMock
            .Setup(x => x.GenerateProjectCodeAsync())
            .ReturnsAsync("PRJ-2026-001");

        var result = await _controller.GenerateCode();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be("PRJ-2026-001");

        _projectServiceMock.Verify(x => x.GenerateProjectCodeAsync(), Times.Once);
    }

    [Fact]
    public async Task BatchCreate_ValidRequest_ReturnsOkWithBatchResult()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateProjectRequest>
        {
            Items = new List<CreateProjectRequest>
            {
                new() { Name = "项目A", ProjectCode = "PRJ-001", CustomerId = 1, ContractAmount = 100000m, StartDate = DateTime.Today },
                new() { Name = "项目B", ProjectCode = "PRJ-002", CustomerId = 2, ContractAmount = 200000m, StartDate = DateTime.Today }
            }
        };

        var expectedResult = new BatchCreateResponse<ProjectDto>
        {
            TotalCount = 2,
            SuccessCount = 2,
            FailedCount = 0,
            SuccessItems = new List<ProjectDto>
            {
                new() { Id = 1, Name = "项目A", ProjectCode = "PRJ-001" },
                new() { Id = 2, Name = "项目B", ProjectCode = "PRJ-002" }
            }
        };

        _projectServiceMock
            .Setup(x => x.BatchCreateAsync(It.IsAny<List<CreateProjectRequest>>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<BatchCreateResponse<ProjectDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.SuccessCount.Should().Be(2);
        apiResponse.Data.FailedCount.Should().Be(0);

        _projectServiceMock.Verify(x => x.BatchCreateAsync(It.IsAny<List<CreateProjectRequest>>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreate_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateProjectRequest> { Items = new List<CreateProjectRequest>() };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_NullItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateProjectRequest> { Items = null! };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_ExceedsLimit_ReturnsBadRequest()
    {
        // Arrange
        var items = Enumerable.Range(1, 501).Select(i => new CreateProjectRequest
        {
            Name = $"项目{i}",
            ProjectCode = $"PRJ-{i:D3}",
            CustomerId = 1,
            ContractAmount = 10000m,
            StartDate = DateTime.Today
        }).ToList();
        var request = new BatchCreateRequest<CreateProjectRequest> { Items = items };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPaged_FilterByName_ReturnsMatchingProjects()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 20, Name = "测试" };
        var expectedResult = new PageResponse<ProjectDto>
        {
            Items = new List<ProjectDto>
            {
                new() { Id = 1, Name = "测试项目A", ProjectCode = "PRJ-001", Status = "进行中" },
                new() { Id = 2, Name = "测试项目B", ProjectCode = "PRJ-002", Status = "进行中" }
            },
            Page = 1,
            PageSize = 20,
            Total = 2
        };

        _projectServiceMock
            .Setup(x => x.GetPagedAsync(It.Is<PageRequest>(r => r.Name == "测试")))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ProjectDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);
        apiResponse.Data.Items.Should().AllSatisfy(p => p.Name.Should().Contain("测试"));

        _projectServiceMock.Verify(x => x.GetPagedAsync(It.Is<PageRequest>(r => r.Name == "测试")), Times.Once);
    }

    [Fact]
    public async Task GetPaged_FilterByCustomerId_ReturnsMatchingProjects()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 20, CustomerId = 5 };
        var expectedResult = new PageResponse<ProjectDto>
        {
            Items = new List<ProjectDto>
            {
                new() { Id = 3, Name = "客户5项目", ProjectCode = "PRJ-003", Status = "进行中" }
            },
            Page = 1,
            PageSize = 20,
            Total = 1
        };

        _projectServiceMock
            .Setup(x => x.GetPagedAsync(It.Is<PageRequest>(r => r.CustomerId == 5)))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ProjectDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(1);
        apiResponse.Data.Total.Should().Be(1);

        _projectServiceMock.Verify(x => x.GetPagedAsync(It.Is<PageRequest>(r => r.CustomerId == 5)), Times.Once);
    }

    [Fact]
    public async Task GetPaged_FilterByStatus_ReturnsMatchingProjects()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 20, Status = "Completed" };
        var expectedResult = new PageResponse<ProjectDto>
        {
            Items = new List<ProjectDto>
            {
                new() { Id = 4, Name = "已完成项目", ProjectCode = "PRJ-004", Status = "已完成" }
            },
            Page = 1,
            PageSize = 20,
            Total = 1
        };

        _projectServiceMock
            .Setup(x => x.GetPagedAsync(It.Is<PageRequest>(r => r.Status == "Completed")))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ProjectDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(1);
        apiResponse.Data.Total.Should().Be(1);
        apiResponse.Data.Items[0].Status.Should().Be("已完成");

        _projectServiceMock.Verify(x => x.GetPagedAsync(It.Is<PageRequest>(r => r.Status == "Completed")), Times.Once);
    }

    [Fact]
    public async Task GetPaged_FilterByMultipleConditions_ReturnsMatchingProjects()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 20, Name = "项目", CustomerId = 3, Status = "Active" };
        var expectedResult = new PageResponse<ProjectDto>
        {
            Items = new List<ProjectDto>
            {
                new() { Id = 5, Name = "项目X", ProjectCode = "PRJ-005", Status = "进行中" }
            },
            Page = 1,
            PageSize = 20,
            Total = 1
        };

        _projectServiceMock
            .Setup(x => x.GetPagedAsync(It.Is<PageRequest>(r =>
                r.Name == "项目" && r.CustomerId == 3 && r.Status == "Active")))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<ProjectDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(1);
        apiResponse.Data.Total.Should().Be(1);

        _projectServiceMock.Verify(x => x.GetPagedAsync(It.Is<PageRequest>(r =>
            r.Name == "项目" && r.CustomerId == 3 && r.Status == "Active")), Times.Once);
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveProjects()
    {
        // Arrange
        var expectedProjects = new List<ProjectDto>
        {
            new() { Id = 1, Name = "项目1", ProjectCode = "PRJ-001", Status = "进行中" },
            new() { Id = 2, Name = "项目2", ProjectCode = "PRJ-002", Status = "进行中" }
        };

        _projectServiceMock
            .Setup(x => x.GetActiveProjectsAsync())
            .ReturnsAsync(expectedProjects);

        // Act
        var result = await _controller.GetActive();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<ProjectDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);

        _projectServiceMock.Verify(x => x.GetActiveProjectsAsync(), Times.Once);
    }
}
