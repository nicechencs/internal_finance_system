using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Category;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<ILogger<CategoryController>> _loggerMock;
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _loggerMock = new Mock<ILogger<CategoryController>>();
        _controller = new CategoryController(_categoryServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPaged_ValidRequest_ReturnsOkWithPagedData()
    {
        // Arrange
        var request = new PageRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new PageResponse<CategoryDto>
        {
            Items = new List<CategoryDto>
            {
                new CategoryDto { Id = 1, Name = "收入", CategoryType = "income" },
                new CategoryDto { Id = 2, Name = "支出", CategoryType = "expense" }
            },
            Total = 2,
            Page = 1,
            PageSize = 10
        };

        _categoryServiceMock
            .Setup(x => x.GetPagedAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaged(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PageResponse<CategoryDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.Total.Should().Be(2);

        _categoryServiceMock.Verify(x => x.GetPagedAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithCategory()
    {
        // Arrange
        var categoryId = 1L;
        var expectedCategory = new CategoryDto
        {
            Id = categoryId,
            Name = "工资收入",
            CategoryType = "income",
            IsActive = true
        };

        _categoryServiceMock
            .Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.GetById(categoryId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(categoryId);

        _categoryServiceMock.Verify(x => x.GetByIdAsync(categoryId), Times.Once);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithCreatedCategory()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "新分类",
            CategoryType = "expense",
            ParentId = null
        };

        var expectedCategory = new CategoryDto
        {
            Id = 1,
            Name = request.Name,
            CategoryType = request.CategoryType,
            ParentId = request.ParentId
        };

        _categoryServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateCategoryRequest>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("新分类");

        _categoryServiceMock.Verify(x => x.CreateAsync(It.IsAny<CreateCategoryRequest>()), Times.Once);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithUpdatedCategory()
    {
        // Arrange
        var categoryId = 1L;
        var request = new UpdateCategoryRequest
        {
            Name = "更新后的分类"
        };

        var expectedCategory = new CategoryDto
        {
            Id = categoryId,
            Name = request.Name
        };

        _categoryServiceMock
            .Setup(x => x.UpdateAsync(categoryId, It.IsAny<UpdateCategoryRequest>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.Update(categoryId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("更新后的分类");

        _categoryServiceMock.Verify(x => x.UpdateAsync(categoryId, It.IsAny<UpdateCategoryRequest>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var categoryId = 1L;

        _categoryServiceMock
            .Setup(x => x.DeleteAsync(categoryId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(categoryId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("删除成功");

        _categoryServiceMock.Verify(x => x.DeleteAsync(categoryId), Times.Once);
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveCategories()
    {
        // Arrange
        var expectedCategories = new List<CategoryDto>
        {
            new CategoryDto { Id = 1, Name = "活跃分类1", IsActive = true },
            new CategoryDto { Id = 2, Name = "活跃分类2", IsActive = true }
        };

        _categoryServiceMock
            .Setup(x => x.GetActiveCategoriesAsync())
            .ReturnsAsync(expectedCategories);

        // Act
        var result = await _controller.GetActive();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(c => c.IsActive).Should().BeTrue();

        _categoryServiceMock.Verify(x => x.GetActiveCategoriesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByType_ValidType_ReturnsOkWithCategories()
    {
        // Arrange
        var categoryType = "income";
        var expectedCategories = new List<CategoryDto>
        {
            new CategoryDto { Id = 1, Name = "工资", CategoryType = categoryType },
            new CategoryDto { Id = 2, Name = "奖金", CategoryType = categoryType }
        };

        _categoryServiceMock
            .Setup(x => x.GetCategoriesByTypeAsync(categoryType))
            .ReturnsAsync(expectedCategories);

        // Act
        var result = await _controller.GetByType(categoryType);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data.All(c => c.CategoryType == categoryType).Should().BeTrue();

        _categoryServiceMock.Verify(x => x.GetCategoriesByTypeAsync(categoryType), Times.Once);
    }
}
