using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Category_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var category = new Category();

        // Assert
        category.Name.Should().BeEmpty();
        category.Level.Should().Be(1);
        category.IsActive.Should().BeTrue();
        category.IsDeleted.Should().BeFalse();
        category.Children.Should().NotBeNull().And.BeEmpty();
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Category_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var category = new Category
        {
            Name = "办公费用",
            CategoryType = CategoryType.Expense,
            Level = 1,
            SortOrder = 10,
            Description = "日常办公支出",
            IsActive = true
        };

        // Assert
        category.Name.Should().Be("办公费用");
        category.CategoryType.Should().Be(CategoryType.Expense);
        category.Level.Should().Be(1);
        category.SortOrder.Should().Be(10);
        category.Description.Should().Be("日常办公支出");
        category.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(CategoryType.Income)]
    [InlineData(CategoryType.Expense)]
    public void Category_ShouldSupportDifferentCategoryTypes(CategoryType categoryType)
    {
        // Arrange & Act
        var category = new Category
        {
            CategoryType = categoryType
        };

        // Assert
        category.CategoryType.Should().Be(categoryType);
    }

    [Fact]
    public void Category_ShouldSupportHierarchicalStructure()
    {
        // Arrange
        var parentCategory = new Category
        {
            Id = 1,
            Name = "费用",
            Level = 1
        };

        var childCategory = new Category
        {
            Id = 2,
            Name = "办公费用",
            ParentId = 1,
            Level = 2,
            Parent = parentCategory
        };

        // Act
        parentCategory.Children.Add(childCategory);

        // Assert
        childCategory.ParentId.Should().Be(1);
        childCategory.Parent.Should().Be(parentCategory);
        childCategory.Level.Should().Be(2);
        parentCategory.Children.Should().Contain(childCategory);
        parentCategory.Children.Should().HaveCount(1);
    }

    [Fact]
    public void Category_ShouldSupportMultipleChildren()
    {
        // Arrange
        var parent = new Category
        {
            Id = 1,
            Name = "收入",
            Level = 1
        };

        var child1 = new Category { Id = 2, Name = "主营业务收入", ParentId = 1, Level = 2 };
        var child2 = new Category { Id = 3, Name = "其他业务收入", ParentId = 1, Level = 2 };
        var child3 = new Category { Id = 4, Name = "营业外收入", ParentId = 1, Level = 2 };

        // Act
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.Children.Add(child3);

        // Assert
        parent.Children.Should().HaveCount(3);
        parent.Children.Should().Contain(new[] { child1, child2, child3 });
    }

    [Fact]
    public void Category_ShouldAllowNullParent()
    {
        // Arrange & Act
        var rootCategory = new Category
        {
            Name = "根分类",
            ParentId = null,
            Level = 1
        };

        // Assert
        rootCategory.ParentId.Should().BeNull();
        rootCategory.Parent.Should().BeNull();
    }

    [Fact]
    public void Category_ShouldSupportSoftDelete()
    {
        // Arrange
        var category = new Category
        {
            Name = "测试分类",
            IsActive = true
        };

        // Act
        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        // Assert
        category.IsDeleted.Should().BeTrue();
        category.DeletedAt.Should().NotBeNull();
    }
}
