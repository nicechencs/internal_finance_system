using FluentAssertions;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Tests.Entities;

// 创建一个测试用的具体实体类
public class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().Be(0);
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BaseEntity_ShouldAllowSettingId()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.Id = 123;

        // Assert
        entity.Id.Should().Be(123);
    }

    [Fact]
    public void BaseEntity_ShouldSupportSoftDelete()
    {
        // Arrange
        var entity = new TestEntity { Name = "测试实体" };
        var deleteTime = DateTime.UtcNow;

        // Act
        entity.IsDeleted = true;
        entity.DeletedAt = deleteTime;

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deleteTime);
    }

    [Fact]
    public void BaseEntity_ShouldTrackCreationTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = new TestEntity();
        var afterCreation = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void BaseEntity_ShouldTrackUpdateTime()
    {
        // Arrange
        var entity = new TestEntity();
        var originalUpdateTime = entity.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10); // 确保时间差异
        entity.UpdatedAt = DateTime.UtcNow;

        // Assert
        entity.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public void BaseEntity_DeletedAt_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.DeletedAt.Should().BeNull();
    }
}