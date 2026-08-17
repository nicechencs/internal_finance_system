using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Customer;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<ILogger<CustomerController>> _loggerMock;
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _loggerMock = new Mock<ILogger<CustomerController>>();
        _controller = new CustomerController(_customerServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task BatchCreate_ValidRequest_ReturnsOkWithBatchResult()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateCustomerRequest>
        {
            Items = new List<CreateCustomerRequest>
            {
                new() { Name = "客户A" },
                new() { Name = "客户B" }
            }
        };

        var expectedResult = new BatchCreateResponse<CustomerDto>
        {
            TotalCount = 2,
            SuccessCount = 2,
            FailedCount = 0,
            SuccessItems = new List<CustomerDto>
            {
                new() { Id = 1, Name = "客户A" },
                new() { Id = 2, Name = "客户B" }
            }
        };

        _customerServiceMock
            .Setup(x => x.BatchCreateAsync(It.IsAny<List<CreateCustomerRequest>>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<BatchCreateResponse<CustomerDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.SuccessCount.Should().Be(2);
        apiResponse.Data.FailedCount.Should().Be(0);

        _customerServiceMock.Verify(x => x.BatchCreateAsync(It.IsAny<List<CreateCustomerRequest>>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreate_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateCustomerRequest> { Items = new List<CreateCustomerRequest>() };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_NullItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreateCustomerRequest> { Items = null! };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_ExceedsLimit_ReturnsBadRequest()
    {
        // Arrange
        var items = Enumerable.Range(1, 501).Select(i => new CreateCustomerRequest { Name = $"客户{i}" }).ToList();
        var request = new BatchCreateRequest<CreateCustomerRequest> { Items = items };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveCustomers()
    {
        // Arrange
        var expectedCustomers = new List<CustomerDto>
        {
            new() { Id = 1, Name = "客户1", IsActive = true },
            new() { Id = 2, Name = "客户2", IsActive = true }
        };

        _customerServiceMock
            .Setup(x => x.GetActiveCustomersAsync())
            .ReturnsAsync(expectedCustomers);

        // Act
        var result = await _controller.GetActive();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CustomerDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(c => c.IsActive).Should().BeTrue();

        _customerServiceMock.Verify(x => x.GetActiveCustomersAsync(), Times.Once);
    }
}
