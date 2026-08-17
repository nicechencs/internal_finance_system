using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.Identity;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class AuditLogControllerTests
{
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly AuditLogController _controller;

    public AuditLogControllerTests()
    {
        _auditLogServiceMock = new Mock<IAuditLogService>();
        var loggerMock = new Mock<ILogger<AuditLogController>>();
        _controller = new AuditLogController(_auditLogServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new AuditLogPageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<AuditLogDto>
        {
            Items = new List<AuditLogDto>
            {
                new AuditLogDto
                {
                    Id = 1,
                    EntityType = "Transaction",
                    EntityId = 100,
                    Action = "Create",
                    OldValue = null,
                    NewValue = "{\"Amount\":1000.00}",
                    OperatorName = "张三",
                    IpAddress = "192.168.1.100",
                    CreatedAt = DateTime.Now.AddHours(-2)
                },
                new AuditLogDto
                {
                    Id = 2,
                    EntityType = "Account",
                    EntityId = 5,
                    Action = "Update",
                    OldValue = "{\"Name\":\"旧账户名\"}",
                    NewValue = "{\"Name\":\"新账户名\"}",
                    OperatorName = "李四",
                    IpAddress = "192.168.1.101",
                    CreatedAt = DateTime.Now.AddHours(-1)
                }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _auditLogServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<AuditLogDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);
        apiResponse.Data.Page.Should().Be(1);
        apiResponse.Data.PageSize.Should().Be(10);

        _auditLogServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_EmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var request = new AuditLogPageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<AuditLogDto>
        {
            Items = new List<AuditLogDto>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _auditLogServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<AuditLogDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.Total.Should().Be(0);

        _auditLogServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_LargePageSize_ReturnsOkWithRequestedPageSize()
    {
        // Arrange
        var request = new AuditLogPageRequest { Page = 1, PageSize = 100 };
        var items = Enumerable.Range(1, 50).Select(i => new AuditLogDto
        {
            Id = i,
            EntityType = "Transaction",
            EntityId = i * 10,
            Action = "Create",
            OperatorName = $"用户{i}",
            IpAddress = $"192.168.1.{i}",
            CreatedAt = DateTime.Now.AddHours(-i)
        }).ToList();

        var expectedResponse = new PageResponse<AuditLogDto>
        {
            Items = items,
            Total = 50,
            Page = 1,
            PageSize = 100
        };

        _auditLogServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<AuditLogDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(50);
        apiResponse.Data.Total.Should().Be(50);

        _auditLogServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsOkWithCorrectPageNumber()
    {
        // Arrange
        var request = new AuditLogPageRequest { Page = 2, PageSize = 10 };
        var expectedResponse = new PageResponse<AuditLogDto>
        {
            Items = new List<AuditLogDto>
            {
                new AuditLogDto
                {
                    Id = 11,
                    EntityType = "Customer",
                    EntityId = 20,
                    Action = "Delete",
                    OldValue = "{\"Name\":\"已删除客户\"}",
                    NewValue = null,
                    OperatorName = "管理员",
                    IpAddress = "192.168.1.1",
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            },
            Total = 15,
            Page = 2,
            PageSize = 10
        };

        _auditLogServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<AuditLogDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Page.Should().Be(2);
        apiResponse.Data.Items.Should().HaveCount(1);

        _auditLogServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<AuditLogPageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_VerifyServiceCalledWithCorrectParameters()
    {
        // Arrange
        var request = new AuditLogPageRequest { Page = 3, PageSize = 25 };
        var expectedResponse = new PageResponse<AuditLogDto>
        {
            Items = new List<AuditLogDto>(),
            Total = 0,
            Page = 3,
            PageSize = 25
        };

        _auditLogServiceMock
            .Setup(x => x.GetPagedAsync(It.Is<AuditLogPageRequest>(r => r.Page == 3 && r.PageSize == 25)))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        _auditLogServiceMock.Verify(
            x => x.GetPagedAsync(It.Is<AuditLogPageRequest>(r => r.Page == 3 && r.PageSize == 25)),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithData()
    {
        // Arrange
        var expectedDto = new AuditLogDto
        {
            Id = 1,
            EntityType = "Transaction",
            EntityId = 100,
            Action = "Create",
            OldValue = null,
            NewValue = "{\"Amount\":1000.00}",
            OperatorName = "张三",
            IpAddress = "192.168.1.100",
            CreatedAt = DateTime.Now.AddHours(-2)
        };

        _auditLogServiceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuditLogDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(1);
        apiResponse.Data.EntityType.Should().Be("Transaction");
        apiResponse.Data.Action.Should().Be("Create");

        _auditLogServiceMock.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _auditLogServiceMock
            .Setup(x => x.GetByIdAsync(999))
            .ThrowsAsync(new NotFoundException("审计日志不存在"));

        // Act
        Func<Task> act = async () => await _controller.GetById(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _auditLogServiceMock.Verify(x => x.GetByIdAsync(999), Times.Once);
    }
}

