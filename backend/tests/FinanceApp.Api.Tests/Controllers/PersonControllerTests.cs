using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class PersonControllerTests
{
    private readonly Mock<IPersonService> _personServiceMock;
    private readonly Mock<IPersonDeliveryService> _personDeliveryServiceMock;
    private readonly Mock<ILogger<PersonController>> _loggerMock;
    private readonly PersonController _controller;

    public PersonControllerTests()
    {
        _personServiceMock = new Mock<IPersonService>();
        _personDeliveryServiceMock = new Mock<IPersonDeliveryService>();
        _loggerMock = new Mock<ILogger<PersonController>>();
        _controller = new PersonController(
            _personServiceMock.Object,
            _personDeliveryServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task BatchCreate_ValidRequest_ReturnsOkWithBatchResult()
    {
        // Arrange
        var request = new BatchCreateRequest<CreatePersonRequest>
        {
            Items = new List<CreatePersonRequest>
            {
                new() { Name = "张三", PersonType = "员工" },
                new() { Name = "李四", PersonType = "外包" }
            }
        };

        var expectedResult = new BatchCreateResponse<PersonDto>
        {
            TotalCount = 2,
            SuccessCount = 2,
            FailedCount = 0,
            SuccessItems = new List<PersonDto>
            {
                new() { Id = 1, Name = "张三", PersonType = "员工" },
                new() { Id = 2, Name = "李四", PersonType = "外包" }
            }
        };

        _personServiceMock
            .Setup(x => x.BatchCreateAsync(It.IsAny<List<CreatePersonRequest>>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<BatchCreateResponse<PersonDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.SuccessCount.Should().Be(2);
        apiResponse.Data.FailedCount.Should().Be(0);

        _personServiceMock.Verify(x => x.BatchCreateAsync(It.IsAny<List<CreatePersonRequest>>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreate_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreatePersonRequest> { Items = new List<CreatePersonRequest>() };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_NullItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCreateRequest<CreatePersonRequest> { Items = null! };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_ExceedsLimit_ReturnsBadRequest()
    {
        // Arrange
        var items = Enumerable.Range(1, 501).Select(i => new CreatePersonRequest { Name = $"人员{i}", PersonType = "员工" }).ToList();
        var request = new BatchCreateRequest<CreatePersonRequest> { Items = items };

        // Act
        var result = await _controller.BatchCreate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetDeliveries_ReturnsOk()
    {
        var expected = new List<PersonDeliveryDto>
        {
            new() { Id = 8, PersonId = 1, Title = "月度报表", Status = "Pending" }
        };
        _personDeliveryServiceMock
            .Setup(x => x.GetByPersonAsync(1))
            .ReturnsAsync(expected);

        var result = await _controller.GetDeliveries(1);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<PersonDeliveryDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().ContainSingle(d => d.Title == "月度报表");
    }

    [Fact]
    public async Task CreateDelivery_ReturnsCreated()
    {
        var request = new CreatePersonDeliveryRequest { Title = "验收包", Status = "Accepted" };
        var created = new PersonDeliveryDto { Id = 9, PersonId = 3, Title = "验收包", Status = "Accepted" };
        _personDeliveryServiceMock
            .Setup(x => x.CreateAsync(3, request))
            .ReturnsAsync(created);

        var result = await _controller.CreateDelivery(3, request);

        var createdResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<PersonDeliveryDto>>().Subject;
        apiResponse.Data!.Id.Should().Be(9);
        apiResponse.Message.Should().Be("交付记录创建成功");
    }

    [Fact]
    public void DeliveryEndpoints_UseMasterDataRoles()
    {
        AssertRoles(nameof(PersonController.GetDeliveries), "Admin,Accountant,Viewer");
        AssertRoles(nameof(PersonController.CreateDelivery), "Admin,Accountant");
        AssertRoles(nameof(PersonController.UpdateDelivery), "Admin,Accountant");
        AssertRoles(nameof(PersonController.DeleteDelivery), "Admin");
    }

    private static void AssertRoles(string methodName, string expectedRoles)
    {
        var method = typeof(PersonController).GetMethod(methodName);
        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(expectedRoles);
    }
}
