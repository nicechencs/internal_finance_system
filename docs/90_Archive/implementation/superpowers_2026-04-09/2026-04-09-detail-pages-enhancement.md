# 详情页面完善实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完善 7 个详情页面：补齐后端字段（Customer 银行信息、Person 部门/职位、Receivable 业务类型）、为客户/供应商/人员详情页增加财务汇总统计卡片、统一视觉体验。

**Architecture:** 后端新增 ReceivableType 实体（镜像 PayableType）、扩展 Customer/Person 实体字段、新增财务汇总 API 端点。前端更新类型定义和 API 调用，改造详情页统计卡片区域，使用 DetailSummaryCards 组件统一展示财务汇总。

**Tech Stack:** .NET 8 + EF Core + PostgreSQL / Vue 3 + Element Plus + TypeScript

---

## 文件结构总览

### 后端新建文件
- `backend/FinanceApp.Domain/Entities/ReceivableType.cs` — 应收业务类型实体
- `backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableTypeConfiguration.cs` — EF Core 配置
- `backend/FinanceApp.Infrastructure/Data/Migrations/YYYYMMDD_AddDetailPageEnhancements.cs` — 数据库迁移
- `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/ReceivableTypeDto.cs` — DTO
- `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/CreateReceivableTypeRequest.cs`
- `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/UpdateReceivableTypeRequest.cs`
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CustomerFinanceSummaryDto.cs`
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Supplier/SupplierFinanceSummaryDto.cs`
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/PersonFinanceSummaryDto.cs`
- `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IReceivableTypeService.cs`
- `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableTypeService.cs`
- `backend/FinanceApp.Api/Controllers/FinanceSettlement/ReceivableTypesController.cs`

### 后端修改文件
- `backend/FinanceApp.Domain/Entities/Customer.cs` — 添加 BankAccount/BankName
- `backend/FinanceApp.Domain/Entities/Person.cs` — 添加 Department/Position
- `backend/FinanceApp.Domain/Entities/Receivable.cs` — 添加 ReceivableTypeId + 导航属性
- `backend/FinanceApp.Infrastructure/Data/Configurations/CustomerConfiguration.cs` — 新字段配置
- `backend/FinanceApp.Infrastructure/Data/Configurations/PersonConfiguration.cs` — 新字段配置
- `backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableConfiguration.cs` — 外键配置
- `backend/FinanceApp.Infrastructure/Data/AppDbContext.cs` — 添加 DbSet<ReceivableType>
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CustomerDto.cs` — 新字段
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CreateCustomerRequest.cs` — 新字段
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/UpdateCustomerRequest.cs` — 新字段
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/PersonDto.cs` — 新字段
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/CreatePersonRequest.cs` — 新字段
- `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/UpdatePersonRequest.cs` — 新字段
- `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/ReceivableDto.cs` — 新字段
- `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/CreateReceivableRequest.cs` — 新字段
- `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/UpdateReceivableRequest.cs` — 新字段
- `backend/FinanceApp.Application/Mappings/MappingConfig.cs` — 新映射规则
- `backend/FinanceApp.Application/Modules/MasterData/Interfaces/ICustomerService.cs` — 新方法
- `backend/FinanceApp.Application/Modules/MasterData/Services/CustomerService.cs` — 新方法实现
- `backend/FinanceApp.Application/Modules/MasterData/Interfaces/ISupplierService.cs` — 新方法
- `backend/FinanceApp.Application/Modules/MasterData/Services/SupplierService.cs` — 新方法实现
- `backend/FinanceApp.Application/Modules/MasterData/Interfaces/IPersonService.cs` — 新方法
- `backend/FinanceApp.Application/Modules/MasterData/Services/PersonService.cs` — 新方法实现
- `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IReceivableService.cs` — 修改映射
- `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableService.cs` — Include ReceivableType
- `backend/FinanceApp.Api/Controllers/MasterData/CustomerController.cs` — 新端点
- `backend/FinanceApp.Api/Controllers/MasterData/SupplierController.cs` — 新端点
- `backend/FinanceApp.Api/Controllers/MasterData/PersonController.cs` — 新端点
- DI 注册文件（ReceivableTypeService 注册）

### 前端修改文件
- `frontend/src/features/master-data/customers/types/customer.ts` — 新字段 + 新类型
- `frontend/src/features/master-data/customers/api/customer.ts` — 新 API
- `frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue` — 银行信息 + 财务卡片
- `frontend/src/features/master-data/customers/pages/CustomerFormPage.vue` — 银行信息表单
- `frontend/src/features/master-data/suppliers/types/supplier.ts` — 新类型
- `frontend/src/features/master-data/suppliers/api/supplier.ts` — 新 API
- `frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue` — 财务卡片
- `frontend/src/features/master-data/persons/types/person.ts` — 新字段 + 新类型
- `frontend/src/features/master-data/persons/api/person.ts` — 新 API
- `frontend/src/features/master-data/persons/pages/PersonDetailPage.vue` — 部门/职位 + 财务卡片
- `frontend/src/features/master-data/persons/components/PersonForm.vue` — 部门/职位表单
- `frontend/src/features/finance/types/receivable.ts` — 新字段 + ReceivableType 类型
- `frontend/src/features/finance/api/receivable.ts` — ReceivableType API
- `frontend/src/features/finance/components/ReceivableDetailContent.vue` — 业务类型展示
- `frontend/src/features/finance/components/ReceivableForm.vue` — 业务类型选择器

---

## Task 1: 后端 — 扩展实体字段

**Files:**
- Modify: `backend/FinanceApp.Domain/Entities/Customer.cs`
- Modify: `backend/FinanceApp.Domain/Entities/Person.cs`
- Create: `backend/FinanceApp.Domain/Entities/ReceivableType.cs`
- Modify: `backend/FinanceApp.Domain/Entities/Receivable.cs`

- [ ] **Step 1: Customer 实体添加银行字段**

在 `Customer.cs` 的 `TaxNumber` 后面添加：

```csharp
public string? BankAccount { get; set; }
public string? BankName { get; set; }
```

- [ ] **Step 2: Person 实体添加部门/职位字段**

在 `Person.cs` 的 `PersonType` 后面添加：

```csharp
public string? Department { get; set; }
public string? Position { get; set; }
```

- [ ] **Step 3: 创建 ReceivableType 实体**

创建 `backend/FinanceApp.Domain/Entities/ReceivableType.cs`，镜像 PayableType：

```csharp
namespace FinanceApp.Domain.Entities;

/// <summary>
/// 应收款业务类型（主数据）
/// </summary>
public class ReceivableType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<Receivable> Receivables { get; set; } = new List<Receivable>();
}
```

- [ ] **Step 4: Receivable 实体添加 ReceivableTypeId**

在 `Receivable.cs` 的 `PersonId` 后面添加：

```csharp
public long? ReceivableTypeId { get; set; }
```

在导航属性区域添加：

```csharp
public ReceivableType? ReceivableType { get; set; }
```

- [ ] **Step 5: 提交**

```bash
git add backend/FinanceApp.Domain/Entities/Customer.cs backend/FinanceApp.Domain/Entities/Person.cs backend/FinanceApp.Domain/Entities/ReceivableType.cs backend/FinanceApp.Domain/Entities/Receivable.cs
git commit -m "feat: 扩展实体字段 — Customer 银行信息、Person 部门/职位、ReceivableType 业务类型"
```

---

## Task 2: 后端 — EF Core 配置

**Files:**
- Modify: `backend/FinanceApp.Infrastructure/Data/Configurations/CustomerConfiguration.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/Configurations/PersonConfiguration.cs`
- Create: `backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableTypeConfiguration.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableConfiguration.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: CustomerConfiguration 添加新字段配置**

在 `TaxNumber` 配置后面添加：

```csharp
builder.Property(e => e.BankAccount)
    .HasColumnName("bank_account")
    .HasMaxLength(50);

builder.Property(e => e.BankName)
    .HasColumnName("bank_name")
    .HasMaxLength(100);
```

- [ ] **Step 2: PersonConfiguration 添加新字段配置**

在 `PersonType` 配置后面添加：

```csharp
builder.Property(e => e.Department)
    .HasColumnName("department")
    .HasMaxLength(100);

builder.Property(e => e.Position)
    .HasColumnName("position")
    .HasMaxLength(100);
```

- [ ] **Step 3: 创建 ReceivableTypeConfiguration**

创建 `backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableTypeConfiguration.cs`，镜像 PayableTypeConfiguration：

```csharp
using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class ReceivableTypeConfiguration : IEntityTypeConfiguration<ReceivableType>
{
    public void Configure(EntityTypeBuilder<ReceivableType> builder)
    {
        builder.ToTable("receivable_types");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        // Relationships
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.Code)
            .HasDatabaseName("idx_receivable_types_code")
            .IsUnique()
            .HasFilter("code IS NOT NULL AND is_deleted = false");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_receivable_types_active")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_receivable_types_created_by")
            .HasFilter("is_deleted = false");
    }
}
```

- [ ] **Step 4: ReceivableConfiguration 添加 ReceivableTypeId 外键**

在 `PersonId` 配置后面添加：

```csharp
builder.Property(e => e.ReceivableTypeId)
    .HasColumnName("receivable_type_id");
```

在 Person 关系配置后面添加：

```csharp
builder.HasOne(e => e.ReceivableType)
    .WithMany(rt => rt.Receivables)
    .HasForeignKey(e => e.ReceivableTypeId)
    .OnDelete(DeleteBehavior.Restrict);
```

在索引区域添加：

```csharp
builder.HasIndex(e => e.ReceivableTypeId)
    .HasDatabaseName("idx_receivables_receivable_type");
```

- [ ] **Step 5: AppDbContext 添加 DbSet**

在 `AppDbContext.cs` 中已有的 `DbSet<PayableType>` 附近添加：

```csharp
public DbSet<ReceivableType> ReceivableTypes { get; set; } = null!;
```

- [ ] **Step 6: 生成 EF Core 迁移**

```bash
cd backend
dotnet ef migrations add AddDetailPageEnhancements --project FinanceApp.Infrastructure --startup-project FinanceApp.Api
```

检查生成的迁移文件，确认包含：
- customers 表添加 bank_account、bank_name 列
- persons 表添加 department、position 列
- 新建 receivable_types 表
- receivables 表添加 receivable_type_id 列 + 外键

- [ ] **Step 7: 为迁移创建 Designer.cs（如需手动）**

如果 `dotnet ef migrations add` 自动生成了 Designer.cs 则跳过。否则复制上一个迁移的 Designer.cs，修改 Migration 名称和类名。

- [ ] **Step 8: 运行迁移**

```bash
dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Api
```

- [ ] **Step 9: 提交**

```bash
git add backend/FinanceApp.Infrastructure/
git commit -m "feat: EF Core 配置与迁移 — Customer 银行字段、Person 部门/职位、ReceivableType 表、Receivable 外键"
```

---

## Task 3: 后端 — 更新 DTOs 和映射

**Files:**
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CustomerDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CreateCustomerRequest.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/UpdateCustomerRequest.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/PersonDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/CreatePersonRequest.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/UpdatePersonRequest.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/ReceivableDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/CreateReceivableRequest.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/UpdateReceivableRequest.cs`
- Create: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/ReceivableTypeDto.cs`
- Create: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/CreateReceivableTypeRequest.cs`
- Create: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/UpdateReceivableTypeRequest.cs`
- Modify: `backend/FinanceApp.Application/Mappings/MappingConfig.cs`

- [ ] **Step 1: CustomerDto 添加银行字段**

在 `TaxNumber` 后面添加：

```csharp
public string? BankAccount { get; set; }
public string? BankName { get; set; }
```

- [ ] **Step 2: CreateCustomerRequest 添加银行字段**

在 `TaxNumber` 后面添加：

```csharp
public string? BankAccount { get; set; }
public string? BankName { get; set; }
```

- [ ] **Step 3: UpdateCustomerRequest 添加银行字段**

在 `TaxNumber` 后面添加：

```csharp
public string? BankAccount { get; set; }
public string? BankName { get; set; }
```

- [ ] **Step 4: PersonDto 添加部门/职位字段**

在 `PersonType` 后面添加：

```csharp
public string? Department { get; set; }
public string? Position { get; set; }
```

- [ ] **Step 5: CreatePersonRequest 添加部门/职位字段**

在 `PersonType` 后面添加：

```csharp
public string? Department { get; set; }
public string? Position { get; set; }
```

- [ ] **Step 6: UpdatePersonRequest 添加部门/职位字段**

在 `PersonType` 后面添加：

```csharp
public string? Department { get; set; }
public string? Position { get; set; }
```

- [ ] **Step 7: ReceivableDto 添加业务类型字段**

在 `PersonName` 后面添加：

```csharp
public long? ReceivableTypeId { get; set; }
public string? ReceivableTypeName { get; set; }
```

- [ ] **Step 8: CreateReceivableRequest 添加业务类型字段**

在已有的 `PersonId` 后面添加：

```csharp
public long? ReceivableTypeId { get; set; }
```

- [ ] **Step 9: UpdateReceivableRequest 添加业务类型字段**

在已有的 `PersonId` 后面添加：

```csharp
public long? ReceivableTypeId { get; set; }
```

- [ ] **Step 10: 创建 ReceivableTypeDto**

创建 `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/ReceivableTypeDto.cs`：

```csharp
namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class ReceivableTypeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 11: 创建 CreateReceivableTypeRequest**

创建 `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/CreateReceivableTypeRequest.cs`：

```csharp
namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class CreateReceivableTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
```

- [ ] **Step 12: 创建 UpdateReceivableTypeRequest**

创建 `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/UpdateReceivableTypeRequest.cs`：

```csharp
namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class UpdateReceivableTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
```

- [ ] **Step 13: 更新 MappingConfig**

在 `MappingConfig.cs` 的 Receivable mappings 区域添加：

```csharp
// ReceivableType mappings
config.NewConfig<ReceivableType, ReceivableTypeDto>();
```

修改 Receivable 映射，添加 ReceivableTypeName：

```csharp
config.NewConfig<Receivable, ReceivableDto>()
    .Map(dest => dest.ProjectName, src => src.Project != null ? src.Project.Name : (string?)null)
    .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : (string?)null)
    .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : (string?)null)
    .Map(dest => dest.PersonName, src => src.Person != null ? src.Person.Name : (string?)null)
    .Map(dest => dest.ReceivableTypeName, src => src.ReceivableType != null ? src.ReceivableType.Name : (string?)null)
    .Map(dest => dest.Status, src => src.Status.ToString().ToLowerInvariant())
    .Ignore(dest => dest.Tags);
```

添加 `using FinanceApp.Domain.Entities;`（如果 ReceivableType 未被隐式引用）。

- [ ] **Step 14: 提交**

```bash
git add backend/FinanceApp.Application/
git commit -m "feat: 更新 DTOs 和映射 — Customer 银行字段、Person 部门/职位、ReceivableType、Receivable 业务类型"
```

---

## Task 4: 后端 — ReceivableType CRUD 服务和控制器

**Files:**
- Create: `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IReceivableTypeService.cs`
- Create: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableTypeService.cs`
- Create: `backend/FinanceApp.Api/Controllers/FinanceSettlement/ReceivableTypesController.cs`
- Modify: DI 注册文件

- [ ] **Step 1: 创建 IReceivableTypeService 接口**

查看 `IPayableTypeService.cs` 的接口定义，创建对应的 `IReceivableTypeService.cs`。镜像 PayableType 的接口模式，继承 `ICrudService<ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>`，添加 `GetActiveReceivableTypesAsync()` 方法。

```csharp
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface IReceivableTypeService : ICrudService<ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>
{
    Task<List<ReceivableTypeDto>> GetActiveReceivableTypesAsync();
}
```

- [ ] **Step 2: 创建 ReceivableTypeService 实现**

镜像 `PayableTypeService.cs` 的实现模式。关键点：
- 继承 `CrudServiceBase<ReceivableType, ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>`
- 实现 `GetActiveReceivableTypesAsync` — 查询 IsActive=true，按 SortOrder 排序
- 复用 PayableTypeService 的删除检查逻辑（检查是否有关联的 Receivable 引用）

```csharp
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

public class ReceivableTypeService : CrudServiceBase<ReceivableType, ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>, IReceivableTypeService
{
    private readonly IRepository<ReceivableType> _repository;

    public ReceivableTypeService(
        IRepository<ReceivableType> repository,
        ICurrentUserService currentUserService,
        ILogger<ReceivableTypeService> logger)
        : base(repository, currentUserService, logger)
    {
        _repository = repository;
    }

    public async Task<List<ReceivableTypeDto>> GetActiveReceivableTypesAsync()
    {
        var types = await _repository.GetQueryable()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return types.Adapt<List<ReceivableTypeDto>>();
    }
}
```

注意：实际实现时需参照 PayableTypeService 的完整代码，确保一致的错误处理和权限过滤逻辑。

- [ ] **Step 3: 创建 ReceivableTypesController**

镜像 `PayableTypesController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

[ApiController]
[Route("api/receivable-types")]
[Authorize]
public class ReceivableTypesController : CrudControllerBase<ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>
{
    private readonly IReceivableTypeService _receivableTypeService;

    public ReceivableTypesController(IReceivableTypeService receivableTypeService, ILogger<ReceivableTypesController> logger)
        : base(receivableTypeService, logger)
    {
        _receivableTypeService = receivableTypeService;
    }

    protected override string ControllerName => "ReceivableTypesController";
    protected override string EntityName => "ReceivableType";

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableTypeDto>>>> GetActive()
    {
        Logger.LogInformation("[ReceivableTypesController.GetActive]");
        var result = await _receivableTypeService.GetActiveReceivableTypesAsync();
        return Ok(ApiResponse<List<ReceivableTypeDto>>.SuccessResponse(result));
    }
}
```

- [ ] **Step 4: DI 注册**

在 FinanceSettlement 模块的 DI 注册方法中添加：

```csharp
services.AddScoped<IReceivableTypeService, ReceivableTypeService>();
```

- [ ] **Step 5: 更新 ReceivableService — Include ReceivableType**

在 `ReceivableService.cs` 的查询中，确保 Include ReceivableType 导航属性（参照 PayableService 中 Include PayableType 的方式）。搜索 `.Include(r => r.Project)` 的位置，在同一链中添加 `.Include(r => r.ReceivableType)`。

- [ ] **Step 6: 提交**

```bash
git add backend/
git commit -m "feat: ReceivableType CRUD — 服务接口、实现、控制器、DI 注册"
```

---

## Task 5: 后端 — 财务汇总 API 端点

**Files:**
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CustomerFinanceSummaryDto.cs`
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Supplier/SupplierFinanceSummaryDto.cs`
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/PersonFinanceSummaryDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Interfaces/ICustomerService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Services/CustomerService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Interfaces/ISupplierService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Services/SupplierService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Interfaces/IPersonService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Services/PersonService.cs`
- Modify: `backend/FinanceApp.Api/Controllers/MasterData/CustomerController.cs`
- Modify: `backend/FinanceApp.Api/Controllers/MasterData/SupplierController.cs`
- Modify: `backend/FinanceApp.Api/Controllers/MasterData/PersonController.cs`

- [ ] **Step 1: 创建 CustomerFinanceSummaryDto**

```csharp
namespace FinanceApp.Application.Modules.MasterData.DTOs.Customer;

public class CustomerFinanceSummaryDto
{
    /// <summary>应收总额</summary>
    public decimal TotalReceivable { get; set; }
    /// <summary>已收金额</summary>
    public decimal TotalReceived { get; set; }
    /// <summary>未收余额</summary>
    public decimal TotalRemaining { get; set; }
    /// <summary>逾期笔数</summary>
    public int OverdueCount { get; set; }
    /// <summary>逾期金额</summary>
    public decimal OverdueAmount { get; set; }
    /// <summary>关联项目数</summary>
    public int ProjectCount { get; set; }
}
```

- [ ] **Step 2: 创建 SupplierFinanceSummaryDto**

```csharp
namespace FinanceApp.Application.Modules.MasterData.DTOs.Supplier;

public class SupplierFinanceSummaryDto
{
    /// <summary>应付总额</summary>
    public decimal TotalPayable { get; set; }
    /// <summary>已付金额</summary>
    public decimal TotalPaid { get; set; }
    /// <summary>未付余额</summary>
    public decimal TotalRemaining { get; set; }
    /// <summary>逾期笔数</summary>
    public int OverdueCount { get; set; }
    /// <summary>逾期金额</summary>
    public decimal OverdueAmount { get; set; }
    /// <summary>关联项目数</summary>
    public int ProjectCount { get; set; }
}
```

- [ ] **Step 3: 创建 PersonFinanceSummaryDto**

```csharp
namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class PersonFinanceSummaryDto
{
    /// <summary>总成本（直接+分摊）</summary>
    public decimal TotalCost { get; set; }
    /// <summary>直接成本</summary>
    public decimal DirectCost { get; set; }
    /// <summary>分摊成本</summary>
    public decimal AllocatedCost { get; set; }
    /// <summary>关联项目数</summary>
    public int ProjectCount { get; set; }
    /// <summary>交易笔数</summary>
    public int TransactionCount { get; set; }
    /// <summary>应付未结金额</summary>
    public decimal PayableRemaining { get; set; }
}
```

- [ ] **Step 4: ICustomerService 添加方法**

```csharp
Task<CustomerFinanceSummaryDto> GetFinanceSummaryAsync(long customerId);
```

- [ ] **Step 5: CustomerService 实现 GetFinanceSummaryAsync**

需要注入 `IRepository<Receivable>` 和 `IRepository<Project>` 到 CustomerService（或通过 DbContext 直接查询）。

参照 ReceivableService.GetReceivableSummaryAsync 的聚合查询模式：

```csharp
public async Task<CustomerFinanceSummaryDto> GetFinanceSummaryAsync(long customerId)
{
    var today = DateTime.UtcNow.Date;

    var receivableQuery = _receivableRepository.GetQueryable()
        .Where(r => r.CustomerId == customerId);

    var summary = await receivableQuery
        .GroupBy(_ => 1)
        .Select(g => new CustomerFinanceSummaryDto
        {
            TotalReceivable = g.Sum(r => r.TotalAmount),
            TotalReceived = g.Sum(r => r.ReceivedAmount),
            TotalRemaining = g.Sum(r => r.RemainingAmount),
            OverdueCount = g.Count(r =>
                r.Status != ReceivableStatus.Settled &&
                r.DueDate.HasValue &&
                r.DueDate.Value < today),
            OverdueAmount = g.Where(r =>
                r.Status != ReceivableStatus.Settled &&
                r.DueDate.HasValue &&
                r.DueDate.Value < today)
                .Sum(r => r.RemainingAmount),
            ProjectCount = g.Select(r => r.ProjectId).Distinct().Count()
        })
        .FirstOrDefaultAsync() ?? new CustomerFinanceSummaryDto();

    return summary;
}
```

注意：需在 CustomerService 构造函数中注入 `IRepository<Receivable>`。

- [ ] **Step 6: ISupplierService 添加方法**

```csharp
Task<SupplierFinanceSummaryDto> GetFinanceSummaryAsync(long supplierId);
```

- [ ] **Step 7: SupplierService 实现 GetFinanceSummaryAsync**

与 CustomerService 类似，但查询 Payable 而非 Receivable：

```csharp
public async Task<SupplierFinanceSummaryDto> GetFinanceSummaryAsync(long supplierId)
{
    var today = DateTime.UtcNow.Date;

    var payableQuery = _payableRepository.GetQueryable()
        .Where(p => p.SupplierId == supplierId);

    var summary = await payableQuery
        .GroupBy(_ => 1)
        .Select(g => new SupplierFinanceSummaryDto
        {
            TotalPayable = g.Sum(p => p.TotalAmount),
            TotalPaid = g.Sum(p => p.PaidAmount),
            TotalRemaining = g.Sum(p => p.RemainingAmount),
            OverdueCount = g.Count(p =>
                p.Status != PayableStatus.Settled &&
                p.DueDate.HasValue &&
                p.DueDate.Value < today),
            OverdueAmount = g.Where(p =>
                p.Status != PayableStatus.Settled &&
                p.DueDate.HasValue &&
                p.DueDate.Value < today)
                .Sum(p => p.RemainingAmount),
            ProjectCount = g.Where(p => p.ProjectId.HasValue)
                .Select(p => p.ProjectId).Distinct().Count()
        })
        .FirstOrDefaultAsync() ?? new SupplierFinanceSummaryDto();

    return summary;
}
```

注意：需在 SupplierService 构造函数中注入 `IRepository<Payable>`。

- [ ] **Step 8: IPersonService 添加方法**

```csharp
Task<PersonFinanceSummaryDto> GetFinanceSummaryAsync(long personId);
```

- [ ] **Step 9: PersonService 实现 GetFinanceSummaryAsync**

复用现有的 `GetPersonCostSummaryAsync` 逻辑，扩展加入应付未结金额和项目数：

```csharp
public async Task<PersonFinanceSummaryDto> GetFinanceSummaryAsync(long personId)
{
    var costSummary = await GetPersonCostSummaryAsync(personId);

    var payableRemaining = await _payableRepository.GetQueryable()
        .Where(p => p.PersonId == personId && p.Status != PayableStatus.Settled)
        .SumAsync(p => p.RemainingAmount);

    var projectCount = await _transactionRepository.GetQueryable()
        .Where(t => t.PersonId == personId && t.ProjectId.HasValue)
        .Select(t => t.ProjectId)
        .Distinct()
        .CountAsync();

    return new PersonFinanceSummaryDto
    {
        TotalCost = costSummary.TotalCost,
        DirectCost = costSummary.DirectCost,
        AllocatedCost = costSummary.AllocatedCost,
        TransactionCount = costSummary.TransactionCount,
        ProjectCount = projectCount,
        PayableRemaining = payableRemaining
    };
}
```

注意：需在 PersonService 构造函数中注入 `IRepository<Payable>`（如果尚未注入）。

- [ ] **Step 10: CustomerController 添加端点**

```csharp
[HttpGet("{id:long}/finance-summary")]
[Authorize(Roles = "Admin,Accountant,Viewer")]
public async Task<ActionResult<ApiResponse<CustomerFinanceSummaryDto>>> GetFinanceSummary(long id)
{
    Logger.LogInformation("[CustomerController.GetFinanceSummary] CustomerId={CustomerId}", id);
    var result = await _customerService.GetFinanceSummaryAsync(id);
    return Ok(ApiResponse<CustomerFinanceSummaryDto>.SuccessResponse(result));
}
```

- [ ] **Step 11: SupplierController 添加端点**

```csharp
[HttpGet("{id:long}/finance-summary")]
[Authorize(Roles = "Admin,Accountant,Viewer")]
public async Task<ActionResult<ApiResponse<SupplierFinanceSummaryDto>>> GetFinanceSummary(long id)
{
    Logger.LogInformation("[SupplierController.GetFinanceSummary] SupplierId={SupplierId}", id);
    var result = await _supplierService.GetFinanceSummaryAsync(id);
    return Ok(ApiResponse<SupplierFinanceSummaryDto>.SuccessResponse(result));
}
```

- [ ] **Step 12: PersonController 添加端点**

```csharp
[HttpGet("{id:long}/finance-summary")]
[Authorize(Roles = "Admin,Accountant,Viewer")]
public async Task<ActionResult<ApiResponse<PersonFinanceSummaryDto>>> GetFinanceSummary(long id)
{
    Logger.LogInformation("[PersonController.GetFinanceSummary] PersonId={PersonId}", id);
    var result = await _personService.GetFinanceSummaryAsync(id);
    return Ok(ApiResponse<PersonFinanceSummaryDto>.SuccessResponse(result));
}
```

- [ ] **Step 13: 构建验证**

```bash
cd backend
dotnet build
```

Expected: Build succeeded

- [ ] **Step 14: 提交**

```bash
git add backend/
git commit -m "feat: 财务汇总 API — 客户应收汇总、供应商应付汇总、人员成本汇总端点"
```

---

## Task 6: 前端 — 更新类型定义和 API

**Files:**
- Modify: `frontend/src/features/master-data/customers/types/customer.ts`
- Modify: `frontend/src/features/master-data/customers/api/customer.ts`
- Modify: `frontend/src/features/master-data/suppliers/types/supplier.ts`
- Modify: `frontend/src/features/master-data/suppliers/api/supplier.ts`
- Modify: `frontend/src/features/master-data/persons/types/person.ts`
- Modify: `frontend/src/features/master-data/persons/api/person.ts`
- Modify: `frontend/src/features/finance/types/receivable.ts`
- Modify: `frontend/src/features/finance/api/receivable.ts`

- [ ] **Step 1: Customer 类型添加银行字段 + FinanceSummary**

在 `customer.ts` 的 `Customer` 接口的 `taxNumber` 后添加：

```typescript
bankAccount?: string
bankName?: string
```

在 `CreateCustomerRequest` 的 `taxNumber` 后添加：

```typescript
bankAccount?: string
bankName?: string
```

在 `UpdateCustomerRequest` 的 `taxNumber` 后添加：

```typescript
bankAccount?: string
bankName?: string
```

在文件末尾添加：

```typescript
export interface CustomerFinanceSummary {
  totalReceivable: number
  totalReceived: number
  totalRemaining: number
  overdueCount: number
  overdueAmount: number
  projectCount: number
}
```

- [ ] **Step 2: Customer API 添加 getFinanceSummary**

在 `customer.ts` API 文件中添加：

```typescript
import type { CustomerFinanceSummary } from '@/features/master-data/customers/types/customer'

export const getCustomerFinanceSummary = (id: number) =>
  request<ApiResponse<CustomerFinanceSummary>>({ url: `/customers/${id}/finance-summary`, method: 'get' })
```

- [ ] **Step 3: Supplier 类型添加 FinanceSummary**

在 `supplier.ts` 类型文件末尾添加：

```typescript
export interface SupplierFinanceSummary {
  totalPayable: number
  totalPaid: number
  totalRemaining: number
  overdueCount: number
  overdueAmount: number
  projectCount: number
}
```

- [ ] **Step 4: Supplier API 添加 getFinanceSummary**

```typescript
import type { SupplierFinanceSummary } from '@/features/master-data/suppliers/types/supplier'

export const getSupplierFinanceSummary = (id: number) =>
  request<ApiResponse<SupplierFinanceSummary>>({ url: `/suppliers/${id}/finance-summary`, method: 'get' })
```

- [ ] **Step 5: Person 类型添加 department/position + FinanceSummary**

在 `person.ts` 的 `Person` 接口的 `personType` 后添加：

```typescript
department?: string
position?: string
```

在 `CreatePersonRequest` 的 `personType` 后添加：

```typescript
department?: string
position?: string
```

在 `UpdatePersonRequest` 的 `personType` 后添加：

```typescript
department?: string
position?: string
```

在文件末尾添加：

```typescript
export interface PersonFinanceSummary {
  totalCost: number
  directCost: number
  allocatedCost: number
  projectCount: number
  transactionCount: number
  payableRemaining: number
}
```

- [ ] **Step 6: Person API 添加 getFinanceSummary**

```typescript
import type { PersonFinanceSummary } from '@/features/master-data/persons/types/person'

export const getPersonFinanceSummary = (id: number) =>
  request<ApiResponse<PersonFinanceSummary>>({ url: `/persons/${id}/finance-summary`, method: 'get' })
```

- [ ] **Step 7: Receivable 类型添加 receivableType**

在 `receivable.ts` 的 `Receivable` 接口的 `personName` 后添加：

```typescript
receivableTypeId?: number
receivableTypeName?: string
```

在 `CreateReceivableRequest` 中添加：

```typescript
receivableTypeId?: number
```

在 `UpdateReceivableRequest` 中添加：

```typescript
receivableTypeId?: number
```

在文件末尾添加：

```typescript
export interface ReceivableType {
  id: number
  name: string
  code?: string
  description?: string
  isActive: boolean
  sortOrder: number
}

export interface CreateReceivableTypeRequest {
  name: string
  code?: string
  description?: string
  isActive?: boolean
  sortOrder?: number
}

export interface UpdateReceivableTypeRequest {
  name: string
  code?: string
  description?: string
  isActive?: boolean
  sortOrder?: number
}
```

- [ ] **Step 8: Receivable API 添加 ReceivableType 相关方法**

在 `receivable.ts` API 文件中添加（参照 payable.ts 中 PayableType 的 API 模式）：

```typescript
import type { ReceivableType, CreateReceivableTypeRequest, UpdateReceivableTypeRequest } from '@/features/finance/types/receivable'

// 应收款业务类型管理
export const getReceivableTypesPaged = (params?: { page?: number; pageSize?: number; name?: string; isActive?: boolean }) =>
  request<ApiResponse<PageResponse<ReceivableType>>>({
    url: '/receivable-types',
    method: 'get',
    params: params || { page: 1, pageSize: 200 }
  })

export const getReceivableTypes = () =>
  request<ApiResponse<ReceivableType[]>>({ url: '/receivable-types/active', method: 'get' })

export const createReceivableType = (data: CreateReceivableTypeRequest) =>
  request<ApiResponse<ReceivableType>>({ url: '/receivable-types', method: 'post', data })

export const updateReceivableType = (id: number, data: UpdateReceivableTypeRequest) =>
  request<ApiResponse<ReceivableType>>({ url: `/receivable-types/${id}`, method: 'put', data })

export const deleteReceivableType = (id: number) =>
  request<ApiResponse<void>>({ url: `/receivable-types/${id}`, method: 'delete' })
```

- [ ] **Step 9: 提交**

```bash
git add frontend/src/features/
git commit -m "feat: 前端类型定义和 API — Customer 银行字段、Person 部门/职位、ReceivableType、财务汇总接口"
```

---

## Task 7: 前端 — 客户详情页完善

**Files:**
- Modify: `frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue`

- [ ] **Step 1: 添加 import**

在 script setup 中添加：

```typescript
import { getCustomerFinanceSummary } from '@/features/master-data/customers/api/customer'
import DetailSummaryCards from '@/shared/ui/DetailSummaryCards.vue'
import type { DetailSummaryCardItem } from '@/shared/ui/DetailSummaryCards.vue'
import type { CustomerFinanceSummary } from '@/features/master-data/customers/types/customer'
import { formatCurrency } from '@/shared/utils/formatters'
```

- [ ] **Step 2: 添加财务汇总状态和加载逻辑**

在 script setup 中添加：

```typescript
const financeSummary = ref<CustomerFinanceSummary | null>(null)
const financeSummaryLoading = ref(false)

const loadFinanceSummary = async () => {
  const id = Number(route.params.id)
  if (!id) return
  financeSummaryLoading.value = true
  try {
    const { data } = await getCustomerFinanceSummary(id)
    financeSummary.value = data.data
  } catch {
    // 静默失败，财务汇总非核心功能
  } finally {
    financeSummaryLoading.value = false
  }
}

const financeSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalReceivable', label: '应收总额', value: formatCurrency(s.totalReceivable), meta: `${s.projectCount} 个关联项目`, tone: 'balance' as const },
    { key: 'totalReceived', label: '已收金额', value: formatCurrency(s.totalReceived), tone: 'income' as const },
    { key: 'totalRemaining', label: '未收余额', value: formatCurrency(s.totalRemaining), tone: 'expense' as const },
    { key: 'overdue', label: '逾期', value: `${s.overdueCount} 笔`, meta: s.overdueAmount > 0 ? formatCurrency(s.overdueAmount) : undefined, tone: s.overdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})
```

在 `loadCustomer` 方法中，`await loadTransactions()` 后添加 `loadFinanceSummary()`。
在 `handleLinkSuccess` 中也添加 `loadFinanceSummary()`。

- [ ] **Step 3: 模板 — 添加银行信息字段**

在 `el-descriptions` 的"税号"项后面添加：

```vue
<el-descriptions-item label="银行账号">
  {{ customer?.bankAccount || '-' }}
</el-descriptions-item>
<el-descriptions-item label="开户行">
  {{ customer?.bankName || '-' }}
</el-descriptions-item>
```

- [ ] **Step 4: 模板 — 添加财务概览**

在现有的 `<SummaryOverview>` 组件（收支概览）后面添加：

```vue
<SummaryOverview
  title="财务概览"
  subtitle="应收账款汇总"
  :loading="financeSummaryLoading"
  :empty="!financeSummary"
  empty-text="暂无应收数据"
>
  <DetailSummaryCards :items="financeSummaryCards" />
</SummaryOverview>
```

- [ ] **Step 5: 提交**

```bash
git add frontend/src/features/master-data/customers/
git commit -m "feat: 客户详情页 — 银行信息字段 + 财务概览统计卡片"
```

---

## Task 8: 前端 — 供应商详情页完善

**Files:**
- Modify: `frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue`

- [ ] **Step 1: 添加 import 和状态**

添加与 Task 7 类似的 import，使用 `getSupplierFinanceSummary` 和 `SupplierFinanceSummary` 类型。

- [ ] **Step 2: 添加财务汇总逻辑**

```typescript
const financeSummary = ref<SupplierFinanceSummary | null>(null)
const financeSummaryLoading = ref(false)

const loadFinanceSummary = async () => {
  const id = Number(route.params.id)
  if (!id) return
  financeSummaryLoading.value = true
  try {
    const { data } = await getSupplierFinanceSummary(id)
    financeSummary.value = data.data
  } catch {
    // 静默失败
  } finally {
    financeSummaryLoading.value = false
  }
}

const financeSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalPayable', label: '应付总额', value: formatCurrency(s.totalPayable), meta: `${s.projectCount} 个关联项目`, tone: 'balance' as const },
    { key: 'totalPaid', label: '已付金额', value: formatCurrency(s.totalPaid), tone: 'income' as const },
    { key: 'totalRemaining', label: '未付余额', value: formatCurrency(s.totalRemaining), tone: 'expense' as const },
    { key: 'overdue', label: '逾期', value: `${s.overdueCount} 笔`, meta: s.overdueAmount > 0 ? formatCurrency(s.overdueAmount) : undefined, tone: s.overdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})
```

在 `loadSupplier` 和 `handleLinkSuccess` 中调用 `loadFinanceSummary()`。

- [ ] **Step 3: 模板 — 添加财务概览**

在现有 `<SummaryOverview>` 后面添加（与客户页保持一致的结构）：

```vue
<SummaryOverview
  title="财务概览"
  subtitle="应付账款汇总"
  :loading="financeSummaryLoading"
  :empty="!financeSummary"
  empty-text="暂无应付数据"
>
  <DetailSummaryCards :items="financeSummaryCards" />
</SummaryOverview>
```

- [ ] **Step 4: 提交**

```bash
git add frontend/src/features/master-data/suppliers/
git commit -m "feat: 供应商详情页 — 财务概览统计卡片"
```

---

## Task 9: 前端 — 人员详情页完善

**Files:**
- Modify: `frontend/src/features/master-data/persons/pages/PersonDetailPage.vue`
- Modify: `frontend/src/features/master-data/persons/components/PersonForm.vue`

- [ ] **Step 1: 详情页添加 import 和财务汇总逻辑**

添加 `getPersonFinanceSummary`、`PersonFinanceSummary`、`DetailSummaryCards` 的 import。

```typescript
const financeSummary = ref<PersonFinanceSummary | null>(null)
const financeSummaryLoading = ref(false)

const loadFinanceSummary = async () => {
  const id = Number(route.params.id)
  if (!id) return
  financeSummaryLoading.value = true
  try {
    const { data } = await getPersonFinanceSummary(id)
    financeSummary.value = data.data
  } catch {
    // 静默失败
  } finally {
    financeSummaryLoading.value = false
  }
}

const financeSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalCost', label: '总成本', value: formatCurrency(s.totalCost), meta: `${s.transactionCount} 笔交易`, tone: 'balance' as const },
    { key: 'directCost', label: '直接成本', value: formatCurrency(s.directCost), tone: 'expense' as const },
    { key: 'allocatedCost', label: '分摊成本', value: formatCurrency(s.allocatedCost), meta: `${s.projectCount} 个关联项目`, tone: 'transfer' as const },
    { key: 'payableRemaining', label: '应付未结', value: formatCurrency(s.payableRemaining), tone: s.payableRemaining > 0 ? 'expense' as const : 'neutral' as const }
  ]
})
```

- [ ] **Step 2: 模板 — 添加部门/职位字段**

在 `el-descriptions` 的"人员类型"后面添加：

```vue
<el-descriptions-item label="部门">{{ person?.department || '-' }}</el-descriptions-item>
<el-descriptions-item label="职位">{{ person?.position || '-' }}</el-descriptions-item>
```

- [ ] **Step 3: 模板 — 添加财务概览**

在现有 `<SummaryOverview>` 后面添加：

```vue
<SummaryOverview
  title="成本概览"
  subtitle="人员关联成本汇总"
  :loading="financeSummaryLoading"
  :empty="!financeSummary"
  empty-text="暂无成本数据"
>
  <DetailSummaryCards :items="financeSummaryCards" />
</SummaryOverview>
```

- [ ] **Step 4: PersonForm.vue 添加部门/职位表单项**

在 PersonForm.vue 的"人员类型"表单项后面添加两个新的 el-form-item：

```vue
<el-form-item label="部门" prop="department">
  <el-input v-model="form.department" placeholder="请输入部门" clearable />
</el-form-item>
<el-form-item label="职位" prop="position">
  <el-input v-model="form.position" placeholder="请输入职位" clearable />
</el-form-item>
```

确保 form 对象的初始值中包含 `department: ''` 和 `position: ''`，编辑模式下正确回填。

- [ ] **Step 5: 提交**

```bash
git add frontend/src/features/master-data/persons/
git commit -m "feat: 人员详情页 — 部门/职位字段 + 成本概览统计卡片"
```

---

## Task 10: 前端 — 应收详情页添加业务类型

**Files:**
- Modify: `frontend/src/features/finance/components/ReceivableDetailContent.vue`
- Modify: `frontend/src/features/finance/components/ReceivableForm.vue`

- [ ] **Step 1: ReceivableDetailContent 添加业务类型展示**

在 `el-descriptions` 中"客户"字段后面（参照 PayableDetailContent 中"业务类型"的位置）添加：

```vue
<el-descriptions-item label="业务类型">
  {{ receivable.receivableTypeName || '-' }}
</el-descriptions-item>
```

- [ ] **Step 2: ReceivableForm 添加业务类型选择器**

参照 PayableForm 中 PayableType 选择器的实现模式：

在 script setup 中添加：

```typescript
import { getReceivableTypes } from '@/features/finance/api/receivable'
import type { ReceivableType } from '@/features/finance/types/receivable'

const receivableTypes = ref<ReceivableType[]>([])

const loadReceivableTypes = async () => {
  try {
    const { data } = await getReceivableTypes()
    receivableTypes.value = data.data
  } catch {
    // 静默失败
  }
}
```

在 `onMounted` 中调用 `loadReceivableTypes()`。

在模板中，项目选择器前面添加：

```vue
<el-form-item label="业务类型" prop="receivableTypeId">
  <el-select v-model="form.receivableTypeId" placeholder="请选择业务类型" clearable>
    <el-option
      v-for="type in receivableTypes"
      :key="type.id"
      :label="type.name"
      :value="type.id"
    />
  </el-select>
</el-form-item>
```

确保 form 初始值包含 `receivableTypeId: undefined`。

- [ ] **Step 3: 提交**

```bash
git add frontend/src/features/finance/
git commit -m "feat: 应收详情页 — 业务类型展示和选择器"
```

---

## Task 11: 前端 — 客户表单添加银行字段

**Files:**
- Modify: `frontend/src/features/master-data/customers/pages/CustomerFormPage.vue`

- [ ] **Step 1: 添加银行信息表单字段**

在 CustomerFormPage.vue 的表单中，"税号"字段后面添加：

```vue
<el-form-item label="银行账号" prop="bankAccount">
  <el-input v-model="form.bankAccount" placeholder="请输入银行账号" clearable />
</el-form-item>
<el-form-item label="开户行" prop="bankName">
  <el-input v-model="form.bankName" placeholder="请输入开户行" clearable />
</el-form-item>
```

确保 form 初始值和编辑回填逻辑包含 `bankAccount` 和 `bankName` 字段。

- [ ] **Step 2: 提交**

```bash
git add frontend/src/features/master-data/customers/
git commit -m "feat: 客户表单 — 添加银行账号和开户行字段"
```

---

## Task 12: 构建验证和最终检查

- [ ] **Step 1: 后端构建验证**

```bash
cd backend
dotnet build
```

Expected: Build succeeded

- [ ] **Step 2: 前端构建验证**

```bash
cd frontend
npm run build
```

Expected: Build succeeded（可能有非关键 warning）

- [ ] **Step 3: 检查页面视觉一致性**

确认所有详情页的结构保持一致：
1. 页面头部（返回按钮 + 标题 + 操作按钮）
2. 基本信息卡片（info-card + el-descriptions，3 列布局）
3. 收支概览（SummaryOverview + TransactionSummaryCards）
4. **新增** 财务概览（SummaryOverview + DetailSummaryCards）
5. 列表区域（tab-section + el-tabs + el-table）

- [ ] **Step 4: 最终提交**

```bash
git add -A
git commit -m "feat: 详情页面完善 — 字段补齐、财务汇总卡片、视觉统一"
```
