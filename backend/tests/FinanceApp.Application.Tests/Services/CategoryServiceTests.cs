using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Category;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using MapsterMapper;

namespace FinanceApp.Application.Tests.Services;

public class CategoryServiceTests : TestBase
{
    private readonly Mock<IRepository<Category>> _repositoryMock;
    private readonly Mock<IRepository<ClassificationRule>> _ruleRepositoryMock;
    private readonly Mock<IMasterDataReferenceGuard> _referenceGuardMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CategoryService>> _loggerMock;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Category>>();
        _ruleRepositoryMock = new Mock<IRepository<ClassificationRule>>();
        _referenceGuardMock = new Mock<IMasterDataReferenceGuard>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CategoryService>>();
        _referenceGuardMock.Setup(g => g.HasCategoryReferencesAsync(It.IsAny<long>())).ReturnsAsync(false);
        _ruleRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<ClassificationRule>().AsQueryable().BuildMock().Object);

        _service = new CategoryService(
            _repositoryMock.Object,
            _ruleRepositoryMock.Object,
            _referenceGuardMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object
        );
    }

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPagedAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items[0].Name.Should().Be("工资收入");
        result.Items[0].CategoryType.Should().Be("Income");
    }

    [Fact]
    public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyPage()
    {
        // Arrange
        var categories = new List<Category>();
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPagedAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagedAsync_WithNameFilter_ShouldReturnMatchingCategories()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "投资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "收入" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.Name.Should().Contain("收入"));
    }

    [Fact]
    public async Task GetPagedAsync_WithCategoryTypeFilter_ShouldReturnMatchingCategories()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "投资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, CategoryType = "Income" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.CategoryType.Should().Be("Income"));
    }

    [Fact]
    public async Task GetPagedAsync_WithNameAndCategoryTypeFilter_ShouldApplyBothFilters()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "投资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 4, Name = "差旅费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "费用", CategoryType = "Expense" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.CategoryType.Should().Be("Expense"));
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidCategoryType_ShouldReturnAllCategories()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, CategoryType = "InvalidType" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCategory()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "工资收入",
            CategoryType = CategoryType.Income,
            ParentId = null,
            Description = "员工工资",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var categories = new List<Category> { category };
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("工资收入");
        result.CategoryType.Should().Be("Income");
        result.Description.Should().Be("员工工资");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        // Arrange
        var categories = new List<Category>();
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_WithParentCategory_ShouldIncludeParentName()
    {
        // Arrange
        var parent = new Category { Id = 1, Name = "收入", CategoryType = CategoryType.Income };
        var category = new Category
        {
            Id = 2,
            Name = "工资收入",
            CategoryType = CategoryType.Income,
            ParentId = 1,
            Parent = parent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var categories = new List<Category> { parent, category };
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(1);
        result.ParentName.Should().Be("收入");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateCategory()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "工资收入",
            CategoryType = "Income",
            Description = "员工工资"
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) =>
            {
                c.Id = 1;
                c.CreatedAt = DateTime.UtcNow;
                return c;
            });


        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("工资收入");
        result.CategoryType.Should().Be("Income");
        result.Description.Should().Be("员工工资");
        result.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
        AuditLogServiceMock.Verify(a => a.LogAsync("Create", "Category", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithParentId_ShouldValidateParentExists()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "工资收入",
            CategoryType = "Income",
            ParentId = 1
        };

        var parent = new Category { Id = 1, Name = "收入", CategoryType = CategoryType.Income };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parent);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) =>
            {
                c.Id = 2;
                c.CreatedAt = DateTime.UtcNow;
                return c;
            });


        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(1);
        _repositoryMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingParent_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "工资收入",
            CategoryType = "Income",
            ParentId = 999
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidType_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "测试分类",
            CategoryType = "InvalidType"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
        exception.Message.Should().Contain("Invalid category type");
    }

    [Fact]
    public async Task CreateAsync_WithExpenseType_ShouldCreateExpenseCategory()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "办公费用",
            CategoryType = "Expense"
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) =>
            {
                c.Id = 1;
                c.CreatedAt = DateTime.UtcNow;
                return c;
            });


        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CategoryType.Should().Be("Expense");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingCategory_ShouldUpdateSuccessfully()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "旧名称",
            CategoryType = CategoryType.Income,
            IsActive = true
        };

        var request = new UpdateCategoryRequest
        {
            Name = "新名称",
            Description = "更新后的描述",
            IsActive = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        var result = await _service.UpdateAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("新名称");
        result.Description.Should().Be("更新后的描述");
        result.IsActive.Should().BeFalse();
        category.Name.Should().Be("新名称");
        AuditLogServiceMock.Verify(a => a.LogAsync("Update", "Category", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingCategory_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { Name = "新名称", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(999, request));
    }

    [Fact]
    public async Task UpdateAsync_WithValidParentId_ShouldUpdateParent()
    {
        // Arrange
        var category = new Category
        {
            Id = 2,
            Name = "子分类",
            CategoryType = CategoryType.Income,
            ParentId = null
        };

        var request = new UpdateCategoryRequest
        {
            Name = "子分类",
            ParentId = 1,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(category);

        // Act
        var result = await _service.UpdateAsync(2, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(1);
        category.ParentId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingParent_ShouldSetParentIdDirectly()
    {
        // Arrange - UpdateAsync no longer validates parent existence
        var category = new Category { Id = 2, Name = "子分类", CategoryType = CategoryType.Income };
        var request = new UpdateCategoryRequest
        {
            Name = "子分类",
            ParentId = 999,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(category);

        // Act
        var result = await _service.UpdateAsync(2, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(999);
    }

    [Fact]
    public async Task UpdateAsync_WithSelfAsParent_ShouldSetParentIdDirectly()
    {
        // Arrange - UpdateAsync no longer validates self-reference
        var category = new Category { Id = 1, Name = "分类", CategoryType = CategoryType.Income };
        var request = new UpdateCategoryRequest
        {
            Name = "分类",
            ParentId = 1,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        var result = await _service.UpdateAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_RemovingParent_ShouldSetParentIdToNull()
    {
        // Arrange
        var category = new Category
        {
            Id = 2,
            Name = "子分类",
            CategoryType = CategoryType.Income,
            ParentId = 1
        };

        var request = new UpdateCategoryRequest
        {
            Name = "子分类",
            ParentId = null,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(category);

        // Act
        var result = await _service.UpdateAsync(2, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().BeNull();
        category.ParentId.Should().BeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingCategory_ShouldDeleteSuccessfully()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "待删除分类", CategoryType = CategoryType.Income };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        AuditLogServiceMock.Verify(a => a.LogAsync("Delete", "Category", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferencedCategory_ShouldDeactivateAndDisableRules()
    {
        var category = new Category { Id = 1, Name = "测试分类", CategoryType = CategoryType.Income, IsActive = true };
        var rules = new List<ClassificationRule>
        {
            new() { Id = 10, CategoryId = 1, IsActive = true, RuleName = "规则1", MatchValue = "A" },
            new() { Id = 11, CategoryId = 1, IsActive = true, RuleName = "规则2", MatchValue = "B" }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _referenceGuardMock.Setup(g => g.HasCategoryReferencesAsync(1)).ReturnsAsync(true);
        _ruleRepositoryMock.Setup(r => r.GetQueryable()).Returns(rules.AsQueryable().BuildMock().Object);

        await _service.DeleteAsync(1);

        category.IsActive.Should().BeFalse();
        rules.Should().OnlyContain(r => !r.IsActive);
        _repositoryMock.Verify(r => r.Update(category), Times.Once);
        _ruleRepositoryMock.Verify(r => r.Update(It.IsAny<ClassificationRule>()), Times.Exactly(2));
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Category", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingCategory_ShouldThrowNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999));
    }

    #endregion

    #region GetActiveCategoriesAsync Tests

    [Fact]
    public async Task GetActiveCategoriesAsync_ShouldReturnOnlyActiveCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "活跃分类1", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "非活跃分类", CategoryType = CategoryType.Expense, IsActive = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "活跃分类2", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetActiveCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.IsActive).Should().BeTrue();
        result.Should().Contain(c => c.Name == "活跃分类1");
        result.Should().Contain(c => c.Name == "活跃分类2");
        result.Should().NotContain(c => c.Name == "非活跃分类");
    }

    [Fact]
    public async Task GetActiveCategoriesAsync_WithNoActiveCategories_ShouldReturnEmptyList()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "非活跃分类1", CategoryType = CategoryType.Income, IsActive = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "非活跃分类2", CategoryType = CategoryType.Expense, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetActiveCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveCategoriesAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        var categories = new List<Category>();
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetActiveCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetCategoriesByTypeAsync Tests

    [Fact]
    public async Task GetCategoriesByTypeAsync_WithIncomeType_ShouldReturnOnlyIncomeCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "投资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _service.GetCategoriesByTypeAsync("Income");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.CategoryType == "Income").Should().BeTrue();
        result.Should().Contain(c => c.Name == "工资收入");
        result.Should().Contain(c => c.Name == "投资收入");
    }

    [Fact]
    public async Task GetCategoriesByTypeAsync_WithExpenseType_ShouldReturnOnlyExpenseCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "差旅费", CategoryType = CategoryType.Expense, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _service.GetCategoriesByTypeAsync("Expense");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.CategoryType == "Expense").Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoriesByTypeAsync_WithInvalidType_ShouldThrowValidationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.GetCategoriesByTypeAsync("InvalidType"));
        exception.Message.Should().Contain("Invalid category type");
    }

    [Fact]
    public async Task GetCategoriesByTypeAsync_WithCaseInsensitiveType_ShouldWork()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _service.GetCategoriesByTypeAsync("income");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].CategoryType.Should().Be("Income");
    }

    [Fact]
    public async Task GetCategoriesByTypeAsync_WithNoMatchingType_ShouldReturnEmptyList()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _service.GetCategoriesByTypeAsync("Expense");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Hierarchy Tests

    [Fact]
    public async Task CreateAsync_WithThreeLevelHierarchy_ShouldWork()
    {
        // Arrange - 创建三级分类：收入 > 工资收入 > 基本工资
        var request = new CreateCategoryRequest
        {
            Name = "基本工资",
            CategoryType = "Income",
            ParentId = 2
        };

        var parentCategory = new Category { Id = 2, Name = "工资收入", CategoryType = CategoryType.Income };
        _repositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(parentCategory);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) =>
            {
                c.Id = 3;
                c.CreatedAt = DateTime.UtcNow;
                return c;
            });


        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(2);
        result.Name.Should().Be("基本工资");
    }

    [Fact]
    public async Task GetByIdAsync_WithMultipleLevelHierarchy_ShouldShowImmediateParentOnly()
    {
        // Arrange
        var grandParent = new Category { Id = 1, Name = "收入", CategoryType = CategoryType.Income };
        var parent = new Category { Id = 2, Name = "工资收入", CategoryType = CategoryType.Income, ParentId = 1, Parent = grandParent };
        var category = new Category
        {
            Id = 3,
            Name = "基本工资",
            CategoryType = CategoryType.Income,
            ParentId = 2,
            Parent = parent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var categories = new List<Category> { grandParent, parent, category };
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetByIdAsync(3);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(2);
        result.ParentName.Should().Be("工资收入");
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_InactiveCategory_NoActiveRules_ShouldSkip()
    {
        // Arrange — 已停用 + 无活跃规则 → 直接返回，不调用 SaveChanges
        var category = new Category { Id = 1, Name = "已停用分类", CategoryType = CategoryType.Income, IsActive = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _referenceGuardMock.Setup(g => g.HasCategoryReferencesAsync(1)).ReturnsAsync(true);

        // 模拟无活跃规则
        var rules = new List<ClassificationRule>();
        _ruleRepositoryMock.Setup(r => r.GetQueryable()).Returns(rules.AsQueryable().BuildMock().Object);

        // Act
        await _service.DeleteAsync(1);

        // Assert — 不应调用 SaveChanges、不应调用 Archive 审计、不应删除/更新分类
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
        category.IsActive.Should().BeFalse(); // 保持不变
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_InactiveCategory_HasActiveRules_ShouldDeactivateRules()
    {
        // Arrange — 已停用 + 有活跃规则 → 只停用规则，不停用分类，不记 Archive 审计
        var category = new Category { Id = 1, Name = "已停用分类", CategoryType = CategoryType.Expense, IsActive = false };
        var rules = new List<ClassificationRule>
        {
            new() { Id = 10, CategoryId = 1, IsActive = true, RuleName = "规则A", MatchValue = "X" },
            new() { Id = 11, CategoryId = 1, IsActive = true, RuleName = "规则B", MatchValue = "Y" }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _referenceGuardMock.Setup(g => g.HasCategoryReferencesAsync(1)).ReturnsAsync(true);
        _ruleRepositoryMock.Setup(r => r.GetQueryable()).Returns(rules.AsQueryable().BuildMock().Object);

        // Act
        await _service.DeleteAsync(1);

        // Assert — 规则被停用
        rules.Should().OnlyContain(r => !r.IsActive);
        _ruleRepositoryMock.Verify(r => r.Update(It.IsAny<ClassificationRule>()), Times.Exactly(2));
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 分类本身不应被更新（已经是停用状态）
        _repositoryMock.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
        category.IsActive.Should().BeFalse(); // 保持不变

        // 不应记录 Archive 审计日志（只是停用了残留规则）
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ActiveCategory_ShouldArchive()
    {
        // Arrange — 未停用 + 无活跃规则 → 停用分类 + 审计日志（Archive），不停用规则
        var category = new Category { Id = 1, Name = "活跃分类", CategoryType = CategoryType.Income, IsActive = true };
        var rules = new List<ClassificationRule>(); // 无活跃规则

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _referenceGuardMock.Setup(g => g.HasCategoryReferencesAsync(1)).ReturnsAsync(true);
        _ruleRepositoryMock.Setup(r => r.GetQueryable()).Returns(rules.AsQueryable().BuildMock().Object);

        // Act
        await _service.DeleteAsync(1);

        // Assert — 分类被停用
        category.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.Update(category), Times.Once);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 应记录 Archive 审计日志
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Category", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);

        // 不应有规则被更新，不应物理删除
        _ruleRepositoryMock.Verify(r => r.Update(It.IsAny<ClassificationRule>()), Times.Never);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ActiveCategory_WithActiveRules_ShouldDeactivateBoth()
    {
        // Arrange — 未停用 + 有活跃规则 → 同时停用分类和规则 + 审计日志（Archive）
        var category = new Category { Id = 1, Name = "活跃分类", CategoryType = CategoryType.Expense, IsActive = true };
        var rules = new List<ClassificationRule>
        {
            new() { Id = 20, CategoryId = 1, IsActive = true, RuleName = "规则1", MatchValue = "M1" },
            new() { Id = 21, CategoryId = 1, IsActive = true, RuleName = "规则2", MatchValue = "M2" },
            new() { Id = 22, CategoryId = 1, IsActive = true, RuleName = "规则3", MatchValue = "M3" }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _referenceGuardMock.Setup(g => g.HasCategoryReferencesAsync(1)).ReturnsAsync(true);
        _ruleRepositoryMock.Setup(r => r.GetQueryable()).Returns(rules.AsQueryable().BuildMock().Object);

        // Act
        await _service.DeleteAsync(1);

        // Assert — 分类被停用
        category.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.Update(category), Times.Once);

        // 所有规则被停用
        rules.Should().OnlyContain(r => !r.IsActive);
        _ruleRepositoryMock.Verify(r => r.Update(It.IsAny<ClassificationRule>()), Times.Exactly(3));

        // SaveChanges 和 Archive 审计
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Category", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);

        // 不应物理删除
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "工资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "办公费用", CategoryType = CategoryType.Expense, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "投资收入", CategoryType = CategoryType.Income, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Name = "差旅费", CategoryType = CategoryType.Expense, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(4);
        result.IncomeCategoryCount.Should().Be(2);
        result.ExpenseCategoryCount.Should().Be(2);
        result.ActiveCount.Should().Be(3);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyData_ShouldReturnZeros()
    {
        // Arrange
        var categories = new List<Category>();
        var queryableMock = categories.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.IncomeCategoryCount.Should().Be(0);
        result.ExpenseCategoryCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
    }

    #endregion
}
