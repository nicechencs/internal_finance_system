using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class RuleServiceTests : TestBase
{
    private readonly Mock<IRepository<ClassificationRule>> _ruleRepositoryMock;
    private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
    private readonly RuleService _service;

    public RuleServiceTests()
    {
        _ruleRepositoryMock = new Mock<IRepository<ClassificationRule>>();
        _categoryRepositoryMock = new Mock<IRepository<Category>>();
        _service = new RuleService(
            _ruleRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            Mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<RuleService>>(),
            Mock.Of<IAuditLogService>(),
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateRule()
    {
        // Arrange
        var categoryId = 1L;
        var category = new Category { Id = categoryId, Name = "测试分类", CategoryType = CategoryType.Expense };

        var request = new CreateRuleRequest
        {
            Name = "测试规则",
            CategoryId = categoryId,
            MatchField = "CounterpartyName",
            MatchOperator = "Contains",
            MatchValue = "阿里云",
            Priority = 100
        };

        _categoryRepositoryMock.Setup(x => x.ExistsAsync(categoryId))
            .ReturnsAsync(true);

        _ruleRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClassificationRule>()))
            .ReturnsAsync((ClassificationRule r) => { r.Id = 1; return r; });

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("测试规则");
        result.CategoryId.Should().Be(categoryId);
        result.MatchField.Should().Be("CounterpartyName");
        result.MatchOperator.Should().Be("Contains");
        result.MatchValue.Should().Be("阿里云");
        result.Priority.Should().Be(100);
        result.IsActive.Should().BeTrue();
        _ruleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ClassificationRule>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCategoryId_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateRuleRequest
        {
            Name = "测试规则",
            CategoryId = 999L,
            MatchField = "CounterpartyName",
            MatchOperator = "Contains",
            MatchValue = "测试"
        };

        _categoryRepositoryMock.Setup(x => x.ExistsAsync(999L))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidMatchField_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateRuleRequest
        {
            Name = "测试规则",
            CategoryId = 1L,
            MatchField = "InvalidField",
            MatchOperator = "Contains",
            MatchValue = "测试"
        };

        _categoryRepositoryMock.Setup(x => x.ExistsAsync(1L))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidMatchOperator_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateRuleRequest
        {
            Name = "测试规则",
            CategoryId = 1L,
            MatchField = "CounterpartyName",
            MatchOperator = "InvalidOperator",
            MatchValue = "测试"
        };

        _categoryRepositoryMock.Setup(x => x.ExistsAsync(1L))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRegex_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateRuleRequest
        {
            Name = "测试规则",
            CategoryId = 1L,
            MatchField = "CounterpartyName",
            MatchOperator = "Regex",
            MatchValue = "[invalid(regex" // 无效的正则表达式
        };

        _categoryRepositoryMock.Setup(x => x.ExistsAsync(1L))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithValidRegex_ShouldCreateRule()
    {
        // Arrange
        var request = new CreateRuleRequest
        {
            Name = "测试规则",
            CategoryId = 1L,
            MatchField = "CounterpartyName",
            MatchOperator = "Regex",
            MatchValue = "^阿里.*$"
        };

        _categoryRepositoryMock.Setup(x => x.ExistsAsync(1L))
            .ReturnsAsync(true);

        _ruleRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClassificationRule>()))
            .ReturnsAsync((ClassificationRule r) => { r.Id = 1; return r; });

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(new Category { Id = 1, Name = "测试", CategoryType = CategoryType.Expense });

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.MatchOperator.Should().Be("Regex");
        _ruleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ClassificationRule>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_WithSortByPriorityDesc_ShouldApplyRequestedOrder()
    {
        var rules = new List<ClassificationRule>
        {
            new() { Id = 1, RuleName = "规则1", Priority = 10, IsActive = true, CategoryId = 1, Category = new Category { Id = 1, Name = "分类A", CategoryType = CategoryType.Expense }, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "A", CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { Id = 2, RuleName = "规则2", Priority = 80, IsActive = true, CategoryId = 2, Category = new Category { Id = 2, Name = "分类B", CategoryType = CategoryType.Expense }, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "B", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new() { Id = 3, RuleName = "规则3", Priority = 50, IsActive = true, CategoryId = 3, Category = new Category { Id = 3, Name = "分类C", CategoryType = CategoryType.Expense }, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "C", CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = rules.AsQueryable().BuildMock();
        _ruleRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetPagedAsync(new PageRequest
        {
            Page = 1,
            PageSize = 10,
            SortBy = "priority",
            SortOrder = "desc"
        });

        result.Total.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items.Select(x => x.Id).Should().ContainInOrder(2, 3, 1);
        result.Items[0].CategoryName.Should().Be("分类B");
    }

    [Fact]
    public async Task GetActiveRulesAsync_ShouldReturnRulesOrderedByPriority()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new() { Id = 1, RuleName = "规则1", Priority = 50, IsActive = true, CategoryId = 1, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "测试1" },
            new() { Id = 2, RuleName = "规则2", Priority = 100, IsActive = true, CategoryId = 1, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "测试2" },
            new() { Id = 3, RuleName = "规则3", Priority = 75, IsActive = true, CategoryId = 1, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "测试3" },
            new() { Id = 4, RuleName = "规则4", Priority = 100, IsActive = false, CategoryId = 1, MatchField = RuleMatchField.CounterpartyName, MatchOperator = RuleMatchOperator.Contains, MatchValue = "测试4" }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.GetActiveRulesAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Priority.Should().Be(100);
        result[0].Id.Should().Be(2);
        result[1].Priority.Should().Be(75);
        result[2].Priority.Should().Be(50);
    }

    [Fact]
    public async Task MatchCategoryAsync_WithContainsOperator_ShouldMatchCorrectly()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "阿里云规则",
                CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Contains,
                MatchValue = "阿里云",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.MatchCategoryAsync("阿里云计算有限公司", "服务器费用", 1000m);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task MatchCategoryAsync_WithEqualsOperator_ShouldMatchCorrectly()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "精确匹配规则",
                CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Equals,
                MatchValue = "阿里云",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var exactMatch = await _service.MatchCategoryAsync("阿里云", "测试", 1000m);
        var partialMatch = await _service.MatchCategoryAsync("阿里云计算", "测试", 1000m);

        // Assert
        exactMatch.Should().Be(10);
        partialMatch.Should().BeNull();
    }

    [Fact]
    public async Task MatchCategoryAsync_WithRegexOperator_ShouldMatchCorrectly()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "正则规则",
                CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Regex,
                MatchValue = "^阿里.*$",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var match1 = await _service.MatchCategoryAsync("阿里云", "测试", 1000m);
        var match2 = await _service.MatchCategoryAsync("阿里巴巴", "测试", 1000m);
        var noMatch = await _service.MatchCategoryAsync("腾讯云", "测试", 1000m);

        // Assert
        match1.Should().Be(10);
        match2.Should().Be(10);
        noMatch.Should().BeNull();
    }

    [Fact]
    public async Task MatchCategoryAsync_WithMultipleRules_ShouldReturnHighestPriority()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "低优先级规则",
                CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Contains,
                MatchValue = "云",
                Priority = 50,
                IsActive = true
            },
            new()
            {
                Id = 2,
                RuleName = "高优先级规则",
                CategoryId = 20,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Contains,
                MatchValue = "阿里",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.MatchCategoryAsync("阿里云", "测试", 1000m);

        // Assert
        result.Should().Be(20); // 应该匹配高优先级规则
    }

    [Fact]
    public async Task MatchCategoryAsync_WithDescriptionField_ShouldMatchCorrectly()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "描述匹配规则",
                CategoryId = 10,
                MatchField = RuleMatchField.Description,
                MatchOperator = RuleMatchOperator.Contains,
                MatchValue = "服务器",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.MatchCategoryAsync("阿里云", "服务器租赁费用", 1000m);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task MatchCategoryAsync_WithAmountField_ShouldMatchCorrectly()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "金额匹配规则",
                CategoryId = 10,
                MatchField = RuleMatchField.Amount,
                MatchOperator = RuleMatchOperator.Equals,
                MatchValue = "1000",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.MatchCategoryAsync("测试", "测试", 1000m);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task MatchCategoryAsync_WithNoMatchingRules_ShouldReturnNull()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "测试规则",
                CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Contains,
                MatchValue = "阿里云",
                Priority = 100,
                IsActive = true
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.MatchCategoryAsync("腾讯云", "测试", 1000m);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MatchCategoryAsync_WithInactiveRule_ShouldNotMatch()
    {
        // Arrange
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1,
                RuleName = "未激活规则",
                CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = RuleMatchOperator.Contains,
                MatchValue = "阿里云",
                Priority = 100,
                IsActive = false
            }
        };

        _ruleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _service.MatchCategoryAsync("阿里云", "测试", 1000m);

        // Assert
        result.Should().BeNull();
    }

    // ─────────────────────── 新增：通过 helper 联通的 Range / Amount / StartsWith / EndsWith ───────────────────────

    [Theory]
    [InlineData(RuleMatchOperator.StartsWith, "阿里", "阿里云", true)]
    [InlineData(RuleMatchOperator.StartsWith, "云", "阿里云", false)]
    [InlineData(RuleMatchOperator.EndsWith, "云", "阿里云", true)]
    [InlineData(RuleMatchOperator.EndsWith, "阿里", "阿里云", false)]
    public async Task MatchCategoryAsync_StringOperators_WorkAsExpected(
        RuleMatchOperator op, string ruleValue, string counterparty, bool expectedMatch)
    {
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1, RuleName = "test", CategoryId = 10,
                MatchField = RuleMatchField.CounterpartyName,
                MatchOperator = op,
                MatchValue = ruleValue,
                IsActive = true
            }
        };
        _ruleRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rules);

        var result = await _service.MatchCategoryAsync(counterparty, string.Empty, 0m);

        (result == 10).Should().Be(expectedMatch);
    }

    [Theory]
    [InlineData("100.5", 100.5, true)]
    [InlineData("100.50", 100.5, true)]   // 精度等价
    [InlineData("100", 100.5, false)]
    public async Task MatchCategoryAsync_AmountEquals_UsesNumericEquality(
        string ruleValue, double amount, bool expectedMatch)
    {
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1, RuleName = "test", CategoryId = 10,
                MatchField = RuleMatchField.Amount,
                MatchOperator = RuleMatchOperator.Equals,
                MatchValue = ruleValue,
                IsActive = true
            }
        };
        _ruleRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rules);

        var result = await _service.MatchCategoryAsync(string.Empty, string.Empty, (decimal)amount);

        (result == 10).Should().Be(expectedMatch);
    }

    [Theory]
    [InlineData("1000", "10000", 5000, true)]
    [InlineData("1000", "10000", 999.99, false)]
    [InlineData("1000", null, 5000, true)]      // 开放上限
    [InlineData("1000", null, 500, false)]
    public async Task MatchCategoryAsync_AmountRange_MatchesByNumericBounds(
        string min, string? max, double amount, bool expectedMatch)
    {
        var rules = new List<ClassificationRule>
        {
            new()
            {
                Id = 1, RuleName = "test", CategoryId = 10,
                MatchField = RuleMatchField.Amount,
                MatchOperator = RuleMatchOperator.Range,
                MatchValue = min,
                MatchValueMax = max,
                IsActive = true
            }
        };
        _ruleRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rules);

        var result = await _service.MatchCategoryAsync(string.Empty, string.Empty, (decimal)amount);

        (result == 10).Should().Be(expectedMatch);
    }

    [Theory]
    [InlineData("Amount", "Contains", "100", null)]
    [InlineData("Amount", "StartsWith", "100", null)]
    [InlineData("Amount", "Equals", "abc", null)]
    [InlineData("CounterpartyName", "Range", "100", null)]
    [InlineData("Amount", "Range", "100", "50")]   // max < min
    public async Task CreateAsync_InvalidFieldOperatorCombination_ShouldThrowValidation(
        string matchField, string matchOperator, string matchValue, string? matchValueMax)
    {
        var request = new CreateRuleRequest
        {
            Name = "非法组合",
            CategoryId = 1L,
            MatchField = matchField,
            MatchOperator = matchOperator,
            MatchValue = matchValue,
            MatchValueMax = matchValueMax,
            Priority = 10
        };
        _categoryRepositoryMock.Setup(x => x.ExistsAsync(1L)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }
}
