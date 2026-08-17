# 标签规则功能实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有标签体系（Tag / TagBinding / TagScope）基础上，新增独立的标签规则（TagRule）功能，支持基于匹配条件自动为实体打标签，Phase 1 仅支持 Transaction 范围。

**Architecture:** 复用现有 Tag、TagBinding、TagScope 基础设施，新增 TagRule 实体和 TagRuleTag 多对多关联。TagRuleService 负责规则 CRUD 和手动执行（规则重跑），匹配逻辑与现有 ClassificationRule 一致但完全独立。前端在"自动化"分组下新增标签规则管理页面。

**Tech Stack:** .NET 8 / EF Core / PostgreSQL / Vue 3 / TypeScript / Element Plus

---

## 文件清单

### 新建文件

| 文件 | 职责 |
|------|------|
| `backend/FinanceApp.Domain/Entities/TagRule.cs` | 标签规则实体 |
| `backend/FinanceApp.Domain/Entities/TagRuleTag.cs` | 规则-标签多对多关联实体 |
| `backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleConfiguration.cs` | TagRule EF 配置 |
| `backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleTagConfiguration.cs` | TagRuleTag EF 配置 |
| `backend/FinanceApp.Infrastructure/Data/Migrations/20260408000000_AddTagRules.cs` | 数据库迁移 |
| `backend/FinanceApp.Infrastructure/Data/Migrations/20260408000000_AddTagRules.Designer.cs` | 迁移 Designer |
| `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/TagRuleDto.cs` | 标签规则响应 DTO |
| `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/CreateTagRuleRequest.cs` | 创建请求 DTO |
| `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/UpdateTagRuleRequest.cs` | 更新请求 DTO |
| `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/RunTagRulesRequest.cs` | 规则重跑请求 DTO |
| `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/RunTagRulesResult.cs` | 规则重跑结果 DTO |
| `backend/FinanceApp.Application/Modules/MasterData/Interfaces/ITagRuleService.cs` | 服务接口 |
| `backend/FinanceApp.Application/Modules/MasterData/Services/TagRuleService.cs` | 服务实现 |
| `backend/FinanceApp.Api/Controllers/MasterData/TagRuleController.cs` | API 控制器 |
| `backend/tests/FinanceApp.Application.Tests/Services/TagRuleServiceTests.cs` | 单元测试 |
| `frontend/src/features/reconciliation/types/tagRule.ts` | 前端类型定义 |
| `frontend/src/features/reconciliation/api/tagRule.ts` | 前端 API 模块 |
| `frontend/src/features/reconciliation/pages/TagRuleListPage.vue` | 标签规则列表页 |
| `frontend/src/features/reconciliation/components/TagRuleForm.vue` | 标签规则表单弹窗 |
| `frontend/src/features/reconciliation/components/TagRuleRerunDialog.vue` | 标签规则重跑弹窗 |

### 修改文件

| 文件 | 变更 |
|------|------|
| `backend/FinanceApp.Infrastructure/Data/AppDbContext.cs` | 新增 DbSet<TagRule> 和 DbSet<TagRuleTag> |
| `backend/FinanceApp.Application/Modules/MasterData/MasterDataModuleExtensions.cs` | 注册 ITagRuleService |
| `frontend/src/features/reconciliation/routes.ts` | 新增 /tag-rules 路由 |

---

### Task 1: Domain 层 — TagRule 和 TagRuleTag 实体

**Files:**
- Create: `backend/FinanceApp.Domain/Entities/TagRule.cs`
- Create: `backend/FinanceApp.Domain/Entities/TagRuleTag.cs`

- [ ] **Step 1: 创建 TagRule 实体**

```csharp
// backend/FinanceApp.Domain/Entities/TagRule.cs
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class TagRule : BaseEntity
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public TagScope TargetScope { get; set; }
    public RuleMatchField MatchField { get; set; }
    public RuleMatchOperator MatchOperator { get; set; }
    public string MatchValue { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<TagRuleTag> TagRuleTags { get; set; } = new List<TagRuleTag>();
}
```

- [ ] **Step 2: 创建 TagRuleTag 关联实体**

```csharp
// backend/FinanceApp.Domain/Entities/TagRuleTag.cs
namespace FinanceApp.Domain.Entities;

public class TagRuleTag
{
    public long TagRuleId { get; set; }
    public long TagId { get; set; }

    // Navigation properties
    public TagRule TagRule { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
```

- [ ] **Step 3: 验证编译通过**

Run: `cd /d/demo/chen/finance_system/backend && dotnet build FinanceApp.Domain`
Expected: Build succeeded

- [ ] **Step 4: 提交**

```bash
git add backend/FinanceApp.Domain/Entities/TagRule.cs backend/FinanceApp.Domain/Entities/TagRuleTag.cs
git commit -m "feat: 新增 TagRule 和 TagRuleTag 领域实体"
```

---

### Task 2: Infrastructure 层 — EF 配置与 DbContext

**Files:**
- Create: `backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleConfiguration.cs`
- Create: `backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleTagConfiguration.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/AppDbContext.cs:28-30`

- [ ] **Step 1: 创建 TagRuleConfiguration**

```csharp
// backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleConfiguration.cs
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TagRuleConfiguration : IEntityTypeConfiguration<TagRule>
{
    public void Configure(EntityTypeBuilder<TagRule> builder)
    {
        builder.ToTable("tag_rules");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.RuleName)
            .HasColumnName("rule_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(e => e.TargetScope)
            .HasColumnName("target_scope")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<TagScope>(v, true));

        builder.Property(e => e.MatchField)
            .HasColumnName("match_field")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RuleMatchField>(v, true));

        builder.Property(e => e.MatchOperator)
            .HasColumnName("match_operator")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RuleMatchOperator>(v, true));

        builder.Property(e => e.MatchValue)
            .HasColumnName("match_value")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
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
        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("idx_tag_rules_priority")
            .HasFilter("is_active = true");

        builder.HasIndex(e => e.TargetScope)
            .HasDatabaseName("idx_tag_rules_target_scope")
            .HasFilter("is_active = true");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_tag_rules_created_by")
            .HasFilter("is_deleted = false");
    }
}
```

- [ ] **Step 2: 创建 TagRuleTagConfiguration**

```csharp
// backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleTagConfiguration.cs
using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TagRuleTagConfiguration : IEntityTypeConfiguration<TagRuleTag>
{
    public void Configure(EntityTypeBuilder<TagRuleTag> builder)
    {
        builder.ToTable("tag_rule_tags");

        builder.HasKey(e => new { e.TagRuleId, e.TagId });

        builder.Property(e => e.TagRuleId)
            .HasColumnName("tag_rule_id");

        builder.Property(e => e.TagId)
            .HasColumnName("tag_id");

        builder.HasOne(e => e.TagRule)
            .WithMany(r => r.TagRuleTags)
            .HasForeignKey(e => e.TagRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany()
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 3: 在 AppDbContext 中注册 DbSet**

在 `AppDbContext.cs` 的 DbSet 声明区域（`Tags` 和 `TagBindings` 之后）添加：

```csharp
public DbSet<TagRule> TagRules { get; set; }
public DbSet<TagRuleTag> TagRuleTags { get; set; }
```

- [ ] **Step 4: 验证编译通过**

Run: `cd /d/demo/chen/finance_system/backend && dotnet build FinanceApp.Infrastructure`
Expected: Build succeeded

- [ ] **Step 5: 提交**

```bash
git add backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleConfiguration.cs \
       backend/FinanceApp.Infrastructure/Data/Configurations/TagRuleTagConfiguration.cs \
       backend/FinanceApp.Infrastructure/Data/AppDbContext.cs
git commit -m "feat: 添加 TagRule/TagRuleTag EF 配置和 DbSet 注册"
```

---

### Task 3: 数据库迁移

**Files:**
- Create: `backend/FinanceApp.Infrastructure/Data/Migrations/20260408000000_AddTagRules.cs`
- Create: `backend/FinanceApp.Infrastructure/Data/Migrations/20260408000000_AddTagRules.Designer.cs`

- [ ] **Step 1: 创建迁移文件**

```csharp
// backend/FinanceApp.Infrastructure/Data/Migrations/20260408000000_AddTagRules.cs
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceApp.Infrastructure.Data.Migrations
{
    public partial class AddTagRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tag_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rule_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    target_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    match_field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    match_operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    match_value = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_rules_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_rule_tags",
                columns: table => new
                {
                    tag_rule_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_rule_tags", x => new { x.tag_rule_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_tag_rule_tags_tag_rules_tag_rule_id",
                        column: x => x.tag_rule_id,
                        principalTable: "tag_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tag_rule_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tag_rules_priority",
                table: "tag_rules",
                column: "priority",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "idx_tag_rules_target_scope",
                table: "tag_rules",
                column: "target_scope",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "idx_tag_rules_created_by",
                table: "tag_rules",
                column: "created_by",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_tag_rule_tags_tag_id",
                table: "tag_rule_tags",
                column: "tag_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "tag_rule_tags");
            migrationBuilder.DropTable(name: "tag_rules");
        }
    }
}
```

- [ ] **Step 2: 创建 Designer.cs**

复制最近一个迁移的 Designer.cs（`20260404114242_PreventDuplicateSettlementBindings.Designer.cs`），修改类名和 Migration attribute 为 `AddTagRules`，并在 `BuildTargetModel` 中添加 `TagRule` 和 `TagRuleTag` 的模型定义。

> **注意**：由于 Designer.cs 包含完整模型快照且内容很长，建议通过 `dotnet ef migrations add AddTagRules --project FinanceApp.Infrastructure --startup-project FinanceApp.Api` 自动生成，然后将生成的文件重命名为 `20260408000000_AddTagRules` 格式。如果自动生成命令不可用，手动复制上一个 Designer.cs 并修改类名和 `[Migration("20260408000000_AddTagRules")]` attribute。

Run: `cd /d/demo/chen/finance_system/backend && dotnet ef migrations add AddTagRules --project FinanceApp.Infrastructure --startup-project FinanceApp.Api`

如果命令成功，将生成的迁移文件重命名为 `20260408000000_AddTagRules.cs` 和对应的 Designer.cs。

- [ ] **Step 3: 运行迁移验证**

Run: `cd /d/demo/chen/finance_system/backend && dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Api`
Expected: 迁移成功，tag_rules 和 tag_rule_tags 表创建完成

- [ ] **Step 4: 提交**

```bash
git add backend/FinanceApp.Infrastructure/Data/Migrations/
git commit -m "feat: 添加 tag_rules/tag_rule_tags 数据库迁移"
```

---

### Task 4: Application 层 — DTO 定义

**Files:**
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/TagRuleDto.cs`
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/CreateTagRuleRequest.cs`
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/UpdateTagRuleRequest.cs`
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/RunTagRulesRequest.cs`
- Create: `backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/RunTagRulesResult.cs`

- [ ] **Step 1: 创建 TagRuleDto**

```csharp
// backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/TagRuleDto.cs
namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class TagRuleDto
{
    public long Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string TargetScope { get; set; } = string.Empty;
    public string MatchField { get; set; } = string.Empty;
    public string MatchOperator { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<TagRuleTagItemDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class TagRuleTagItemDto
{
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
}
```

- [ ] **Step 2: 创建 CreateTagRuleRequest**

```csharp
// backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/CreateTagRuleRequest.cs
namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class CreateTagRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string TargetScope { get; set; } = string.Empty;
    public string MatchField { get; set; } = string.Empty;
    public string MatchOperator { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public List<long> TagIds { get; set; } = new();
    /// <summary>
    /// 即时创建的新标签名称列表（可选），创建后自动关联
    /// </summary>
    public List<string> NewTagNames { get; set; } = new();
}
```

- [ ] **Step 3: 创建 UpdateTagRuleRequest**

```csharp
// backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/UpdateTagRuleRequest.cs
namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class UpdateTagRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string TargetScope { get; set; } = string.Empty;
    public string MatchField { get; set; } = string.Empty;
    public string MatchOperator { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<long> TagIds { get; set; } = new();
    public List<string> NewTagNames { get; set; } = new();
}
```

- [ ] **Step 4: 创建 RunTagRulesRequest 和 RunTagRulesResult**

```csharp
// backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/RunTagRulesRequest.cs
namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class RunTagRulesRequest
{
    public string TargetScope { get; set; } = string.Empty;
    /// <summary>
    /// 可选：指定实体 ID 列表。为空时对该 scope 全部实体重跑。
    /// </summary>
    public List<long>? EntityIds { get; set; }
}
```

```csharp
// backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/RunTagRulesResult.cs
namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class RunTagRulesResult
{
    public int ScannedCount { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
}
```

- [ ] **Step 5: 验证编译**

Run: `cd /d/demo/chen/finance_system/backend && dotnet build FinanceApp.Application`
Expected: Build succeeded

- [ ] **Step 6: 提交**

```bash
git add backend/FinanceApp.Application/Modules/MasterData/DTOs/TagRule/
git commit -m "feat: 添加标签规则 DTO 定义"
```

---

### Task 5: Application 层 — ITagRuleService 接口

**Files:**
- Create: `backend/FinanceApp.Application/Modules/MasterData/Interfaces/ITagRuleService.cs`

- [ ] **Step 1: 创建接口**

```csharp
// backend/FinanceApp.Application/Modules/MasterData/Interfaces/ITagRuleService.cs
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ITagRuleService
{
    Task<PageResponse<TagRuleDto>> GetPagedAsync(PageRequest request);
    Task<TagRuleDto> GetByIdAsync(long id);
    Task<TagRuleDto> CreateAsync(CreateTagRuleRequest request);
    Task<TagRuleDto> UpdateAsync(long id, UpdateTagRuleRequest request);
    Task DeleteAsync(long id);
    Task<RunTagRulesResult> RunRulesAsync(RunTagRulesRequest request);
}
```

- [ ] **Step 2: 验证编译**

Run: `cd /d/demo/chen/finance_system/backend && dotnet build FinanceApp.Application`
Expected: Build succeeded

- [ ] **Step 3: 提交**

```bash
git add backend/FinanceApp.Application/Modules/MasterData/Interfaces/ITagRuleService.cs
git commit -m "feat: 添加 ITagRuleService 接口"
```

---

### Task 6: Application 层 — TagRuleService 实现

**Files:**
- Create: `backend/FinanceApp.Application/Modules/MasterData/Services/TagRuleService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/MasterDataModuleExtensions.cs`

- [ ] **Step 1: 创建 TagRuleService**

```csharp
// backend/FinanceApp.Application/Modules/MasterData/Services/TagRuleService.cs
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class TagRuleService : ServiceBase, ITagRuleService
{
    private readonly IRepository<TagRule> _tagRuleRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<TagRuleTag> _tagRuleTagRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly ILogger<TagRuleService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public TagRuleService(
        IRepository<TagRule> tagRuleRepository,
        IRepository<Tag> tagRepository,
        IRepository<TagRuleTag> tagRuleTagRepository,
        IRepository<TagBinding> tagBindingRepository,
        IRepository<Transaction> transactionRepository,
        ILogger<TagRuleService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _tagRuleRepository = tagRuleRepository;
        _tagRepository = tagRepository;
        _tagRuleTagRepository = tagRuleTagRepository;
        _tagBindingRepository = tagBindingRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<TagRuleDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("TagRuleService.GetPagedAsync: Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);

        var sortableFields = SortingHelper.Merge(
            SortingHelper.GetBaseFields<TagRule>(),
            new Dictionary<string, System.Linq.Expressions.Expression<Func<TagRule, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["ruleName"] = r => r.RuleName,
                ["priority"] = r => r.Priority,
                ["targetScope"] = r => r.TargetScope,
                ["isActive"] = r => r.IsActive,
            });

        var query = _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .OrderByDescending(r => r.CreatedAt);

        var sortedQuery = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);
        var total = await sortedQuery.CountAsync();
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();

        _logger.LogInformation("获取标签规则分页列表成功, 返回 {Count} 条, 总计 {Total} 条", dtos.Count, total);

        return new PageResponse<TagRuleDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<TagRuleDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("TagRuleService.GetByIdAsync: Id={Id}", id);

        var rule = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
            throw new NotFoundException("标签规则不存在");

        return MapToDto(rule);
    }

    public async Task<TagRuleDto> CreateAsync(CreateTagRuleRequest request)
    {
        _logger.LogDebug("TagRuleService.CreateAsync: RuleName={RuleName}, TargetScope={TargetScope}",
            request.RuleName, request.TargetScope);

        // 验证 TargetScope
        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
            throw new ValidationException($"无效的目标范围: {request.TargetScope}");

        // 验证 MatchField
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, true, out var matchField))
            throw new ValidationException($"无效的匹配字段: {request.MatchField}");

        // 验证 MatchOperator
        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, true, out var matchOperator))
            throw new ValidationException($"无效的匹配操作符: {request.MatchOperator}");

        // 验证正则表达式
        if (matchOperator == RuleMatchOperator.Regex)
        {
            try { _ = new Regex(request.MatchValue); }
            catch (ArgumentException ex)
            {
                throw new ValidationException($"无效的正则表达式: {ex.Message}");
            }
        }

        // 解析即时创建的新标签
        var allTagIds = new List<long>(request.TagIds);
        if (request.NewTagNames.Count > 0)
        {
            foreach (var newName in request.NewTagNames)
            {
                var trimmedName = newName.Trim();
                if (string.IsNullOrEmpty(trimmedName)) continue;

                // 检查是否已存在同 scope 同名标签
                var existingTag = await _tagRepository.GetQueryable()
                    .FirstOrDefaultAsync(t => t.Scope == targetScope && t.Name == trimmedName);

                if (existingTag != null)
                {
                    allTagIds.Add(existingTag.Id);
                }
                else
                {
                    var newTag = new Tag
                    {
                        Scope = targetScope,
                        Name = trimmedName,
                        IsActive = true,
                        IsSystem = false
                    };
                    await _tagRepository.AddAsync(newTag);
                    await _unitOfWork.SaveChangesAsync();
                    allTagIds.Add(newTag.Id);
                }
            }
        }

        // 验证标签存在且 scope 匹配
        if (allTagIds.Count == 0)
            throw new ValidationException("至少需要关联一个标签");

        var distinctTagIds = allTagIds.Distinct().ToList();
        var tags = await _tagRepository.GetQueryable()
            .Where(t => distinctTagIds.Contains(t.Id))
            .ToListAsync();

        if (tags.Count != distinctTagIds.Count)
            throw new ValidationException("部分标签不存在");

        var mismatchedTag = tags.FirstOrDefault(t => t.Scope != targetScope);
        if (mismatchedTag != null)
            throw new ValidationException($"标签 \"{mismatchedTag.Name}\" 的范围 ({mismatchedTag.Scope}) 与规则目标范围 ({targetScope}) 不匹配");

        var rule = new TagRule
        {
            RuleName = request.RuleName,
            Priority = request.Priority,
            TargetScope = targetScope,
            MatchField = matchField,
            MatchOperator = matchOperator,
            MatchValue = request.MatchValue,
            IsActive = true
        };

        await _tagRuleRepository.AddAsync(rule);
        await _unitOfWork.SaveChangesAsync();

        // 添加关联
        foreach (var tagId in distinctTagIds)
        {
            await _tagRuleTagRepository.AddAsync(new TagRuleTag
            {
                TagRuleId = rule.Id,
                TagId = tagId
            });
        }
        await _unitOfWork.SaveChangesAsync();

        // 重新加载含导航属性的数据
        var created = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstAsync(r => r.Id == rule.Id);

        var dto = MapToDto(created);
        await _auditLogService.LogAsync("Create", "TagRule", rule.Id, null, SerializeForAudit(dto));
        _logger.LogInformation("创建标签规则成功: Id={Id}, RuleName={RuleName}", rule.Id, rule.RuleName);

        return dto;
    }

    public async Task<TagRuleDto> UpdateAsync(long id, UpdateTagRuleRequest request)
    {
        _logger.LogDebug("TagRuleService.UpdateAsync: Id={Id}, RuleName={RuleName}", id, request.RuleName);

        var rule = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
            throw new NotFoundException("标签规则不存在");

        var oldDto = MapToDto(rule);

        // 验证枚举
        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
            throw new ValidationException($"无效的目标范围: {request.TargetScope}");
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, true, out var matchField))
            throw new ValidationException($"无效的匹配字段: {request.MatchField}");
        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, true, out var matchOperator))
            throw new ValidationException($"无效的匹配操作符: {request.MatchOperator}");

        if (matchOperator == RuleMatchOperator.Regex)
        {
            try { _ = new Regex(request.MatchValue); }
            catch (ArgumentException ex) { throw new ValidationException($"无效的正则表达式: {ex.Message}"); }
        }

        // 处理即时创建的新标签
        var allTagIds = new List<long>(request.TagIds);
        if (request.NewTagNames.Count > 0)
        {
            foreach (var newName in request.NewTagNames)
            {
                var trimmedName = newName.Trim();
                if (string.IsNullOrEmpty(trimmedName)) continue;

                var existingTag = await _tagRepository.GetQueryable()
                    .FirstOrDefaultAsync(t => t.Scope == targetScope && t.Name == trimmedName);

                if (existingTag != null)
                {
                    allTagIds.Add(existingTag.Id);
                }
                else
                {
                    var newTag = new Tag { Scope = targetScope, Name = trimmedName, IsActive = true, IsSystem = false };
                    await _tagRepository.AddAsync(newTag);
                    await _unitOfWork.SaveChangesAsync();
                    allTagIds.Add(newTag.Id);
                }
            }
        }

        var distinctTagIds = allTagIds.Distinct().ToList();
        if (distinctTagIds.Count == 0)
            throw new ValidationException("至少需要关联一个标签");

        // 验证标签
        var tags = await _tagRepository.GetQueryable()
            .Where(t => distinctTagIds.Contains(t.Id))
            .ToListAsync();
        if (tags.Count != distinctTagIds.Count)
            throw new ValidationException("部分标签不存在");
        var mismatchedTag = tags.FirstOrDefault(t => t.Scope != targetScope);
        if (mismatchedTag != null)
            throw new ValidationException($"标签 \"{mismatchedTag.Name}\" 的范围与规则目标范围不匹配");

        // 更新规则字段
        rule.RuleName = request.RuleName;
        rule.Priority = request.Priority;
        rule.TargetScope = targetScope;
        rule.MatchField = matchField;
        rule.MatchOperator = matchOperator;
        rule.MatchValue = request.MatchValue;
        rule.IsActive = request.IsActive;

        // 更新标签关联：删除旧的，添加新的
        var existingRuleTags = rule.TagRuleTags.ToList();
        foreach (var rt in existingRuleTags)
        {
            _tagRuleTagRepository.Delete(rt);
        }
        await _unitOfWork.SaveChangesAsync();

        foreach (var tagId in distinctTagIds)
        {
            await _tagRuleTagRepository.AddAsync(new TagRuleTag { TagRuleId = rule.Id, TagId = tagId });
        }

        _tagRuleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync();

        // 重新加载
        var updated = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstAsync(r => r.Id == id);

        var newDto = MapToDto(updated);
        await _auditLogService.LogAsync("Update", "TagRule", rule.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));
        _logger.LogInformation("更新标签规则成功: Id={Id}, RuleName={RuleName}", id, rule.RuleName);

        return newDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("TagRuleService.DeleteAsync: Id={Id}", id);

        var rule = await _tagRuleRepository.GetByIdAsync(id);
        if (rule == null)
            throw new NotFoundException("标签规则不存在");

        var oldDto = MapToDto(rule);

        rule.IsDeleted = true;
        rule.DeletedAt = DateTime.UtcNow;
        _tagRuleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "TagRule", rule.Id, SerializeForAudit(oldDto), null);
        _logger.LogInformation("软删除标签规则成功: Id={Id}, RuleName={RuleName}", id, rule.RuleName);
    }

    public async Task<RunTagRulesResult> RunRulesAsync(RunTagRulesRequest request)
    {
        _logger.LogDebug("TagRuleService.RunRulesAsync: TargetScope={TargetScope}, EntityIds={EntityIds}",
            request.TargetScope, request.EntityIds != null ? string.Join(",", request.EntityIds) : "ALL");

        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
            throw new ValidationException($"无效的目标范围: {request.TargetScope}");

        // Phase 1: 仅支持 Transaction
        if (targetScope != TagScope.Transaction)
            throw new ValidationException($"当前仅支持 Transaction 范围的标签规则重跑");

        // 加载该 scope 的所有活跃规则（含关联标签）
        var rules = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags)
            .Where(r => r.IsActive && r.TargetScope == targetScope)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

        if (rules.Count == 0)
        {
            _logger.LogInformation("没有活跃的标签规则, TargetScope={TargetScope}", targetScope);
            return new RunTagRulesResult { ScannedCount = 0, AddedCount = 0, SkippedCount = 0 };
        }

        // 加载目标实体
        var transactionQuery = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Include(t => t.BankTransaction);

        if (request.EntityIds != null && request.EntityIds.Count > 0)
        {
            transactionQuery = transactionQuery.Where(t => request.EntityIds.Contains(t.Id));
        }

        var transactions = await transactionQuery.ToListAsync();

        // 加载已有绑定（避免重复）
        var transactionIds = transactions.Select(t => t.Id).ToList();
        var existingBindings = await _tagBindingRepository.GetQueryable()
            .Where(b => b.OwnerType == targetScope && transactionIds.Contains(b.OwnerId))
            .Select(b => new { b.OwnerId, b.TagId })
            .ToListAsync();

        var existingBindingSet = new HashSet<(long OwnerId, long TagId)>(
            existingBindings.Select(b => (b.OwnerId, b.TagId)));

        int addedCount = 0;
        int skippedCount = 0;

        foreach (var transaction in transactions)
        {
            foreach (var rule in rules)
            {
                // 从 Transaction + BankTransaction 提取匹配值
                string valueToMatch = rule.MatchField switch
                {
                    RuleMatchField.CounterpartyName => transaction.BankTransaction?.Counterparty ?? string.Empty,
                    RuleMatchField.Counterparty => transaction.BankTransaction?.Counterparty ?? string.Empty,
                    RuleMatchField.Description => transaction.Description ?? transaction.BankTransaction?.Description ?? string.Empty,
                    RuleMatchField.Memo => transaction.BankTransaction?.Memo ?? string.Empty,
                    RuleMatchField.Amount => transaction.Amount.ToString(),
                    _ => string.Empty
                };

                bool isMatch = rule.MatchOperator switch
                {
                    RuleMatchOperator.Contains => valueToMatch.Contains(rule.MatchValue, StringComparison.OrdinalIgnoreCase),
                    RuleMatchOperator.Equals => valueToMatch.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase),
                    RuleMatchOperator.StartsWith => valueToMatch.StartsWith(rule.MatchValue, StringComparison.OrdinalIgnoreCase),
                    RuleMatchOperator.EndsWith => valueToMatch.EndsWith(rule.MatchValue, StringComparison.OrdinalIgnoreCase),
                    RuleMatchOperator.Regex => IsRegexMatch(valueToMatch, rule.MatchValue),
                    _ => false
                };

                if (isMatch)
                {
                    // 将该规则关联的所有标签打到实体上
                    foreach (var ruleTag in rule.TagRuleTags)
                    {
                        if (existingBindingSet.Contains((transaction.Id, ruleTag.TagId)))
                        {
                            skippedCount++;
                            continue;
                        }

                        await _tagBindingRepository.AddAsync(new TagBinding
                        {
                            TagId = ruleTag.TagId,
                            OwnerType = targetScope,
                            OwnerId = transaction.Id
                        });
                        existingBindingSet.Add((transaction.Id, ruleTag.TagId));
                        addedCount++;
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("标签规则重跑完成: Scope={Scope}, 扫描={Scanned}, 新增={Added}, 跳过={Skipped}",
            targetScope, transactions.Count, addedCount, skippedCount);

        return new RunTagRulesResult
        {
            ScannedCount = transactions.Count,
            AddedCount = addedCount,
            SkippedCount = skippedCount
        };
    }

    private static bool IsRegexMatch(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch
        {
            return false;
        }
    }

    private static TagRuleDto MapToDto(TagRule rule) => new TagRuleDto
    {
        Id = rule.Id,
        RuleName = rule.RuleName,
        Priority = rule.Priority,
        TargetScope = rule.TargetScope.ToString(),
        MatchField = rule.MatchField.ToString(),
        MatchOperator = rule.MatchOperator.ToString(),
        MatchValue = rule.MatchValue,
        IsActive = rule.IsActive,
        Tags = rule.TagRuleTags?.Select(rt => new TagRuleTagItemDto
        {
            TagId = rt.TagId,
            TagName = rt.Tag?.Name ?? string.Empty,
            TagColor = rt.Tag?.Color
        }).ToList() ?? new(),
        CreatedAt = rule.CreatedAt
    };
}
```

- [ ] **Step 2: 在 MasterDataModuleExtensions 中注册服务**

在 `MasterDataModuleExtensions.cs` 的 `AddMasterDataModule` 方法中添加：

```csharp
services.AddScoped<ITagRuleService, TagRuleService>();
```

- [ ] **Step 3: 验证编译**

Run: `cd /d/demo/chen/finance_system/backend && dotnet build`
Expected: Build succeeded

- [ ] **Step 4: 提交**

```bash
git add backend/FinanceApp.Application/Modules/MasterData/Services/TagRuleService.cs \
       backend/FinanceApp.Application/Modules/MasterData/MasterDataModuleExtensions.cs
git commit -m "feat: 实现 TagRuleService（CRUD + 规则重跑）"
```

---

### Task 7: API 层 — TagRuleController

**Files:**
- Create: `backend/FinanceApp.Api/Controllers/MasterData/TagRuleController.cs`

- [ ] **Step 1: 创建控制器**

```csharp
// backend/FinanceApp.Api/Controllers/MasterData/TagRuleController.cs
using FinanceApp.Api.Controllers;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/tag-rule")]
[Authorize]
public class TagRuleController : BaseApiController
{
    private readonly ITagRuleService _tagRuleService;
    private readonly ILogger<TagRuleController> _logger;

    public TagRuleController(ITagRuleService tagRuleService, ILogger<TagRuleController> logger)
    {
        _tagRuleService = tagRuleService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PageResponse<TagRuleDto>>>> GetPaged([FromQuery] PageRequest request)
    {
        _logger.LogInformation("[TagRuleController.GetPaged] Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);
        try
        {
            var result = await _tagRuleService.GetPagedAsync(request);
            return Ok(ApiResponse<PageResponse<TagRuleDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TagRuleController.GetPaged] 失败");
            throw;
        }
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TagRuleDto>>> GetById(long id)
    {
        _logger.LogInformation("[TagRuleController.GetById] Id={Id}", id);
        try
        {
            var result = await _tagRuleService.GetByIdAsync(id);
            return Ok(ApiResponse<TagRuleDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TagRuleController.GetById] 失败, Id={Id}", id);
            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TagRuleDto>>> Create([FromBody] CreateTagRuleRequest request)
    {
        _logger.LogInformation("[TagRuleController.Create] RuleName={RuleName}", request.RuleName);
        try
        {
            var result = await _tagRuleService.CreateAsync(request);
            return Ok(ApiResponse<TagRuleDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TagRuleController.Create] 失败, RuleName={RuleName}", request.RuleName);
            throw;
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TagRuleDto>>> Update(long id, [FromBody] UpdateTagRuleRequest request)
    {
        _logger.LogInformation("[TagRuleController.Update] Id={Id}, RuleName={RuleName}", id, request.RuleName);
        try
        {
            var result = await _tagRuleService.UpdateAsync(id, request);
            return Ok(ApiResponse<TagRuleDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TagRuleController.Update] 失败, Id={Id}", id);
            throw;
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        _logger.LogInformation("[TagRuleController.Delete] Id={Id}", id);
        try
        {
            await _tagRuleService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TagRuleController.Delete] 失败, Id={Id}", id);
            throw;
        }
    }

    [HttpPost("run")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RunTagRulesResult>>> RunRules([FromBody] RunTagRulesRequest request)
    {
        _logger.LogInformation("[TagRuleController.RunRules] TargetScope={TargetScope}", request.TargetScope);
        try
        {
            var result = await _tagRuleService.RunRulesAsync(request);
            return Ok(ApiResponse<RunTagRulesResult>.SuccessResponse(result,
                $"重跑完成: 扫描 {result.ScannedCount} 条, 新增 {result.AddedCount} 个标签, 跳过 {result.SkippedCount} 个"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TagRuleController.RunRules] 失败, TargetScope={TargetScope}", request.TargetScope);
            throw;
        }
    }
}
```

- [ ] **Step 2: 验证编译**

Run: `cd /d/demo/chen/finance_system/backend && dotnet build`
Expected: Build succeeded

- [ ] **Step 3: 提交**

```bash
git add backend/FinanceApp.Api/Controllers/MasterData/TagRuleController.cs
git commit -m "feat: 添加 TagRuleController API 端点"
```

---

### Task 8: 后端单元测试

**Files:**
- Create: `backend/tests/FinanceApp.Application.Tests/Services/TagRuleServiceTests.cs`

- [ ] **Step 1: 编写测试**

```csharp
// backend/tests/FinanceApp.Application.Tests/Services/TagRuleServiceTests.cs
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TagRuleServiceTests : TestBase
{
    private readonly Mock<IRepository<TagRule>> _tagRuleRepoMock;
    private readonly Mock<IRepository<Tag>> _tagRepoMock;
    private readonly Mock<IRepository<TagRuleTag>> _tagRuleTagRepoMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepoMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepoMock;
    private readonly TagRuleService _service;

    public TagRuleServiceTests()
    {
        _tagRuleRepoMock = new Mock<IRepository<TagRule>>();
        _tagRepoMock = new Mock<IRepository<Tag>>();
        _tagRuleTagRepoMock = new Mock<IRepository<TagRuleTag>>();
        _tagBindingRepoMock = new Mock<IRepository<TagBinding>>();
        _transactionRepoMock = new Mock<IRepository<Transaction>>();

        _service = new TagRuleService(
            _tagRuleRepoMock.Object,
            _tagRepoMock.Object,
            _tagRuleTagRepoMock.Object,
            _tagBindingRepoMock.Object,
            _transactionRepoMock.Object,
            Mock.Of<ILogger<TagRuleService>>(),
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateRule()
    {
        // Arrange
        var tag = new Tag { Id = 1, Scope = TagScope.Transaction, Name = "高优先级", IsActive = true };
        var request = new CreateTagRuleRequest
        {
            RuleName = "标记大额交易",
            Priority = 100,
            TargetScope = "Transaction",
            MatchField = "Amount",
            MatchOperator = "Regex",
            MatchValue = @"^\d{5,}",
            TagIds = new List<long> { 1 }
        };

        _tagRepoMock.Setup(x => x.GetQueryable())
            .Returns(new List<Tag> { tag }.AsQueryable().BuildMockDbSet().Object);
        _tagRuleRepoMock.Setup(x => x.AddAsync(It.IsAny<TagRule>()))
            .ReturnsAsync((TagRule r) => { r.Id = 10; return r; });
        _tagRuleTagRepoMock.Setup(x => x.AddAsync(It.IsAny<TagRuleTag>()))
            .ReturnsAsync((TagRuleTag rt) => rt);

        // 重新加载时返回完整对象
        var createdRule = new TagRule
        {
            Id = 10, RuleName = "标记大额交易", Priority = 100,
            TargetScope = TagScope.Transaction,
            MatchField = RuleMatchField.Amount,
            MatchOperator = RuleMatchOperator.Regex,
            MatchValue = @"^\d{5,}",
            IsActive = true,
            TagRuleTags = new List<TagRuleTag>
            {
                new() { TagRuleId = 10, TagId = 1, Tag = tag }
            }
        };
        _tagRuleRepoMock.Setup(x => x.GetQueryable())
            .Returns(new List<TagRule> { createdRule }.AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.RuleName.Should().Be("标记大额交易");
        result.Priority.Should().Be(100);
        result.TargetScope.Should().Be("Transaction");
        result.Tags.Should().HaveCount(1);
        result.Tags[0].TagName.Should().Be("高优先级");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidScope_ShouldThrowValidation()
    {
        // Arrange
        var request = new CreateTagRuleRequest
        {
            RuleName = "测试", TargetScope = "InvalidScope",
            MatchField = "Description", MatchOperator = "Contains",
            MatchValue = "test", TagIds = new List<long> { 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithNoTags_ShouldThrowValidation()
    {
        // Arrange
        var request = new CreateTagRuleRequest
        {
            RuleName = "测试", TargetScope = "Transaction",
            MatchField = "Description", MatchOperator = "Contains",
            MatchValue = "test", TagIds = new List<long>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRegex_ShouldThrowValidation()
    {
        // Arrange
        var request = new CreateTagRuleRequest
        {
            RuleName = "测试", TargetScope = "Transaction",
            MatchField = "Description", MatchOperator = "Regex",
            MatchValue = "[invalid(", TagIds = new List<long> { 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_ExistingRule_ShouldSoftDelete()
    {
        // Arrange
        var rule = new TagRule { Id = 1, RuleName = "测试规则", IsActive = true };
        _tagRuleRepoMock.Setup(x => x.GetByIdAsync(1L)).ReturnsAsync(rule);

        // Act
        await _service.DeleteAsync(1L);

        // Assert
        rule.IsDeleted.Should().BeTrue();
        rule.DeletedAt.Should().NotBeNull();
        _tagRuleRepoMock.Verify(x => x.Update(rule), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentRule_ShouldThrowNotFound()
    {
        // Arrange
        _tagRuleRepoMock.Setup(x => x.GetByIdAsync(999L)).ReturnsAsync((TagRule?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999L));
    }
}
```

> **注意**：由于 Moq 无法直接 Mock `IQueryable` 的 `Include` 和 EF 扩展方法，部分测试需要使用 `MockQueryable.Moq` 包（`BuildMockDbSet()`）。如果项目中尚未安装此包，需先添加：`dotnet add tests/FinanceApp.Application.Tests package MockQueryable.Moq`。如果已安装其他 Mock IQueryable 方案，按项目现有模式调整。

- [ ] **Step 2: 运行测试**

Run: `cd /d/demo/chen/finance_system/backend && dotnet test tests/FinanceApp.Application.Tests --filter "FullyQualifiedName~TagRuleServiceTests" -v normal`
Expected: All tests pass

- [ ] **Step 3: 提交**

```bash
git add backend/tests/FinanceApp.Application.Tests/Services/TagRuleServiceTests.cs
git commit -m "test: 添加 TagRuleService 单元测试"
```

---

### Task 9: 前端 — 类型定义和 API 模块

**Files:**
- Create: `frontend/src/features/reconciliation/types/tagRule.ts`
- Create: `frontend/src/features/reconciliation/api/tagRule.ts`

- [ ] **Step 1: 创建类型定义**

```typescript
// frontend/src/features/reconciliation/types/tagRule.ts

export interface TagRuleTagItem {
  tagId: number
  tagName: string
  tagColor?: string
}

export interface TagRule {
  id: number
  ruleName: string
  priority: number
  targetScope: string
  matchField: string
  matchOperator: string
  matchValue: string
  isActive: boolean
  tags: TagRuleTagItem[]
  createdAt: string
}

export interface CreateTagRuleRequest {
  ruleName: string
  priority: number
  targetScope: string
  matchField: string
  matchOperator: string
  matchValue: string
  tagIds: number[]
  newTagNames: string[]
}

export interface UpdateTagRuleRequest {
  ruleName: string
  priority: number
  targetScope: string
  matchField: string
  matchOperator: string
  matchValue: string
  isActive: boolean
  tagIds: number[]
  newTagNames: string[]
}

export interface RunTagRulesRequest {
  targetScope: string
  entityIds?: number[]
}

export interface RunTagRulesResult {
  scannedCount: number
  addedCount: number
  skippedCount: number
}
```

- [ ] **Step 2: 创建 API 模块**

```typescript
// frontend/src/features/reconciliation/api/tagRule.ts
import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  TagRule,
  CreateTagRuleRequest,
  UpdateTagRuleRequest,
  RunTagRulesRequest,
  RunTagRulesResult
} from '@/features/reconciliation/types/tagRule'

const api = createCrudApi<TagRule, CreateTagRuleRequest, UpdateTagRuleRequest>({
  baseUrl: '/tag-rule'
})

export const getTagRules = api.getList
export const getTagRuleById = api.getById
export const createTagRule = api.create
export const updateTagRule = api.update
export const deleteTagRule = api.remove

export const runTagRules = (data: RunTagRulesRequest) =>
  request<ApiResponse<RunTagRulesResult>>({ url: '/tag-rule/run', method: 'post', data })
```

- [ ] **Step 3: 提交**

```bash
git add frontend/src/features/reconciliation/types/tagRule.ts \
       frontend/src/features/reconciliation/api/tagRule.ts
git commit -m "feat: 添加标签规则前端类型定义和 API 模块"
```

---

### Task 10: 前端 — 标签规则表单弹窗

**Files:**
- Create: `frontend/src/features/reconciliation/components/TagRuleForm.vue`

- [ ] **Step 1: 创建组件**

```vue
<!-- frontend/src/features/reconciliation/components/TagRuleForm.vue -->
<template>
  <el-dialog
    v-model="visible"
    :title="isEdit ? '编辑标签规则' : '新增标签规则'"
    width="600px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px">
      <el-form-item label="规则名称" prop="ruleName">
        <el-input v-model="formData.ruleName" placeholder="请输入规则名称" />
      </el-form-item>

      <el-form-item label="目标范围" prop="targetScope">
        <el-select v-model="formData.targetScope" placeholder="请选择" :disabled="isEdit" style="width: 100%">
          <el-option label="交易" value="Transaction" />
        </el-select>
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 2px">
          当前仅支持"交易"范围
        </div>
      </el-form-item>

      <el-form-item label="匹配字段" prop="matchField">
        <el-select v-model="formData.matchField" placeholder="请选择" style="width: 100%">
          <el-option
            v-for="field in availableMatchFields"
            :key="field.value"
            :label="field.label"
            :value="field.value"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="匹配方式" prop="matchOperator">
        <el-select v-model="formData.matchOperator" placeholder="请选择" style="width: 100%">
          <el-option label="包含" value="Contains" />
          <el-option label="精确匹配" value="Equals" />
          <el-option label="正则表达式" value="Regex" />
          <el-option label="开头匹配" value="StartsWith" />
          <el-option label="结尾匹配" value="EndsWith" />
        </el-select>
      </el-form-item>

      <el-form-item label="匹配值" prop="matchValue">
        <el-input v-model="formData.matchValue" placeholder="请输入匹配值" />
      </el-form-item>

      <el-form-item label="关联标签" prop="selectedTagIds">
        <el-select
          v-model="formData.selectedTagIds"
          multiple
          filterable
          allow-create
          default-first-option
          placeholder="选择或输入新标签名"
          style="width: 100%"
          @change="handleTagChange"
        >
          <el-option
            v-for="tag in availableTags"
            :key="tag.id"
            :label="tag.name"
            :value="tag.id"
          >
            <span style="display: inline-flex; align-items: center; gap: 6px">
              <span
                v-if="tag.color"
                :style="{ background: tag.color, width: '12px', height: '12px', borderRadius: '2px', display: 'inline-block' }"
              />
              {{ tag.name }}
            </span>
          </el-option>
        </el-select>
      </el-form-item>

      <el-form-item label="优先级" prop="priority">
        <el-input-number v-model="formData.priority" :min="0" :max="999" style="width: 100%" />
      </el-form-item>

      <el-form-item v-if="isEdit" label="状态">
        <el-switch v-model="formData.isActive" active-text="启用" inactive-text="停用" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEdit ? '保存' : '创建' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'
import { createTagRule, updateTagRule } from '@/features/reconciliation/api/tagRule'
import { getTags } from '@/features/master-data/tags/api/tag'
import type { TagRule } from '@/features/reconciliation/types/tagRule'
import type { Tag } from '@/features/master-data/tags/types/tag'

interface Props {
  modelValue: boolean
  rule?: TagRule | null
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const formRef = ref<FormInstance>()
const submitting = ref(false)
const availableTags = ref<Tag[]>([])

const isEdit = computed(() => !!props.rule)

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const matchFieldOptions: Record<string, { label: string; value: string }[]> = {
  Transaction: [
    { label: '对方名称', value: 'CounterpartyName' },
    { label: '描述/摘要', value: 'Description' },
    { label: '备注', value: 'Memo' },
    { label: '金额', value: 'Amount' }
  ]
}

const availableMatchFields = computed(() =>
  matchFieldOptions[formData.targetScope] || []
)

const formData = reactive({
  ruleName: '',
  targetScope: 'Transaction',
  matchField: 'CounterpartyName',
  matchOperator: 'Contains',
  matchValue: '',
  priority: 0,
  isActive: true,
  selectedTagIds: [] as (number | string)[]
})

const rules: FormRules = {
  ruleName: [{ required: true, message: '请输入规则名称', trigger: 'blur' }],
  targetScope: [{ required: true, message: '请选择目标范围', trigger: 'change' }],
  matchField: [{ required: true, message: '请选择匹配字段', trigger: 'change' }],
  matchOperator: [{ required: true, message: '请选择匹配方式', trigger: 'change' }],
  matchValue: [{ required: true, message: '请输入匹配值', trigger: 'blur' }],
  selectedTagIds: [{ required: true, type: 'array', min: 1, message: '请至少选择一个标签', trigger: 'change' }]
}

const handleTagChange = (val: (number | string)[]) => {
  formData.selectedTagIds = val
}

const loadTags = async () => {
  try {
    const scope = formData.targetScope.toLowerCase()
    const { data } = await getTags(scope, true)
    availableTags.value = data.data || []
  } catch {
    availableTags.value = []
  }
}

const resetForm = () => {
  formData.ruleName = ''
  formData.targetScope = 'Transaction'
  formData.matchField = 'CounterpartyName'
  formData.matchOperator = 'Contains'
  formData.matchValue = ''
  formData.priority = 0
  formData.isActive = true
  formData.selectedTagIds = []
}

const fillForm = (rule: TagRule) => {
  formData.ruleName = rule.ruleName
  formData.targetScope = rule.targetScope
  formData.matchField = rule.matchField
  formData.matchOperator = rule.matchOperator
  formData.matchValue = rule.matchValue
  formData.priority = rule.priority
  formData.isActive = rule.isActive
  formData.selectedTagIds = rule.tags.map(t => t.tagId)
}

watch(visible, (val) => {
  if (val) {
    loadTags()
    if (props.rule) {
      fillForm(props.rule)
    } else {
      resetForm()
    }
  }
})

const handleSubmit = async () => {
  await formRef.value?.validate()
  submitting.value = true

  try {
    // 分离已有标签 ID 和新标签名
    const tagIds = formData.selectedTagIds.filter(v => typeof v === 'number') as number[]
    const newTagNames = formData.selectedTagIds.filter(v => typeof v === 'string') as string[]

    if (isEdit.value && props.rule) {
      await updateTagRule(props.rule.id, {
        ruleName: formData.ruleName,
        targetScope: formData.targetScope,
        matchField: formData.matchField,
        matchOperator: formData.matchOperator,
        matchValue: formData.matchValue,
        priority: formData.priority,
        isActive: formData.isActive,
        tagIds,
        newTagNames
      })
      ElMessage.success('标签规则更新成功')
    } else {
      await createTagRule({
        ruleName: formData.ruleName,
        targetScope: formData.targetScope,
        matchField: formData.matchField,
        matchOperator: formData.matchOperator,
        matchValue: formData.matchValue,
        priority: formData.priority,
        tagIds,
        newTagNames
      })
      ElMessage.success('标签规则创建成功')
    }
    emit('success')
    handleClose()
  } catch (error: any) {
    console.error('保存标签规则失败:', error)
  } finally {
    submitting.value = false
  }
}

const handleClose = () => {
  formRef.value?.resetFields()
  resetForm()
  visible.value = false
}
</script>
```

- [ ] **Step 2: 提交**

```bash
git add frontend/src/features/reconciliation/components/TagRuleForm.vue
git commit -m "feat: 添加标签规则表单弹窗组件"
```

---

### Task 11: 前端 — 标签规则重跑弹窗

**Files:**
- Create: `frontend/src/features/reconciliation/components/TagRuleRerunDialog.vue`

- [ ] **Step 1: 创建组件**

```vue
<!-- frontend/src/features/reconciliation/components/TagRuleRerunDialog.vue -->
<template>
  <el-dialog
    v-model="visible"
    title="标签规则重跑"
    width="500px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-form :model="form" label-width="100px" style="max-width: 400px">
      <el-form-item label="目标范围">
        <el-select v-model="form.targetScope" placeholder="请选择" style="width: 100%">
          <el-option label="交易" value="Transaction" />
        </el-select>
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 2px">
          对所有交易记录执行活跃的标签规则
        </div>
      </el-form-item>
    </el-form>

    <!-- 执行结果 -->
    <el-result v-if="result" :icon="result.addedCount > 0 ? 'success' : 'info'" style="padding: 20px 0">
      <template #title>重跑完成</template>
      <template #sub-title>
        <p>扫描 {{ result.scannedCount }} 条记录</p>
        <p>新增 {{ result.addedCount }} 个标签绑定</p>
        <p>跳过 {{ result.skippedCount }} 个（已存在）</p>
      </template>
    </el-result>

    <template #footer>
      <el-button @click="handleClose">{{ result ? '关闭' : '取消' }}</el-button>
      <el-button v-if="!result" type="primary" :loading="running" @click="handleRun">
        执行重跑
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { runTagRules } from '@/features/reconciliation/api/tagRule'
import type { RunTagRulesResult } from '@/features/reconciliation/types/tagRule'

interface Props {
  modelValue: boolean
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const running = ref(false)
const result = ref<RunTagRulesResult | null>(null)

const form = reactive({
  targetScope: 'Transaction'
})

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const handleRun = async () => {
  try {
    await ElMessageBox.confirm(
      '将对所有交易记录执行标签规则匹配，已存在的标签不会被重复添加。确认执行？',
      '确认标签规则重跑',
      { confirmButtonText: '确认执行', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return
  }

  running.value = true
  try {
    const { data } = await runTagRules({ targetScope: form.targetScope })
    result.value = data.data!
    ElMessage.success(data.message || '标签规则重跑完成')
    emit('success')
  } catch (error: any) {
    console.error('标签规则重跑失败:', error)
    ElMessage.error('标签规则重跑失败')
  } finally {
    running.value = false
  }
}

const handleClose = () => {
  result.value = null
  form.targetScope = 'Transaction'
  visible.value = false
}

watch(visible, (val) => {
  if (!val) handleClose()
})
</script>
```

- [ ] **Step 2: 提交**

```bash
git add frontend/src/features/reconciliation/components/TagRuleRerunDialog.vue
git commit -m "feat: 添加标签规则重跑弹窗组件"
```

---

### Task 12: 前端 — 标签规则列表页

**Files:**
- Create: `frontend/src/features/reconciliation/pages/TagRuleListPage.vue`

- [ ] **Step 1: 创建列表页**

```vue
<!-- frontend/src/features/reconciliation/pages/TagRuleListPage.vue -->
<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h2>标签规则管理</h2>
        <p class="page-description">配置自动标签规则，基于匹配条件为数据自动打标签</p>
      </div>
      <div v-if="userStore.isAdmin" class="page-actions">
        <el-button type="primary" @click="handleAdd">新增规则</el-button>
        <el-button @click="rerunVisible = true">规则重跑</el-button>
      </div>
    </div>

    <el-table
      :data="tableData"
      v-loading="loading"
      border
      stripe
      style="width: 100%"
      @sort-change="handleSortChange"
    >
      <el-table-column prop="ruleName" label="规则名称" min-width="160" sortable="custom" show-overflow-tooltip />
      <el-table-column label="目标范围" width="100">
        <template #default="{ row }">
          {{ scopeLabels[row.targetScope] || row.targetScope }}
        </template>
      </el-table-column>
      <el-table-column label="匹配字段" width="110">
        <template #default="{ row }">
          {{ fieldLabels[row.matchField] || row.matchField }}
        </template>
      </el-table-column>
      <el-table-column label="匹配方式" width="100">
        <template #default="{ row }">
          {{ operatorLabels[row.matchOperator] || row.matchOperator }}
        </template>
      </el-table-column>
      <el-table-column prop="matchValue" label="匹配值" min-width="140" show-overflow-tooltip />
      <el-table-column label="关联标签" min-width="200">
        <template #default="{ row }">
          <el-tag
            v-for="tag in row.tags"
            :key="tag.tagId"
            :color="tag.tagColor"
            :style="tag.tagColor ? { color: '#fff', borderColor: tag.tagColor } : {}"
            size="small"
            style="margin-right: 4px; margin-bottom: 2px"
          >
            {{ tag.tagName }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="priority" label="优先级" width="90" sortable="custom" align="center" />
      <el-table-column label="状态" width="80" align="center">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
            {{ row.isActive ? '启用' : '停用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column v-if="userStore.isAdmin" label="操作" width="160" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary" link size="small" @click="handleEdit(row)">编辑</el-button>
          <el-button type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <div class="pagination-container">
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadData"
        @current-change="loadData"
      />
    </div>

    <TagRuleForm
      v-model="formVisible"
      :rule="currentRule"
      @success="loadData"
    />

    <TagRuleRerunDialog
      v-model="rerunVisible"
      @success="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getTagRules, deleteTagRule } from '@/features/reconciliation/api/tagRule'
import type { TagRule } from '@/features/reconciliation/types/tagRule'
import TagRuleForm from '@/features/reconciliation/components/TagRuleForm.vue'
import TagRuleRerunDialog from '@/features/reconciliation/components/TagRuleRerunDialog.vue'
import { useUserStore } from '@/features/auth/stores/user'

const userStore = useUserStore()
const loading = ref(false)
const tableData = ref<TagRule[]>([])
const currentRule = ref<TagRule | null>(null)
const formVisible = ref(false)
const rerunVisible = ref(false)
const pagination = reactive({ page: 1, pageSize: 20, total: 0 })
const sortState = reactive({ sortBy: '', sortOrder: '' as '' | 'asc' | 'desc' })

const scopeLabels: Record<string, string> = {
  Transaction: '交易',
  Project: '项目',
  Person: '人员',
  Customer: '客户',
  Supplier: '供应商'
}

const fieldLabels: Record<string, string> = {
  CounterpartyName: '对方名称',
  Counterparty: '对方名称',
  Description: '描述/摘要',
  Memo: '备注',
  Amount: '金额'
}

const operatorLabels: Record<string, string> = {
  Contains: '包含',
  Equals: '精确匹配',
  Regex: '正则表达式',
  StartsWith: '开头匹配',
  EndsWith: '结尾匹配'
}

const loadData = async () => {
  loading.value = true
  try {
    const { data } = await getTagRules({
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sortState.sortBy,
      sortOrder: sortState.sortOrder
    })
    const result = data.data
    tableData.value = result.items || []
    pagination.total = result.total || 0
  } catch (error: any) {
    console.error('加载标签规则失败:', error)
  } finally {
    loading.value = false
  }
}

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = prop || ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  loadData()
}

const handleAdd = () => {
  currentRule.value = null
  formVisible.value = true
}

const handleEdit = (rule: TagRule) => {
  currentRule.value = rule
  formVisible.value = true
}

const handleDelete = async (rule: TagRule) => {
  try {
    await ElMessageBox.confirm(
      `确定删除标签规则「${rule.ruleName}」吗？`,
      '确认删除',
      { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning' }
    )
    await deleteTagRule(rule.id)
    ElMessage.success('删除成功')
    loadData()
  } catch {
    // 用户取消
  }
}

onMounted(loadData)
</script>
```

- [ ] **Step 2: 提交**

```bash
git add frontend/src/features/reconciliation/pages/TagRuleListPage.vue
git commit -m "feat: 添加标签规则列表页面"
```

---

### Task 13: 前端 — 注册路由

**Files:**
- Modify: `frontend/src/features/reconciliation/routes.ts`

- [ ] **Step 1: 添加标签规则路由**

在 `reconciliationRoutes` 数组中，在现有 `rules` 路由后面添加：

```typescript
{
  path: 'tag-rules',
  name: 'TagRules',
  component: () => import('@/features/reconciliation/pages/TagRuleListPage.vue'),
  meta: { title: '标签规则', roles: PermissionGroups.ADMIN_ONLY, icon: 'PriceTag', group: '自动化', order: 12 }
}
```

- [ ] **Step 2: 验证前端编译**

Run: `cd /d/demo/chen/finance_system/frontend && npm run build`
Expected: Build succeeded（或仅有不影响功能的警告）

- [ ] **Step 3: 提交**

```bash
git add frontend/src/features/reconciliation/routes.ts
git commit -m "feat: 注册标签规则前端路由"
```

---

### Task 14: 验证端到端功能

- [ ] **Step 1: 启动后端**

Run: `cd /d/demo/chen/finance_system/backend && dotnet run --project FinanceApp.Api`
Expected: 应用启动成功，无错误

- [ ] **Step 2: 启动前端**

Run: `cd /d/demo/chen/finance_system/frontend && npm run dev`
Expected: 开发服务器启动

- [ ] **Step 3: 验证 API**

- 登录获取 session
- `GET /api/tag-rule` — 返回空分页列表
- `POST /api/tag-rule` — 创建一条标签规则，关联已有标签
- `GET /api/tag-rule/{id}` — 返回含标签详情的规则
- `PUT /api/tag-rule/{id}` — 更新规则
- `POST /api/tag-rule/run` — 执行重跑
- `DELETE /api/tag-rule/{id}` — 删除规则

- [ ] **Step 4: 验证前端页面**

- 访问 `/tag-rules` — 列表页正常渲染
- 新增规则 — 表单弹窗正常，可选择/创建标签
- 编辑规则 — 数据回填正确
- 规则重跑 — 弹窗正常，执行后显示结果

- [ ] **Step 5: 运行全部后端测试**

Run: `cd /d/demo/chen/finance_system/backend && dotnet test`
Expected: All tests pass

- [ ] **Step 6: 最终提交**

确认所有功能正常后，如有遗漏的文件变更，补充提交。
