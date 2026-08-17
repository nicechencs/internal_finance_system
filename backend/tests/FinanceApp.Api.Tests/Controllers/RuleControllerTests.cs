using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class RuleControllerTests
{
    private readonly Mock<IRuleService> _ruleServiceMock;
    private readonly Mock<ILogger<RuleController>> _loggerMock;
    private readonly RuleController _controller;

    public RuleControllerTests()
    {
        _ruleServiceMock = new Mock<IRuleService>();
        _loggerMock = new Mock<ILogger<RuleController>>();
        _controller = new RuleController(_ruleServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<RuleDto>
        {
            Items = new List<RuleDto>
            {
                new RuleDto { Id = 1, Name = "规则1", Priority = 1 },
                new RuleDto { Id = 2, Name = "规则2", Priority = 2 }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _ruleServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<RuleDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _ruleServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithRule()
    {
        // Arrange
        var ruleId = 1L;
        var expectedRule = new RuleDto
        {
            Id = ruleId,
            Name = "测试规则",
            Priority = 1,
            IsActive = true
        };

        _ruleServiceMock
            .Setup(x => x.GetByIdAsync(ruleId))
            .ReturnsAsync(expectedRule);

        // Act
        var result = await _controller.GetById(ruleId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<RuleDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(ruleId);

        _ruleServiceMock.Verify(x => x.GetByIdAsync(ruleId), Times.Once);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithCreatedRule()
    {
        // Arrange
        var request = new CreateRuleRequest
        {
            Name = "新规则",
            Priority = 1,
            CategoryId = 1
        };

        var expectedRule = new RuleDto
        {
            Id = 1,
            Name = request.Name,
            Priority = request.Priority,
            CategoryId = request.CategoryId
        };

        _ruleServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleRequest>()))
            .ReturnsAsync(expectedRule);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<RuleDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("新规则");

        _ruleServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateRuleRequest>()), Times.Once);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithUpdatedRule()
    {
        // Arrange
        var ruleId = 1L;
        var request = new UpdateRuleRequest
        {
            Name = "更新后的规则",
            Priority = 2
        };

        var expectedRule = new RuleDto
        {
            Id = ruleId,
            Name = request.Name,
            Priority = request.Priority
        };

        _ruleServiceMock
            .Setup(x => x.UpdateAsync(ruleId, It.IsAny<UpdateRuleRequest>()))
            .ReturnsAsync(expectedRule);

        // Act
        var result = await _controller.Update(ruleId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<RuleDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("更新后的规则");

        _ruleServiceMock.Verify(x => x.UpdateAsync(ruleId, It.IsAny<UpdateRuleRequest>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var ruleId = 1L;

        _ruleServiceMock
            .Setup(x => x.DeleteAsync(ruleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(ruleId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("删除成功");

        _ruleServiceMock.Verify(x => x.DeleteAsync(ruleId), Times.Once);
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveRules()
    {
        // Arrange
        var expectedRules = new List<RuleDto>
        {
            new RuleDto { Id = 1, Name = "活跃规则1", IsActive = true },
            new RuleDto { Id = 2, Name = "活跃规则2", IsActive = true }
        };

        _ruleServiceMock
            .Setup(x => x.GetActiveRulesAsync())
            .ReturnsAsync(expectedRules);

        // Act
        var result = await _controller.GetActive();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<RuleDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(r => r.IsActive).Should().BeTrue();

        _ruleServiceMock.Verify(x => x.GetActiveRulesAsync(), Times.Once);
    }

    [Fact]
    public async Task MatchCategory_ValidRequest_ReturnsOkWithCategoryId()
    {
        // Arrange
        var request = new MatchCategoryRequest
        {
            CounterpartyName = "测试对方",
            Description = "测试描述",
            Amount = 1000.00m
        };

        var expectedCategoryId = 1L;

        _ruleServiceMock
            .Setup(x => x.MatchCategoryAsync(
                request.CounterpartyName,
                request.Description,
                request.Amount,
                It.IsAny<string?>()))
            .ReturnsAsync(expectedCategoryId);

        // Act
        var result = await _controller.MatchCategory(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<long?>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(expectedCategoryId);

        _ruleServiceMock.Verify(x => x.MatchCategoryAsync(
            request.CounterpartyName,
            request.Description,
            request.Amount,
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task MatchCategory_NoMatch_ReturnsOkWithNull()
    {
        // Arrange
        var request = new MatchCategoryRequest
        {
            CounterpartyName = "未知对方",
            Description = "未知描述",
            Amount = 500.00m
        };

        _ruleServiceMock
            .Setup(x => x.MatchCategoryAsync(
                request.CounterpartyName,
                request.Description,
                request.Amount,
                It.IsAny<string?>()))
            .ReturnsAsync((long?)null);

        // Act
        var result = await _controller.MatchCategory(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<long?>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().BeNull();

        _ruleServiceMock.Verify(x => x.MatchCategoryAsync(
            request.CounterpartyName,
            request.Description,
            request.Amount,
            It.IsAny<string?>()), Times.Once);
    }
}
