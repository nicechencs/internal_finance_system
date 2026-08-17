using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using System.Linq.Expressions;

namespace FinanceApp.Application.Tests.Common;

public class SortingHelperTests
{
    private class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private static readonly Dictionary<string, Expression<Func<TestEntity, object>>> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = e => e.Name,
        ["amount"] = e => e.Amount,
        ["createdAt"] = e => e.CreatedAt
    };

    private static IQueryable<TestEntity> CreateTestData()
    {
        return new List<TestEntity>
        {
            new() { Name = "Charlie", Amount = 300, CreatedAt = new DateTime(2025, 1, 3) },
            new() { Name = "Alice", Amount = 100, CreatedAt = new DateTime(2025, 1, 1) },
            new() { Name = "Bob", Amount = 200, CreatedAt = new DateTime(2025, 1, 2) }
        }.AsQueryable();
    }

    [Fact]
    public void ApplySorting_WithNullSortBy_ReturnsOriginalQuery()
    {
        var query = CreateTestData().OrderBy(e => e.CreatedAt);

        var result = SortingHelper.ApplySorting(query, null, null, SortableFields);

        result.First().Name.Should().Be("Alice");
    }

    [Fact]
    public void ApplySorting_WithEmptySortBy_ReturnsOriginalQuery()
    {
        var query = CreateTestData().OrderBy(e => e.CreatedAt);

        var result = SortingHelper.ApplySorting(query, "", "asc", SortableFields);

        result.First().Name.Should().Be("Alice");
    }

    [Fact]
    public void ApplySorting_WithInvalidField_ReturnsOriginalQuery()
    {
        var query = CreateTestData().OrderBy(e => e.CreatedAt);

        var result = SortingHelper.ApplySorting(query, "invalidField", "asc", SortableFields);

        result.First().Name.Should().Be("Alice");
    }

    [Fact]
    public void ApplySorting_ByNameAsc_SortsCorrectly()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "name", "asc", SortableFields).ToList();

        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public void ApplySorting_ByNameDesc_SortsCorrectly()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "name", "desc", SortableFields).ToList();

        result[0].Name.Should().Be("Charlie");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Alice");
    }

    [Fact]
    public void ApplySorting_ByAmountAsc_SortsCorrectly()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "amount", "asc", SortableFields).ToList();

        result[0].Amount.Should().Be(100);
        result[1].Amount.Should().Be(200);
        result[2].Amount.Should().Be(300);
    }

    [Fact]
    public void ApplySorting_ByAmountDesc_SortsCorrectly()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "amount", "desc", SortableFields).ToList();

        result[0].Amount.Should().Be(300);
        result[1].Amount.Should().Be(200);
        result[2].Amount.Should().Be(100);
    }

    [Fact]
    public void ApplySorting_CaseInsensitive_SortsCorrectly()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "Name", "ASC", SortableFields).ToList();

        result[0].Name.Should().Be("Alice");
        result[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public void ApplySorting_DefaultSortOrderIsAsc()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "amount", null, SortableFields).ToList();

        result[0].Amount.Should().Be(100);
        result[2].Amount.Should().Be(300);
    }

    [Fact]
    public void ApplySorting_ByCreatedAtDesc_SortsCorrectly()
    {
        var query = CreateTestData();

        var result = SortingHelper.ApplySorting(query, "createdAt", "desc", SortableFields).ToList();

        result[0].CreatedAt.Should().Be(new DateTime(2025, 1, 3));
        result[2].CreatedAt.Should().Be(new DateTime(2025, 1, 1));
    }
}
