using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Supplier;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class SupplierControllerTests
{
    private readonly Mock<ISupplierService> _supplierServiceMock;
    private readonly Mock<ILogger<SupplierController>> _loggerMock;
    private readonly SupplierController _controller;

    public SupplierControllerTests()
    {
        _supplierServiceMock = new Mock<ISupplierService>();
        _loggerMock = new Mock<ILogger<SupplierController>>();
        _controller = new SupplierController(_supplierServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task BatchCreate_ValidRequest_ReturnsOkWithBatchResult()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateSupplierRequest>
        {
            Items = new List<CreateSupplierRequest>
            {
                new() { Name = "供应商A" },
                new() { Name = "供应商B" }
            }
        };

        var expectedResult = new BatchCreateResponse<SupplierDto>
        {
            TotalCount = 2,
            SuccessCount = 2,
            FailedCount = 0,
            SuccessItems = new List<SupplierDto>
            {
                new() { Id = 1, Name = "供应商A" },
                new() { Id = 2, Name = "供应商B" }
            }
        };

        _supplierServiceMock
            .Setup(x => x.BatchCreateAsync(It.IsAny<List<CreateSupplierRequest>>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<BatchCreateResponse<SupplierDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.SuccessCount.Should().Be(2);
        apiResponse.Data.FailedCount.Should().Be(0);

        _supplierServiceMock.Verify(x => x.BatchCreateAsync(It.IsAny<List<CreateSupplierRequest>>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreate_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateSupplierRequest> { Items = new List<CreateSupplierRequest>() };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_NullItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateSupplierRequest> { Items = null! };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_ExceedsLimit_ReturnsBadRequest()
    {
        // Arrange
        var items = Enumerable.Range(1, 501).Select(i => new CreateSupplierRequest { Name = $"供应商{i}" }).ToList();
        var request = new BatchCreateRequest<CreateSupplierRequest> { Items = items };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveSuppliers()
    {
        // Arrange
        var expectedSuppliers = new List<SupplierDto>
        {
            new() { Id = 1, Name = "供应商1", IsActive = true },
            new() { Id = 2, Name = "供应商2", IsActive = true }
        };

        _supplierServiceMock
            .Setup(x => x.GetActiveSuppliersAsync())
            .ReturnsAsync(expectedSuppliers);

        // Act
        var result = await _controller.GetActive();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<SupplierDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(s => s.IsActive).Should().BeTrue();

        _supplierServiceMock.Verify(x => x.GetActiveSuppliersAsync(), Times.Once);
    }
}
