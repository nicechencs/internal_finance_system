using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Tests.Common;

public class TagQueryExtensionsTests
{
    private sealed class TestOwner : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void ApplyOwnerTagFilter_WithDuplicateTagIdsInAndMode_DeduplicatesBeforeMatching()
    {
        var owners = new List<TestOwner>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        }.AsQueryable();

        var tagBindings = new List<TagBinding>
        {
            new() { Id = 1, OwnerType = TagScope.Customer, OwnerId = 1, TagId = 101 },
            new() { Id = 2, OwnerType = TagScope.Customer, OwnerId = 1, TagId = 102 },
            new() { Id = 3, OwnerType = TagScope.Customer, OwnerId = 2, TagId = 101 }
        }.AsQueryable();

        var filter = new TagFilterGroup
        {
            Scope = TagScope.Customer,
            TagIds = new List<long> { 101, 101, 102 },
            MatchMode = TagMatchMode.And
        };

        var result = owners
            .ApplyOwnerTagFilter(tagBindings, filter)
            .Select(owner => owner.Id)
            .ToList();

        result.Should().Equal(1);
    }

    [Fact]
    public void ApplyOwnerTagFilters_WithMultipleGroupsOfSameScope_AppliesGroupIntersection()
    {
        var owners = new List<TestOwner>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        }.AsQueryable();

        var tagBindings = new List<TagBinding>
        {
            new() { Id = 1, OwnerType = TagScope.Customer, OwnerId = 1, TagId = 101 },
            new() { Id = 2, OwnerType = TagScope.Customer, OwnerId = 1, TagId = 102 },
            new() { Id = 3, OwnerType = TagScope.Customer, OwnerId = 2, TagId = 101 },
            new() { Id = 4, OwnerType = TagScope.Customer, OwnerId = 3, TagId = 102 },
            new() { Id = 5, OwnerType = TagScope.Project, OwnerId = 999, TagId = 201 }
        }.AsQueryable();

        var filters = new List<TagFilterGroup>
        {
            new() { Scope = TagScope.Customer, TagIds = new List<long> { 101 }, MatchMode = TagMatchMode.Or },
            new() { Scope = TagScope.Customer, TagIds = new List<long> { 102 }, MatchMode = TagMatchMode.Or },
            new() { Scope = TagScope.Project, TagIds = new List<long> { 201 }, MatchMode = TagMatchMode.Or }
        };

        var result = owners
            .ApplyOwnerTagFilters(tagBindings, filters, TagScope.Customer)
            .Select(owner => owner.Id)
            .ToList();

        result.Should().Equal(1);
    }
}
