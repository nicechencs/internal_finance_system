# 数据权限控制完整实施方案

> 文档版本：v1.0  
> 创建日期：2026-03-14  
> 适用系统：财务管理系统（开发阶段，无历史数据）

---

## 目录

1. [方案概述](#1-方案概述)
2. [技术基础分析](#2-技术基础分析)
3. [权限模型设计](#3-权限模型设计)
4. [实施方案](#4-实施方案)
5. [数据库设计](#5-数据库设计)
6. [后端实现](#6-后端实现)
7. [前端实现](#7-前端实现)
8. [测试方案](#8-测试方案)
9. [风险评估](#9-风险评估)
10. [实施计划](#10-实施计划)

---

## 1. 方案概述

### 1.1 目标

实现一套完整的、企业级的数据权限控制系统，包括：
- **基于角色的访问控制（RBAC）**：Admin、Accountant、Viewer 三种角色
- **基于数据所有权的行级权限**：用户只能访问自己创建或被授权的数据
- **基于业务规则的字段级权限**：敏感字段根据角色脱敏
- **前后端一致的权限控制**：后端强制，前端优化体验

### 1.2 设计原则

1. **安全优先**：后端强制权限检查，前端权限仅为 UX 优化
2. **最小权限**：默认拒绝，显式授权
3. **可扩展性**：支持未来扩展部门权限、项目成员权限
4. **性能优先**：使用 EF Core Query Filter 在数据库层面过滤
5. **审计完整**：所有权限操作记录审计日志

### 1.3 适用场景

- ✅ 开发阶段，无历史数据
- ✅ 小到中型团队（5-50 人）
- ✅ 需要数据隔离的财务系统
- ✅ 需要审计合规的企业应用

---

## 2. 技术基础分析

### 2.1 当前系统现状

| 项目 | 状态 | 说明 |
|------|------|------|
| JWT 认证 | ✅ 已有 | Token 包含 UserId、Username、Email、Role |
| 用户角色 | ⚠️ 字符串 | Role 为 string 类型，值为 "admin" 或 "viewer" |
| Controller 授权 | ⚠️ 仅认证 | 所有 Controller 有 `[Authorize]`，无角色区分 |
| Service 数据过滤 | ❌ 缺失 | 所有查询返回全部数据 |
| CreatedBy 字段 | ⚠️ 仅 2 个实体 | Transaction 和 ImportBatch 有，其他 15 个实体没有 |
| 前端权限 | ❌ 缺失 | 路由守卫只检查登录状态 |

### 2.2 实体 CreatedBy 字段情况

**有 CreatedBy 的实体（2 个）：**
- Transaction（交易）
- ImportBatch（导入批次）

**需要添加 CreatedBy 的实体（15 个）：**
- Account（账户）
- Category（分类）
- ClassificationRule（分类规则）
- Customer（客户）
- Supplier（供应商）
- Person（人员）
- Project（项目）
- Receivable（应收）
- ReceivableDetail（应收明细）
- Payable（应付）
- PayableDetail（应付明细）
- BankTransaction（银行流水）
- TransactionAllocation（交易分摊）
- SystemConfig（系统配置）
- AuditLog（审计日志）

**不需要 CreatedBy 的实体（1 个）：**
- User（用户表本身）

### 2.3 Service 层查询模式

所有 Service 都使用统一的查询模式：
```csharp
var query = _repository.GetQueryable()
    .Include(...)  // 关联加载
    .OrderByDescending(x => x.CreatedAt);
```

**需要权限过滤的 Service 方法统计：**
- TransactionService: 8 个方法
- AccountService: 3 个方法
- ProjectService: 3 个方法
- CustomerService: 3 个方法
- SupplierService: 3 个方法
- PersonService: 2 个方法
- CategoryService: 3 个方法
- RuleService: 3 个方法
- ReceivableService: 2 个方法
- PayableService: 2 个方法
- DashboardService: 5 个方法
- ReportService: 6 个方法

**总计：45 个方法需要添加权限过滤**

### 2.4 Controller 端点统计

| Controller | 端点数 | 当前授权 |
|-----------|--------|---------|
| TransactionsController | 11 | [Authorize] |
| AccountController | 8 | [Authorize] |
| ProjectsController | 8 | [Authorize] |
| CustomerController | 7 | [Authorize] |
| SupplierController | 7 | [Authorize] |
| PersonController | 7 | [Authorize] |
| ReceivablesController | 6 | [Authorize] |
| PayablesController | 6 | [Authorize] |
| CategoryController | 6 | [Authorize] |
| RuleController | 6 | [Authorize] |
| DashboardController | 5 | [Authorize] |
| ReportController | 6 | [Authorize] |
| ImportController | 4 | [Authorize] |
| ConfigController | 4 | [Authorize] |
| AuthController | 3 | 部分公开 |

**总计：94 个端点需要添加角色授权**

---

## 3. 权限模型设计

### 3.1 角色定义

```csharp
public enum UserRole
{
    Admin = 1,      // 管理员：全部权限
    Accountant = 2, // 会计：财务数据权限
    Viewer = 3      // 查看者：只读权限，仅看自己的数据
}
```

### 3.2 角色权限矩阵

| 功能模块 | Admin | Accountant | Viewer |
|---------|-------|------------|--------|
| **交易管理** | 全部 | 全部 | 仅自己创建的（只读） |
| **应收应付** | 全部 | 全部 | 仅自己创建的（只读） |
| **账户管理** | 全部 | 查看+编辑 | 仅查看 |
| **项目管理** | 全部 | 查看+编辑 | 仅查看 |
| **客户/供应商/人员** | 全部 | 查看+编辑 | 仅查看 |
| **分类/规则** | 全部 | 查看 | 仅查看 |
| **仪表盘** | 全部数据 | 全部数据 | 仅自己的数据 |
| **报表系统** | 全部数据 | 全部数据 | 仅自己的数据 |
| **导入导出** | ✅ | ✅ | ❌ |
| **用户管理** | ✅ | ❌ | ❌ |
| **系统配置** | ✅ | ❌ | ❌ |

### 3.3 数据权限规则

#### 规则 1：Admin 角色
- 可以查看和操作所有数据
- 无任何数据过滤

#### 规则 2：Accountant 角色
- 可以查看所有财务数据
- 可以创建/编辑交易、应收应付
- 可以查看和编辑账户、项目、客户、供应商、人员
- 不能删除关键数据（需 Admin 权限）
- 不能管理用户和系统配置

#### 规则 3：Viewer 角色
- 只能查看自己创建的交易、应收应付
- 可以查看所有账户、项目、客户、供应商、人员（只读）
- 不能创建/编辑/删除任何数据
- 不能导入导出
- 仪表盘和报表只显示自己的数据

### 3.4 数据过滤策略

```csharp
// 伪代码示例
if (currentUser.Role == UserRole.Viewer)
{
    // 对于交易、应收应付：只能看到自己创建的
    query = query.Where(x => x.CreatedBy == currentUser.Id);
}
else if (currentUser.Role == UserRole.Accountant)
{
    // 可以看到所有数据
    // 无过滤
}
else if (currentUser.Role == UserRole.Admin)
{
    // 可以看到所有数据
    // 无过滤
}
```

---

## 4. 实施方案

### 4.1 方案选择

**选择方案：完整的 RBAC + 数据所有权（方案 B）**

理由：
1. ✅ 开发阶段，无历史数据包袱
2. ✅ 一步到位，避免后续重构
3. ✅ 满足企业级应用标准
4. ✅ 支持未来扩展（部门权限、项目成员权限）

### 4.2 实施范围

| 层级 | 影响文件数 | 主要工作 |
|------|-----------|---------|
| Domain 层 | 18 | 新增 UserRole 枚举，修改 User 实体，15 个实体添加 CreatedBy |
| Infrastructure 层 | 18 | 修改 17 个 Configuration，修改 Repository，新增 ICurrentUserService |
| Application 层 | 15 | 修改 12 个 Service，新增 IDataPermissionService |
| API 层 | 15 | 修改 15 个 Controller，新增 CurrentUserService |
| 前端 | 20 | 修改 store、router、types，添加权限指令，修改 views |
| 数据库 | 1 | 迁移脚本（添加 CreatedBy 字段） |
| 测试 | 10 | 单元测试、集成测试 |
| **总计** | **97** | |

### 4.3 技术选型

| 技术点 | 选择 | 说明 |
|--------|------|------|
| 角色存储 | UserRole 枚举 | 类型安全，编译时检查 |
| 数据过滤 | EF Core Query Filter | 数据库层面过滤，性能最优 |
| 当前用户获取 | IHttpContextAccessor | ASP.NET Core 标准方式 |
| 权限检查 | 自定义 IDataPermissionService | 灵活，易于扩展 |
| 前端权限 | 自定义 v-permission 指令 | 声明式，易于使用 |

---

## 5. 数据库设计

### 5.1 BaseEntity 扩展

修改 `Domain/Entities/BaseEntity.cs`：

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // 新增字段
    public int CreatedBy { get; set; }  // 创建人 ID

    // 导航属性（可选）
    public virtual User? Creator { get; set; }
}
```

### 5.2 User 实体修改

修改 `Domain/Entities/User.cs`：

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // 修改：从 string 改为 UserRole 枚举
    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 5.3 UserRole 枚举

新建 `Domain/Enums/UserRole.cs`：

```csharp
namespace FinanceApp.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Accountant = 2,
    Viewer = 3
}
```

### 5.4 数据库迁移脚本

创建迁移脚本 `docs/02_Database/03_add_permission_control.sql`：

```sql
-- 1. 添加 UserRole 枚举类型
DO $$ BEGIN
    CREATE TYPE user_role AS ENUM ('Admin', 'Accountant', 'Viewer');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- 2. 修改 users 表
ALTER TABLE users 
    ALTER COLUMN role TYPE user_role USING role::user_role,
    ALTER COLUMN role SET DEFAULT 'Viewer';

-- 3. 为所有业务表添加 created_by 字段
ALTER TABLE accounts ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE categories ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE classification_rules ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE customers ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE suppliers ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE persons ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE projects ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE receivables ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE receivable_payments ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE payables ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE payable_payments ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE bank_statements ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE transaction_allocations ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;
ALTER TABLE excel_import_logs ADD COLUMN created_by INTEGER NOT NULL DEFAULT 1;

-- 4. 添加外键约束
ALTER TABLE accounts ADD CONSTRAINT fk_accounts_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE categories ADD CONSTRAINT fk_categories_created_by FOREIGN KEY (created_by) REFERENCES users(id);
-- ... 其他表的外键约束

-- 5. 创建索引
CREATE INDEX idx_accounts_created_by ON accounts(created_by) WHERE is_deleted = false;
CREATE INDEX idx_transactions_created_by ON transactions(created_by) WHERE is_deleted = false;
-- ... 其他表的索引

-- 6. 更新现有数据（将所有记录的 created_by 设置为管理员用户 ID）
UPDATE accounts SET created_by = 1 WHERE created_by IS NULL;
UPDATE transactions SET created_by = 1 WHERE created_by IS NULL;
-- ... 其他表的更新
```

---

## 6. 后端实现

### 6.1 Domain 层

#### 6.1.1 新增 UserRole 枚举

文件：`backend/FinanceApp.Domain/Enums/UserRole.cs`

```csharp
namespace FinanceApp.Domain.Enums;

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 管理员：全部权限
    /// </summary>
    Admin = 1,

    /// <summary>
    /// 会计：财务数据权限
    /// </summary>
    Accountant = 2,

    /// <summary>
    /// 查看者：只读权限，仅看自己的数据
    /// </summary>
    Viewer = 3
}
```

#### 6.1.2 修改 User 实体

文件：`backend/FinanceApp.Domain/Entities/User.cs`

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // 修改：从 string 改为 UserRole 枚举
    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### 6.1.3 修改 BaseEntity

文件：`backend/FinanceApp.Domain/Entities/BaseEntity.cs`

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // 新增字段
    public int CreatedBy { get; set; }

    // 导航属性
    public virtual User? Creator { get; set; }
}
```

### 6.2 Infrastructure 层

#### 6.2.1 新增 ICurrentUserService 接口

文件：`backend/FinanceApp.Domain/Interfaces/ICurrentUserService.cs`

```csharp
namespace FinanceApp.Domain.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    UserRole Role { get; }
    bool IsAdmin { get; }
    bool IsAccountant { get; }
    bool IsViewer { get; }
}
```

#### 6.2.2 实现 CurrentUserService

文件：`backend/FinanceApp.Api/Services/CurrentUserService.cs`

```csharp
using System.Security.Claims;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public string Username
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Viewer;
        }
    }

    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsAccountant => Role == UserRole.Accountant;
    public bool IsViewer => Role == UserRole.Viewer;
}
```

#### 6.2.3 修改 ApplicationDbContext

文件：`backend/FinanceApp.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // DbSet 定义...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用所有配置
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // 配置全局查询过滤器
        ConfigureGlobalQueryFilters(modelBuilder);
    }

    private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // 软删除过滤器（所有实体）
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    Expression.Lambda(body, parameter));
            }
        }

        // 数据权限过滤器（仅 Viewer 角色）
        if (_currentUserService.IsViewer)
        {
            // 交易相关实体：只能看到自己创建的
            modelBuilder.Entity<Transaction>().HasQueryFilter(t => 
                t.CreatedBy == _currentUserService.UserId);
            modelBuilder.Entity<Receivable>().HasQueryFilter(r => 
                r.CreatedBy == _currentUserService.UserId);
            modelBuilder.Entity<Payable>().HasQueryFilter(p => 
                p.CreatedBy == _currentUserService.UserId);
            modelBuilder.Entity<BankStatement>().HasQueryFilter(b => 
                b.CreatedBy == _currentUserService.UserId);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动设置 CreatedBy
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = _currentUserService.UserId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

### 6.3 Application 层

#### 6.3.1 新增 IDataPermissionService 接口

文件：`backend/FinanceApp.Application/Interfaces/IDataPermissionService.cs`

```csharp
namespace FinanceApp.Application.Interfaces;

public interface IDataPermissionService
{
    /// <summary>
    /// 检查是否有权限访问指定实体
    /// </summary>
    bool CanAccess<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 检查是否有权限修改指定实体
    /// </summary>
    bool CanModify<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 检查是否有权限删除指定实体
    /// </summary>
    bool CanDelete<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 对查询应用权限过滤
    /// </summary>
    IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity;
}
```

#### 6.3.2 实现 DataPermissionService

文件：`backend/FinanceApp.Application/Services/DataPermissionService.cs`

```csharp
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Application.Services;

public class DataPermissionService : IDataPermissionService
{
    private readonly ICurrentUserService _currentUserService;

    public DataPermissionService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public bool CanAccess<T>(T entity) where T : BaseEntity
    {
        // Admin 和 Accountant 可以访问所有数据
        if (_currentUserService.IsAdmin || _currentUserService.IsAccountant)
            return true;

        // Viewer 只能访问自己创建的数据
        return entity.CreatedBy == _currentUserService.UserId;
    }

    public bool CanModify<T>(T entity) where T : BaseEntity
    {
        // Viewer 不能修改任何数据
        if (_currentUserService.IsViewer)
            return false;

        // Admin 可以修改所有数据
        if (_currentUserService.IsAdmin)
            return true;

        // Accountant 可以修改财务相关数据
        if (_currentUserService.IsAccountant)
        {
            return entity is Transaction or Receivable or Payable or 
                   Account or Project or Customer or Supplier or Person;
        }

        return false;
    }

    public bool CanDelete<T>(T entity) where T : BaseEntity
    {
        // 只有 Admin 可以删除数据
        return _currentUserService.IsAdmin;
    }

    public IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity
    {
        // Admin 和 Accountant 可以看到所有数据
        if (_currentUserService.IsAdmin || _currentUserService.IsAccountant)
            return query;

        // Viewer 只能看到自己创建的数据
        return query.Where(e => e.CreatedBy == _currentUserService.UserId);
    }
}
```

#### 6.3.3 修改 Service 示例（TransactionService）

文件：`backend/FinanceApp.Application/Services/TransactionService.cs`

```csharp
public class TransactionService : ITransactionService
{
    private readonly IRepository<Transaction> _repository;
    private readonly IDataPermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;

    public TransactionService(
        IRepository<Transaction> repository,
        IDataPermissionService permissionService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _permissionService = permissionService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<TransactionDto>> GetPagedAsync(TransactionQueryDto query)
    {
        var dbQuery = _repository.GetQueryable()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Project);

        // 应用权限过滤
        dbQuery = _permissionService.ApplyPermissionFilter(dbQuery);

        // 应用业务过滤
        if (query.AccountId.HasValue)
            dbQuery = dbQuery.Where(t => t.AccountId == query.AccountId.Value);

        // 分页查询...
    }

    public async Task<TransactionDto> GetByIdAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            throw new NotFoundException("交易记录不存在");

        // 检查访问权限
        if (!_permissionService.CanAccess(transaction))
            throw new ForbiddenException("无权访问此交易记录");

        return _mapper.Map<TransactionDto>(transaction);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionDto dto)
    {
        var transaction = _mapper.Map<Transaction>(dto);
        
        // CreatedBy 会在 DbContext.SaveChangesAsync 中自动设置
        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();

        return _mapper.Map<TransactionDto>(transaction);
    }

    public async Task UpdateAsync(int id, UpdateTransactionDto dto)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            throw new NotFoundException("交易记录不存在");

        // 检查修改权限
        if (!_permissionService.CanModify(transaction))
            throw new ForbiddenException("无权修改此交易记录");

        _mapper.Map(dto, transaction);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            throw new NotFoundException("交易记录不存在");

        // 检查删除权限
        if (!_permissionService.CanDelete(transaction))
            throw new ForbiddenException("无权删除此交易记录");

        await _repository.DeleteAsync(transaction);
        await _repository.SaveChangesAsync();
    }
}
```

### 6.4 API 层

#### 6.4.1 修改 Controller 示例（TransactionsController）

文件：`backend/FinanceApp.Api/Controllers/TransactionsController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // 所有端点都需要认证
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// 获取交易列表（分页）
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]  // 所有角色都可以查看
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetPaged([FromQuery] TransactionQueryDto query)
    {
        var result = await _transactionService.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// 获取交易详情
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<TransactionDto>> GetById(int id)
    {
        var result = await _transactionService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// 创建交易
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]  // 只有 Admin 和 Accountant 可以创建
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionDto dto)
    {
        var result = await _transactionService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// 更新交易
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        await _transactionService.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>
    /// 删除交易
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]  // 只有 Admin 可以删除
    public async Task<ActionResult> Delete(int id)
    {
        await _transactionService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 批量导入交易
    /// </summary>
    [HttpPost("batch")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<BatchImportResultDto>> BatchImport([FromBody] List<CreateTransactionDto> dtos)
    {
        var result = await _transactionService.BatchImportAsync(dtos);
        return Ok(result);
    }
}
```

#### 6.4.2 修改 JwtService

文件：`backend/FinanceApp.Application/Services/JwtService.cs`

```csharp
public string GenerateToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("name", user.Name),
        // 修改：使用枚举的字符串表示
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

#### 6.4.3 注册服务

文件：`backend/FinanceApp.Api/Program.cs`

```csharp
// 注册 IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 注册当前用户服务
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 注册数据权限服务
builder.Services.AddScoped<IDataPermissionService, DataPermissionService>();

// 配置 JWT 认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

// 配置授权
builder.Services.AddAuthorization();
```

---

## 7. 前端实现

### 7.1 类型定义

#### 7.1.1 UserRole 枚举

文件：`frontend/src/types/user.ts`

```typescript
export enum UserRole {
  Admin = 1,
  Accountant = 2,
  Viewer = 3
}

export interface User {
  id: number
  username: string
  email: string
  name: string
  role: UserRole
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}
```

### 7.2 Store 修改

#### 7.2.1 修改 userStore

文件：`frontend/src/stores/user.ts`

```typescript
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { login as apiLogin } from '@/api/auth'
import type { User, LoginRequest, UserRole } from '@/types/user'
import { UserRole as UserRoleEnum } from '@/types/user'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const user = ref<User | null>(null)

  // 计算属性：角色判断
  const isAdmin = computed(() => user.value?.role === UserRoleEnum.Admin)
  const isAccountant = computed(() => user.value?.role === UserRoleEnum.Accountant)
  const isViewer = computed(() => user.value?.role === UserRoleEnum.Viewer)

  // 计算属性：权限判断
  const canCreate = computed(() => isAdmin.value || isAccountant.value)
  const canEdit = computed(() => isAdmin.value || isAccountant.value)
  const canDelete = computed(() => isAdmin.value)
  const canImport = computed(() => isAdmin.value || isAccountant.value)
  const canManageUsers = computed(() => isAdmin.value)
  const canManageConfig = computed(() => isAdmin.value)

  // 登录
  const login = async (credentials: LoginRequest) => {
    const response = await apiLogin(credentials)
    token.value = response.token
    user.value = response.user
    localStorage.setItem('token', response.token)
    localStorage.setItem('user', JSON.stringify(response.user))
  }

  // 登出
  const logout = () => {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  // 初始化用户信息
  const initUser = () => {
    const storedUser = localStorage.getItem('user')
    if (storedUser) {
      user.value = JSON.parse(storedUser)
    }
  }

  // 检查是否有权限访问指定路由
  const hasRoutePermission = (routeName: string): boolean => {
    if (isAdmin.value) return true

    const accountantRoutes = [
      'transactions', 'accounts', 'projects', 'customers', 
      'suppliers', 'persons', 'receivables', 'payables',
      'dashboard', 'reports', 'import'
    ]

    const viewerRoutes = [
      'transactions', 'accounts', 'projects', 'customers',
      'suppliers', 'persons', 'dashboard'
    ]

    if (isAccountant.value) {
      return accountantRoutes.includes(routeName)
    }

    if (isViewer.value) {
      return viewerRoutes.includes(routeName)
    }

    return false
  }

  return {
    token,
    user,
    isAdmin,
    isAccountant,
    isViewer,
    canCreate,
    canEdit,
    canDelete,
    canImport,
    canManageUsers,
    canManageConfig,
    login,
    logout,
    initUser,
    hasRoutePermission
  }
})
```

### 7.3 Router 修改

#### 7.3.1 添加路由守卫

文件：`frontend/src/router/index.ts`

```typescript
import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: '/dashboard'
      },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/dashboard/Dashboard.vue'),
        meta: { title: '仪表盘', roles: ['Admin', 'Accountant', 'Viewer'] }
      },
      {
        path: 'transactions',
        name: 'transactions',
        component: () => import('@/views/transactions/TransactionList.vue'),
        meta: { title: '交易管理', roles: ['Admin', 'Accountant', 'Viewer'] }
      },
      {
        path: 'accounts',
        name: 'accounts',
        component: () => import('@/views/accounts/AccountList.vue'),
        meta: { title: '账户管理', roles: ['Admin', 'Accountant', 'Viewer'] }
      },
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/users/UserList.vue'),
        meta: { title: '用户管理', roles: ['Admin'] }
      }
      // ... 其他路由
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 全局前置守卫
router.beforeEach((to, from, next) => {
  const userStore = useUserStore()

  // 检查是否需要认证
  if (to.meta.requiresAuth !== false && !userStore.token) {
    next({ name: 'login', query: { redirect: to.fullPath } })
    return
  }

  // 检查角色权限
  if (to.meta.roles && Array.isArray(to.meta.roles)) {
    const userRole = userStore.user?.role
    const roleNames = {
      1: 'Admin',
      2: 'Accountant',
      3: 'Viewer'
    }
    const userRoleName = roleNames[userRole as keyof typeof roleNames]

    if (!to.meta.roles.includes(userRoleName)) {
      ElMessage.error('无权访问此页面')
      next({ name: 'dashboard' })
      return
    }
  }

  next()
})

export default router
```

### 7.4 权限指令

#### 7.4.1 创建 v-permission 指令

文件：`frontend/src/directives/permission.ts`

```typescript
import type { Directive, DirectiveBinding } from 'vue'
import { useUserStore } from '@/stores/user'

export const permission: Directive = {
  mounted(el: HTMLElement, binding: DirectiveBinding) {
    const { value } = binding
    const userStore = useUserStore()

    if (value && Array.isArray(value) && value.length > 0) {
      const requiredRoles = value
      const userRole = userStore.user?.role

      const roleNames = {
        1: 'Admin',
        2: 'Accountant',
        3: 'Viewer'
      }
      const userRoleName = roleNames[userRole as keyof typeof roleNames]

      if (!requiredRoles.includes(userRoleName)) {
        el.style.display = 'none'
        // 或者直接移除元素
        // el.parentNode?.removeChild(el)
      }
    }
  }
}

// 注册指令
// 在 main.ts 中：
// import { permission } from './directives/permission'
// app.directive('permission', permission)
```

### 7.5 组件修改示例

#### 7.5.1 TransactionList.vue

文件：`frontend/src/views/transactions/TransactionList.vue`

```vue
<template>
  <div class="transaction-list">
    <el-card>
      <!-- 操作栏 -->
      <div class="toolbar">
        <el-button 
          v-permission="['Admin', 'Accountant']"
          type="primary" 
          @click="handleCreate">
          新增交易
        </el-button>
      </div>

      <!-- 表格 -->
      <el-table :data="transactions">
        <el-table-column prop="date" label="日期" />
        <el-table-column prop="amount" label="金额" />
        <el-table-column label="操作">
          <template #default="{ row }">
            <el-button 
              v-permission="['Admin', 'Accountant']"
              size="small" 
              @click="handleEdit(row)">
              编辑
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
```

---

## 8. 测试方案

### 8.1 单元测试

#### 8.1.1 DataPermissionService 测试

```csharp
public class DataPermissionServiceTests
{
    [Fact]
    public void CanAccess_AdminUser_ReturnsTrue()
    {
        // Arrange
        var service = new DataPermissionService(mockCurrentUserService);
        var transaction = new Transaction { Id = 1, CreatedBy = 2 };

        // Act
        var result = service.CanAccess(transaction);

        // Assert
        Assert.True(result);
    }
}
```

### 8.2 集成测试

#### 8.2.1 TransactionController 权限测试

```csharp
public class TransactionsControllerTests
{
    [Fact]
    public async Task GetTransactions_AsViewer_ReturnsOnlyOwnTransactions()
    {
        // 测试 Viewer 只能看到自己的数据
    }

    [Fact]
    public async Task CreateTransaction_AsViewer_ReturnsForbidden()
    {
        // 测试 Viewer 不能创建数据
    }
}
```

### 8.3 E2E 测试

前端权限测试，验证不同角色的用户界面和功能访问。

---

## 9. 风险评估

### 9.1 技术风险

| 风险项 | 风险等级 | 影响 | 缓解措施 |
|--------|---------|------|---------|
| EF Core Query Filter 性能问题 | 中 | 查询性能下降 | 使用索引优化，监控慢查询 |
| JWT Token 过期处理 | 低 | 用户体验 | 实现 Token 刷新机制 |
| 前后端权限不一致 | 高 | 安全漏洞 | 后端强制检查，前端仅为 UX |
| 数据库迁移失败 | 中 | 部署失败 | 充分测试迁移脚本，准备回滚方案 |

### 9.2 业务风险

| 风险项 | 风险等级 | 影响 | 缓解措施 |
|--------|---------|------|---------|
| 用户角色分配错误 | 高 | 数据泄露 | 实现审计日志，定期审查权限 |
| Viewer 看不到必要数据 | 中 | 业务受阻 | 提供数据共享机制 |
| 权限过于严格 | 中 | 用户体验差 | 收集反馈，灵活调整 |

### 9.3 实施风险

| 风险项 | 风险等级 | 影响 | 缓解措施 |
|--------|---------|------|---------|
| 开发工作量超预期 | 中 | 延期交付 | 分阶段实施，优先核心功能 |
| 测试覆盖不足 | 高 | 线上故障 | 编写完整的单元测试和集成测试 |
| 文档不完善 | 低 | 维护困难 | 同步更新文档 |

---

## 10. 实施计划

### 10.1 分阶段实施

#### 阶段 1：基础设施（1-2 天）

**目标**：搭建权限控制基础框架

**任务清单**：
- 创建 UserRole 枚举
- 修改 User 实体
- 修改 BaseEntity 添加 CreatedBy
- 创建数据库迁移脚本
- 实现 ICurrentUserService 和 CurrentUserService
- 实现 IDataPermissionService 和 DataPermissionService
- 修改 JwtService 生成包含角色的 Token
- 注册服务到 DI 容器

**验收标准**：
- 所有服务正常注册
- JWT Token 包含正确的角色信息
- 单元测试通过

#### 阶段 2：Domain 和 Infrastructure 层（2-3 天）

**目标**：修改所有实体和配置

**任务清单**：
- 修改 15 个实体添加 CreatedBy 字段
- 修改 17 个 EntityConfiguration
- 修改 ApplicationDbContext 添加 Query Filter
- 修改 ApplicationDbContext.SaveChangesAsync 自动设置 CreatedBy
- 执行数据库迁移
- 创建测试数据（不同角色的用户）

**验收标准**：
- 数据库迁移成功
- Query Filter 正常工作
- CreatedBy 自动设置

#### 阶段 3：Application 层（2-3 天）

**目标**：修改所有 Service 添加权限过滤

**任务清单**：
- 修改 TransactionService（6 个方法）
- 修改 AccountService（3 个方法）
- 修改 ProjectService（3 个方法）
- 修改其他 Service（共 45 个方法）

**验收标准**：
- 所有 Service 方法都有权限检查
- 单元测试通过
- 集成测试通过

#### 阶段 4：API 层（1-2 天）

**目标**：修改所有 Controller 添加角色授权

**任务清单**：
- 修改 TransactionsController（11 个端点）
- 修改 AccountController（8 个端点）
- 修改其他 Controller（共 94 个端点）

**验收标准**：
- 所有端点都有正确的角色授权
- API 测试通过
- Swagger 文档更新

#### 阶段 5：前端实现（2-3 天）

**目标**：实现前端权限控制

**任务清单**：
- 修改 user.ts 类型定义
- 修改 userStore 添加权限判断
- 修改 router 添加路由守卫
- 创建 v-permission 指令
- 修改所有页面组件
- 修改导航菜单（根据权限显示/隐藏）

**验收标准**：
- 不同角色看到不同的菜单和按钮
- 路由守卫正常工作
- E2E 测试通过

#### 阶段 6：测试和优化（2-3 天）

**目标**：全面测试和性能优化

**任务清单**：
- 编写单元测试（目标覆盖率 80%）
- 编写集成测试
- 编写 E2E 测试
- 性能测试（查询性能）
- 安全测试（权限绕过测试）
- 修复发现的 Bug
- 优化慢查询
- 更新文档

**验收标准**：
- 测试覆盖率达标
- 性能满足要求
- 无严重 Bug

### 10.2 时间表

| 阶段 | 工作日 | 起止日期 | 负责人 |
|------|--------|---------|--------|
| 阶段 1：基础设施 | 2 天 | Day 1-2 | 后端开发 |
| 阶段 2：Domain/Infrastructure | 3 天 | Day 3-5 | 后端开发 |
| 阶段 3：Application 层 | 3 天 | Day 6-8 | 后端开发 |
| 阶段 4：API 层 | 2 天 | Day 9-10 | 后端开发 |
| 阶段 5：前端实现 | 3 天 | Day 11-13 | 前端开发 |
| 阶段 6：测试和优化 | 3 天 | Day 14-16 | 全员 |
| **总计** | **16 天** | | |

### 10.3 里程碑

| 里程碑 | 日期 | 交付物 |
|--------|------|--------|
| M1：基础框架完成 | Day 2 | 权限服务、枚举、接口 |
| M2：后端实体完成 | Day 5 | 所有实体添加 CreatedBy，数据库迁移 |
| M3：后端逻辑完成 | Day 10 | 所有 Service 和 Controller 添加权限控制 |
| M4：前端完成 | Day 13 | 前端权限控制、路由守卫、指令 |
| M5：测试完成 | Day 16 | 测试报告、性能报告 |
| M6：上线 | Day 17 | 生产环境部署 |

### 10.4 资源需求

| 资源 | 数量 | 说明 |
|------|------|------|
| 后端开发 | 1 人 | 熟悉 .NET 和 EF Core |
| 前端开发 | 1 人 | 熟悉 Vue3 和 TypeScript |
| 测试工程师 | 0.5 人 | 兼职，负责测试用例编写 |
| DBA | 0.5 人 | 兼职，负责数据库迁移审查 |

### 10.5 回滚方案

如果实施过程中出现严重问题，可以按以下步骤回滚：

1. **代码回滚**：使用 Git 回滚到实施前的 commit
2. **数据库回滚**：执行回滚脚本，删除 CreatedBy 字段
3. **配置回滚**：恢复原有的 JWT 配置
4. **前端回滚**：部署旧版本前端代码

---

## 11. 附录

### 11.1 文件修改清单

#### Domain 层（18 个文件）

- Domain/Enums/UserRole.cs（新建）
- Domain/Entities/BaseEntity.cs
- Domain/Entities/User.cs
- Domain/Entities/Account.cs
- Domain/Entities/Category.cs
- Domain/Entities/ClassificationRule.cs
- Domain/Entities/Customer.cs
- Domain/Entities/Supplier.cs
- Domain/Entities/Person.cs
- Domain/Entities/Project.cs
- Domain/Entities/Transaction.cs
- Domain/Entities/TransactionAllocation.cs
- Domain/Entities/BankStatement.cs
- Domain/Entities/Receivable.cs
- Domain/Entities/ReceivablePayment.cs
- Domain/Entities/Payable.cs
- Domain/Entities/PayablePayment.cs
- Domain/Entities/ExcelImportLog.cs
- Domain/Interfaces/ICurrentUserService.cs（新建）

#### Infrastructure 层（18 个文件）

- Infrastructure/Data/ApplicationDbContext.cs
- Infrastructure/Data/Configurations/UserConfiguration.cs
- Infrastructure/Data/Configurations/AccountConfiguration.cs
- 其他 14 个 Configuration 文件

#### Application 层（15 个文件）

- Application/Interfaces/IDataPermissionService.cs（新建）
- Application/Services/DataPermissionService.cs（新建）
- Application/Services/JwtService.cs
- 其他 12 个 Service 文件

#### API 层（15 个文件）

- Api/Services/CurrentUserService.cs（新建）
- Api/Program.cs
- 13 个 Controller 文件

#### 前端（20 个文件）

- frontend/src/types/user.ts
- frontend/src/stores/user.ts
- frontend/src/router/index.ts
- frontend/src/directives/permission.ts（新建）
- 16 个 Vue 组件文件

#### 数据库（1 个文件）

- docs/02_Database/03_add_permission_control.sql（新建）

#### 测试（10 个文件）

- 后端测试 5 个文件
- 前端测试 5 个文件

**总计：97 个文件**

---

## 12. 参考资料

### 12.1 技术文档

- ASP.NET Core Authorization
- EF Core Global Query Filters
- JWT Authentication in ASP.NET Core
- Vue Router Navigation Guards
- Pinia State Management

### 12.2 最佳实践

- OWASP Authorization Cheat Sheet
- Microsoft Security Best Practices
- RBAC Design Patterns

---

**文档结束**

> 本文档由 Claude Code 生成，版本 v1.0，创建于 2026-03-14
