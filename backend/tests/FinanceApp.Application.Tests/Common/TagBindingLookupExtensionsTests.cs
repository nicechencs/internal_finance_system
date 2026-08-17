using FluentAssertions;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Application.Tests.Common;

public class TagBindingLookupExtensionsTests
{
    [Fact]
    public async Task BuildTagLookupAsync_ShouldReturnTagsGroupedByOwner()
    {
        await using var context = CreateContext();

        context.Tags.AddRange(
            new Tag { Id = 100, Scope = TagScope.Customer, Name = "tag-a", Color = "#111111", SortOrder = 2 },
            new Tag { Id = 101, Scope = TagScope.Customer, Name = "tag-b", Color = "#222222", SortOrder = 1 },
            new Tag { Id = 102, Scope = TagScope.Customer, Name = "tag-c", Color = "#333333", SortOrder = 1 },
            new Tag { Id = 103, Scope = TagScope.Supplier, Name = "supplier-tag", Color = "#444444", SortOrder = 1 },
            new Tag { Id = 104, Scope = TagScope.Customer, Name = "deleted-binding", Color = "#555555", SortOrder = 1 },
            new Tag { Id = 105, Scope = TagScope.Customer, Name = "deleted-tag", Color = "#666666", SortOrder = 1, IsDeleted = true });

        context.TagBindings.AddRange(
            new TagBinding { Id = 1, OwnerType = TagScope.Customer, OwnerId = 10, TagId = 100 },
            new TagBinding { Id = 2, OwnerType = TagScope.Customer, OwnerId = 10, TagId = 101 },
            new TagBinding { Id = 3, OwnerType = TagScope.Customer, OwnerId = 20, TagId = 102 },
            new TagBinding { Id = 4, OwnerType = TagScope.Supplier, OwnerId = 10, TagId = 103 },
            new TagBinding { Id = 5, OwnerType = TagScope.Customer, OwnerId = 30, TagId = 104, IsDeleted = true },
            new TagBinding { Id = 6, OwnerType = TagScope.Customer, OwnerId = 40, TagId = 105 });

        await context.SaveChangesAsync();

        var result = await context.TagBindings.BuildTagLookupAsync(TagScope.Customer, new[] { 10L, 20L, 30L, 40L });

        result.Keys.Should().BeEquivalentTo(new[] { 10L, 20L });
        result[10].Select(tag => tag.TagId).Should().Equal(101, 100);
        result[20].Should().ContainSingle()
            .Which.TagName.Should().Be("tag-c");
    }

    [Fact]
    public async Task ApplyTagAsync_ShouldAssignTagsForSingleItem()
    {
        await using var context = CreateContext();

        context.Tags.AddRange(
            new Tag { Id = 100, Scope = TagScope.Customer, Name = "tag-a", Color = "#111111", SortOrder = 2 },
            new Tag { Id = 101, Scope = TagScope.Customer, Name = "tag-b", Color = "#222222", SortOrder = 1 });

        context.TagBindings.AddRange(
            new TagBinding { Id = 1, OwnerType = TagScope.Customer, OwnerId = 10, TagId = 100 },
            new TagBinding { Id = 2, OwnerType = TagScope.Customer, OwnerId = 10, TagId = 101 });

        await context.SaveChangesAsync();

        var item = new TestDto { Id = 10 };

        await context.TagBindings.ApplyTagAsync(
            TagScope.Customer,
            item,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        item.Tags.Select(tag => tag.TagId).Should().Equal(101, 100);
    }

    [Fact]
    public async Task ApplyTagsAsync_ShouldAssignEmptyListWhenRepositoryQueryableIsNull()
    {
        var items = new List<TestDto>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        };

        await ((IQueryable<TagBinding>?)null).ApplyTagsAsync(
            TagScope.Customer,
            items,
            item => item.Id,
            (item, tags) => item.Tags = tags);

        items.Should().OnlyContain(item => item.Tags.Count == 0);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class TestDto
    {
        public long Id { get; set; }
        public List<TagItemDto> Tags { get; set; } = new();
    }
}
