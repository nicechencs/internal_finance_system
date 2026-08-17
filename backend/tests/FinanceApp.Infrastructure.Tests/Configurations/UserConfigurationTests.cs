using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Tests.Configurations;

public class UserConfigurationTests : IDisposable
{
    private readonly AppDbContext _context;

    public UserConfigurationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
    }

    [Fact]
    public void UserConfiguration_ShouldMapToCorrectTable()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("users");
    }

    [Fact]
    public void UserConfiguration_ShouldHaveCorrectPrimaryKey()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));
        var primaryKey = entityType!.FindPrimaryKey();

        // Assert
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
    }

    [Fact]
    public void UserConfiguration_ShouldHaveCorrectColumnMappings()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));

        // Assert
        entityType!.FindProperty("Id")!.GetColumnName().Should().Be("id");
        entityType.FindProperty("Username")!.GetColumnName().Should().Be("username");
        entityType.FindProperty("PasswordHash")!.GetColumnName().Should().Be("password_hash");
        entityType.FindProperty("FullName")!.GetColumnName().Should().Be("full_name");
        entityType.FindProperty("Email")!.GetColumnName().Should().Be("email");
        entityType.FindProperty("Role")!.GetColumnName().Should().Be("role");
        entityType.FindProperty("IsActive")!.GetColumnName().Should().Be("is_active");
        entityType.FindProperty("CreatedAt")!.GetColumnName().Should().Be("created_at");
        entityType.FindProperty("UpdatedAt")!.GetColumnName().Should().Be("updated_at");
        entityType.FindProperty("DeletedAt")!.GetColumnName().Should().Be("deleted_at");
        entityType.FindProperty("IsDeleted")!.GetColumnName().Should().Be("is_deleted");
    }

    [Fact]
    public void UserConfiguration_ShouldHaveCorrectMaxLengths()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));

        // Assert
        entityType!.FindProperty("Username")!.GetMaxLength().Should().Be(50);
        entityType.FindProperty("PasswordHash")!.GetMaxLength().Should().Be(255);
        entityType.FindProperty("FullName")!.GetMaxLength().Should().Be(100);
        entityType.FindProperty("Email")!.GetMaxLength().Should().Be(100);
        // Role is now UserRole enum, no max length constraint
        entityType.FindProperty("Role").Should().NotBeNull();
    }

    [Fact]
    public void UserConfiguration_ShouldHaveCorrectRequiredFields()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));

        // Assert
        entityType!.FindProperty("Username")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("PasswordHash")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("FullName")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("Role")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("IsActive")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("CreatedAt")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("UpdatedAt")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("IsDeleted")!.IsNullable.Should().BeFalse();

        // Email 和 DeletedAt 可以为空
        entityType.FindProperty("Email")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("DeletedAt")!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void UserConfiguration_ShouldHaveIndexes()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));
        var indexes = entityType!.GetIndexes().ToList();

        // Assert
        indexes.Should().NotBeEmpty();

        // 应该有 Username 索引
        var usernameIndexes = indexes.Where(i =>
            i.Properties.Count == 1 &&
            i.Properties.First().Name == "Username").ToList();
        usernameIndexes.Should().NotBeEmpty();
    }

    [Fact]
    public void UserConfiguration_ShouldHaveUniqueConstraintOnUsername()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(User));
        var indexes = entityType!.GetIndexes();

        // Assert
        var uniqueIndex = indexes.FirstOrDefault(i =>
            i.IsUnique &&
            i.Properties.Count == 1 &&
            i.Properties.First().Name == "Username");

        uniqueIndex.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

