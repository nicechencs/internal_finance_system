using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class PersonDeliveryService : ServiceBase, IPersonDeliveryService
{
    public const int TitleMaxLength = 200;
    public const int TextMaxLength = 1000;
    public const int AcceptedByMaxLength = 100;

    private readonly IRepository<PersonDelivery> _deliveryRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PersonDeliveryService> _logger;

    public PersonDeliveryService(
        IRepository<PersonDelivery> deliveryRepository,
        IRepository<Person> personRepository,
        IRepository<Project> projectRepository,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork,
        ILogger<PersonDeliveryService> logger)
        : base(currentUserService, permissionService)
    {
        _deliveryRepository = deliveryRepository;
        _personRepository = personRepository;
        _projectRepository = projectRepository;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<PersonDeliveryDto>> GetByPersonAsync(long personId)
    {
        await GetAccessiblePersonAsync(personId);

        var items = await _deliveryRepository.GetQueryable()
            .Include(d => d.Project)
            .Where(d => d.PersonId == personId)
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<PersonDeliveryDto> CreateAsync(long personId, CreatePersonDeliveryRequest request)
    {
        var person = await GetAccessiblePersonAsync(personId);
        EnsureCanEdit(person);

        var status = ParseStatus(request.Status, PersonDeliveryStatus.Pending);
        var title = NormalizeRequiredTitle(request.Title);
        var description = NormalizeOptional(request.Description, TextMaxLength, "交付说明");
        var notes = NormalizeOptional(request.Notes, TextMaxLength, "备注");
        var acceptedBy = NormalizeOptional(request.AcceptedBy, AcceptedByMaxLength, "验收人");
        await EnsureProjectExistsAsync(request.ProjectId);

        var now = DateTime.UtcNow;
        var delivery = new PersonDelivery
        {
            PersonId = personId,
            ProjectId = request.ProjectId,
            Title = title,
            Description = description,
            Notes = notes,
            Status = status,
            AcceptedAt = status == PersonDeliveryStatus.Accepted
                ? request.AcceptedAt ?? now
                : null,
            AcceptedBy = status == PersonDeliveryStatus.Accepted
                ? acceptedBy ?? CurrentUserName()
                : null
        };

        await _deliveryRepository.AddAsync(delivery);
        await _unitOfWork.SaveChangesAsync();

        var saved = await LoadDeliveryAsync(personId, delivery.Id);
        var dto = ToDto(saved);
        await _auditLogService.LogAsync("Create", "PersonDelivery", saved.Id, null, SerializeForAudit(dto));
        _logger.LogInformation(
            "创建人员交付记录: Id={Id}, PersonId={PersonId}, Status={Status}",
            saved.Id, personId, saved.Status);
        return dto;
    }

    public async Task<PersonDeliveryDto> UpdateAsync(long personId, long id, UpdatePersonDeliveryRequest request)
    {
        var person = await GetAccessiblePersonAsync(personId);
        EnsureCanEdit(person);

        var delivery = await LoadDeliveryAsync(personId, id);
        var oldDto = ToDto(delivery);

        var status = ParseStatus(request.Status, null);
        var title = NormalizeRequiredTitle(request.Title);
        var description = NormalizeOptional(request.Description, TextMaxLength, "交付说明");
        var notes = NormalizeOptional(request.Notes, TextMaxLength, "备注");
        var acceptedBy = NormalizeOptional(request.AcceptedBy, AcceptedByMaxLength, "验收人");
        await EnsureProjectExistsAsync(request.ProjectId);

        delivery.Title = title;
        delivery.Description = description;
        delivery.Notes = notes;
        delivery.ProjectId = request.ProjectId;
        ApplyStatus(delivery, status, request.AcceptedAt, acceptedBy);

        _deliveryRepository.Update(delivery);
        await _unitOfWork.SaveChangesAsync();

        var saved = await LoadDeliveryAsync(personId, id);
        var dto = ToDto(saved);
        await _auditLogService.LogAsync("Update", "PersonDelivery", saved.Id, SerializeForAudit(oldDto), SerializeForAudit(dto));
        return dto;
    }

    public async Task DeleteAsync(long personId, long id)
    {
        var person = await GetAccessiblePersonAsync(personId);
        EnsureCanDelete(person);

        var delivery = await _deliveryRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.Id == id && d.PersonId == personId);

        if (delivery == null)
        {
            throw new NotFoundException("交付记录不存在");
        }

        var oldDto = ToDto(delivery);
        _deliveryRepository.Delete(delivery);
        await _unitOfWork.SaveChangesAsync();
        await _auditLogService.LogAsync("Delete", "PersonDelivery", id, SerializeForAudit(oldDto), null);
    }

    private async Task<Person> GetAccessiblePersonAsync(long personId)
    {
        var person = await _personRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (person == null)
        {
            throw new NotFoundException("人员不存在");
        }

        EnsureCanAccess(person);
        return person;
    }

    private async Task<PersonDelivery> LoadDeliveryAsync(long personId, long id)
    {
        var delivery = await _deliveryRepository.GetQueryable()
            .Include(d => d.Person)
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.Id == id && d.PersonId == personId);

        if (delivery == null)
        {
            throw new NotFoundException("交付记录不存在");
        }

        return delivery;
    }

    private async Task EnsureProjectExistsAsync(long? projectId)
    {
        if (projectId == null)
        {
            return;
        }

        var exists = await _projectRepository.GetQueryable()
            .AnyAsync(p => p.Id == projectId.Value);

        if (!exists)
        {
            throw new ValidationException("关联项目不存在");
        }
    }

    private void ApplyStatus(PersonDelivery delivery, PersonDeliveryStatus status, DateTime? acceptedAt, string? acceptedBy)
    {
        var wasAccepted = delivery.Status == PersonDeliveryStatus.Accepted;
        delivery.Status = status;

        if (status != PersonDeliveryStatus.Accepted)
        {
            delivery.AcceptedAt = null;
            delivery.AcceptedBy = null;
            return;
        }

        delivery.AcceptedAt = acceptedAt ?? (wasAccepted ? delivery.AcceptedAt : null) ?? DateTime.UtcNow;
        delivery.AcceptedBy = acceptedBy ?? (wasAccepted ? delivery.AcceptedBy : null) ?? CurrentUserName();
    }

    private string? CurrentUserName()
    {
        var username = CurrentUserService.Username;
        return string.IsNullOrWhiteSpace(username) ? null : username.Trim();
    }

    private static string NormalizeRequiredTitle(string? title)
    {
        var value = title?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            throw new ValidationException("交付物名称不能为空");
        }

        if (value.Length > TitleMaxLength)
        {
            throw new ValidationException($"交付物名称不能超过{TitleMaxLength}个字符");
        }

        return value;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ValidationException($"{fieldName}不能超过{maxLength}个字符");
        }

        return trimmed;
    }

    private static PersonDeliveryStatus ParseStatus(string? status, PersonDeliveryStatus? fallback)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            if (fallback.HasValue)
            {
                return fallback.Value;
            }

            throw new ValidationException("验收状态不能为空");
        }

        if (!Enum.TryParse<PersonDeliveryStatus>(status.Trim(), true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new ValidationException("无效的验收状态");
        }

        return parsed;
    }

    private static PersonDeliveryDto ToDto(PersonDelivery delivery)
    {
        return new PersonDeliveryDto
        {
            Id = delivery.Id,
            PersonId = delivery.PersonId,
            PersonName = delivery.Person?.Name ?? string.Empty,
            ProjectId = delivery.ProjectId,
            ProjectName = delivery.Project?.Name,
            Title = delivery.Title,
            Description = delivery.Description,
            Status = delivery.Status.ToString(),
            AcceptedAt = delivery.AcceptedAt,
            AcceptedBy = delivery.AcceptedBy,
            Notes = delivery.Notes,
            CreatedAt = delivery.CreatedAt,
            UpdatedAt = delivery.UpdatedAt
        };
    }
}
