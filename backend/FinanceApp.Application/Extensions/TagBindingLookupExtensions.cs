using FinanceApp.Application.Modules.MasterData.DTOs.Tag;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace FinanceApp.Application.Extensions;

public static class TagBindingLookupExtensions
{
    public static async Task ApplyTagAsync<TDto>(
        this IQueryable<TagBinding>? tagBindings,
        TagScope scope,
        TDto item,
        Func<TDto, long> ownerIdSelector,
        Action<TDto, List<TagItemDto>> tagSetter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(ownerIdSelector);
        ArgumentNullException.ThrowIfNull(tagSetter);

        var ownerId = ownerIdSelector(item);
        var tags = await tagBindings.GetTagsAsync(scope, ownerId, cancellationToken);
        tagSetter(item, tags);
    }

    public static async Task ApplyTagsAsync<TDto>(
        this IQueryable<TagBinding>? tagBindings,
        TagScope scope,
        IEnumerable<TDto> items,
        Func<TDto, long> ownerIdSelector,
        Action<TDto, List<TagItemDto>> tagSetter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(ownerIdSelector);
        ArgumentNullException.ThrowIfNull(tagSetter);

        var itemList = items as IList<TDto> ?? items.ToList();
        if (itemList.Count == 0)
        {
            return;
        }

        var tagLookup = await tagBindings.BuildTagLookupAsync(
            scope,
            itemList.Select(ownerIdSelector),
            cancellationToken);

        foreach (var item in itemList)
        {
            tagSetter(item, tagLookup.GetTagsOrEmpty(ownerIdSelector(item)));
        }
    }

    public static async Task<List<TagItemDto>> GetTagsAsync(
        this IQueryable<TagBinding>? tagBindings,
        TagScope scope,
        long ownerId,
        CancellationToken cancellationToken = default)
    {
        if (tagBindings == null)
        {
            return new List<TagItemDto>();
        }

        var query = tagBindings
            .Where(binding =>
                !binding.IsDeleted &&
                binding.OwnerType == scope &&
                binding.OwnerId == ownerId &&
                binding.Tag != null &&
                !binding.Tag.IsDeleted)
            .OrderBy(binding => binding.Tag!.SortOrder)
            .ThenBy(binding => binding.Tag!.Name)
            .Select(binding => new TagItemDto
            {
                TagId = binding.TagId,
                TagName = binding.Tag!.Name,
                TagColor = binding.Tag.Color
            });

        return query.Provider is IAsyncQueryProvider
            ? await query.ToListAsync(cancellationToken)
            : query.ToList();
    }

    public static async Task<Dictionary<long, List<TagItemDto>>> BuildTagLookupAsync(
        this IQueryable<TagBinding>? tagBindings,
        TagScope scope,
        IEnumerable<long> ownerIds,
        CancellationToken cancellationToken = default)
    {
        if (tagBindings == null)
        {
            return new Dictionary<long, List<TagItemDto>>();
        }

        var distinctOwnerIds = ownerIds
            .Distinct()
            .ToList();

        if (distinctOwnerIds.Count == 0)
        {
            return new Dictionary<long, List<TagItemDto>>();
        }

        var query = tagBindings
            .Where(binding =>
                !binding.IsDeleted &&
                binding.OwnerType == scope &&
                distinctOwnerIds.Contains(binding.OwnerId) &&
                binding.Tag != null &&
                !binding.Tag.IsDeleted)
            .OrderBy(binding => binding.OwnerId)
            .ThenBy(binding => binding.Tag!.SortOrder)
            .ThenBy(binding => binding.Tag!.Name)
            .Select(binding => new
            {
                binding.OwnerId,
                Tag = new TagItemDto
                {
                    TagId = binding.TagId,
                    TagName = binding.Tag!.Name,
                    TagColor = binding.Tag.Color
                }
            });

        var tagRows = query.Provider is IAsyncQueryProvider
            ? await query.ToListAsync(cancellationToken)
            : query.ToList();

        return tagRows
            .GroupBy(row => row.OwnerId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => row.Tag).ToList());
    }

    public static List<TagItemDto> GetTagsOrEmpty(
        this IReadOnlyDictionary<long, List<TagItemDto>>? tagLookup,
        long ownerId)
    {
        return tagLookup != null && tagLookup.TryGetValue(ownerId, out var tags)
            ? new List<TagItemDto>(tags)
            : new List<TagItemDto>();
    }
}
