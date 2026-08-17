using FinanceApp.Application.Modules.MasterData.DTOs.Tag;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ITagService
{
    Task<List<TagDto>> GetTagsAsync(string scope, bool? isActive = null);
    Task<TagDto> GetByIdAsync(long id);
    Task<TagDto> CreateAsync(CreateTagRequest request);
    Task<TagDto> UpdateAsync(long id, UpdateTagRequest request);
    Task DeleteAsync(long id);
    Task<List<TagBindingDto>> GetBindingsAsync(string ownerType, long ownerId);
    Task SetBindingsAsync(SetBindingsRequest request);
    Task AddBindingAsync(string ownerType, long ownerId, long tagId);
    Task RemoveBindingAsync(string ownerType, long ownerId, long tagId);
    Task BatchSetBindingsAsync(BatchSetBindingsRequest request);
}
