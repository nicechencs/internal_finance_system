using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Tests.Configurations;

public class PayableTypeConfigurationTests : IDisposable
{
    private readonly AppDbContext _context;

    public PayableTypeConfigurationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
    }

    [Fact]
    public void PayableTypeConfiguration_ShouldMapToCorrectTable()
    {
        var entityType = _context.Model.FindEntityType(typeof(PayableType));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("payable_types");
    }

    [Fact]
    public void PayableTypeConfiguration_ShouldMapBaseEntityColumnsToSnakeCase()
    {
        var entityType = _context.Model.FindEntityType(typeof(PayableType));

        entityType.Should().NotBeNull();
        entityType!.FindProperty("Id")!.GetColumnName().Should().Be("id");
        entityType.FindProperty("Name")!.GetColumnName().Should().Be("name");
        entityType.FindProperty("Code")!.GetColumnName().Should().Be("code");
        entityType.FindProperty("Description")!.GetColumnName().Should().Be("description");
        entityType.FindProperty("IsActive")!.GetColumnName().Should().Be("is_active");
        entityType.FindProperty("SortOrder")!.GetColumnName().Should().Be("sort_order");
        entityType.FindProperty("CreatedAt")!.GetColumnName().Should().Be("created_at");
        entityType.FindProperty("UpdatedAt")!.GetColumnName().Should().Be("updated_at");
        entityType.FindProperty("DeletedAt")!.GetColumnName().Should().Be("deleted_at");
        entityType.FindProperty("IsDeleted")!.GetColumnName().Should().Be("is_deleted");
        entityType.FindProperty("CreatedBy")!.GetColumnName().Should().Be("created_by");
    }

    [Fact]
    public void PayableTypeConfiguration_ShouldUseCreatedByInsteadOfShadowCreatedByUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(PayableType));

        entityType.Should().NotBeNull();
        entityType!.FindProperty("CreatedByUserId").Should().BeNull();

        var createdByNavigation = entityType.FindNavigation(nameof(BaseEntity.CreatedByUser));
        createdByNavigation.Should().NotBeNull();
        createdByNavigation!.ForeignKey.Properties.Should().ContainSingle();
        createdByNavigation.ForeignKey.Properties[0].Name.Should().Be("CreatedBy");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
