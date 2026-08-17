# 架构改进方案

> 基于代码深度审查，按优先级排列的 7 项改进方案。

---

## 一、恢复分层边界（P0 - 高优先级）

### 1.1 问题现状

- `Application.csproj` 第 5 行直接引用 `Infrastructure` 项目
- 3 个 Service 直接注入 `AppDbContext`：
  - `TransactionService`（9 处 context 调用 + 事务方法）
  - `ImportService`（9 处 DbSet 直接操作 + 事务）
  - `DashboardService`（6 处 DbSet 聚合查询）
- 缺少 `IUnitOfWork` 接口，事务管理散落在各 Service 中
- `Repository.AddAsync` 内部已调 `SaveChangesAsync`，Service 层又重复调用（双重保存）

### 1.2 方案设计

#### 步骤 A：新增 IUnitOfWork + ITransactionScope 接口（Domain 层）

```
新建文件：backend/FinanceApp.Domain/Interfaces/IUnitOfWork.cs
```

```csharp
namespace FinanceApp.Domain.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>统一保存当前 DbContext 所有变更</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>开始数据库事务。InMemory 数据库返回 null</summary>
    Task<ITransactionScope?> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface ITransactionScope : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

#### 步骤 B：实现 UnitOfWork（Infrastructure 层）

```
新建文件：backend/FinanceApp.Infrastructure/Data/UnitOfWork.cs
```

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task<ITransactionScope?> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (!_context.Database.IsRelational()) return null;
        var tx = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransactionScope(tx);
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal class EfTransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;
    public EfTransactionScope(IDbContextTransaction tx) => _transaction = tx;
    public Task CommitAsync(CancellationToken ct = default) => _transaction.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct = default) => _transaction.RollbackAsync(ct);
    public void Dispose() => _transaction.Dispose();
    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
```

#### 步骤 C：重构 Repository -- 取消自动 SaveChanges

修改 `IRepository<T>` 接口：
- `AddAsync` 不再内部调 SaveChanges，仅追踪实体
- `UpdateAsync` 改为同步 `Update`
- `DeleteAsync` 改为同步 `Delete`（软删除）
- **删除** `SaveChangesAsync()`、`AddWithoutSaveAsync`、`UpdateWithoutSaveAsync`

```csharp
// 修改后的核心方法签名
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(long id);
    Task<List<T>> GetAllAsync();
    Task<(List<T> Items, int Total)> GetPagedAsync(int page, int pageSize);
    IQueryable<T> GetQueryable();
    Task<bool> ExistsAsync(long id);

    Task<T> AddAsync(T entity);    // 仅追踪，不保存
    void Update(T entity);         // 仅标记修改
    void Delete(T entity);         // 软删除标记
}
```

#### 步骤 D：重构 3 个 Service，移除 AppDbContext 依赖

**TransactionService**（改动 ~12 行）：
```csharp
// 前：private readonly AppDbContext _context;
// 后：private readonly IUnitOfWork _unitOfWork;

// 9 处 _context.SaveChangesAsync() → _unitOfWork.SaveChangesAsync()
// BeginTransactionSafeAsync() → _unitOfWork.BeginTransactionAsync()
```

**ImportService**（改动 ~20 行）：
```csharp
// 前：_dbContext.BankTransactions.Add(bankTransaction);
// 后：await _unitOfWork.Repository<BankTransaction>().AddAsync(bankTransaction);
// 或：注入 IRepository<BankTransaction>，用 AddAsync

// 事务：_dbContext.Database.BeginTransactionAsync() → _unitOfWork.BeginTransactionAsync()
```

**DashboardService**（改动 ~10 行）：
```csharp
// 前：_context.Transactions.AsQueryable()
// 后：_transactionRepository.GetQueryable()
// 注入 IRepository<Transaction>、IRepository<Account>、IRepository<Project>
```

#### 步骤 E：修改项目引用

```xml
<!-- Application.csproj：删除 Infrastructure 引用，添加 EF Core NuGet -->
<ItemGroup>
  <ProjectReference Include="..\FinanceApp.Domain\FinanceApp.Domain.csproj" />
  <!-- 删除: <ProjectReference Include="..\FinanceApp.Infrastructure\..." /> -->
</ItemGroup>
<ItemGroup>
  <!-- 保留 EF Core 包，用于 IQueryable 扩展方法 (ToListAsync/Include 等) -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
</ItemGroup>
```

#### 步骤 F：集中 DI 注册

```csharp
// Infrastructure 层新增扩展方法
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options => ...);
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();
        return services;
    }
}

// Application 层新增扩展方法
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAccountService, AccountService>();
        // ... 其他 Service 注册
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        return services;
    }
}

// Program.cs 简化为
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
```

#### 步骤 G：修复全部 Service 的双重保存

涉及 ~10 个 Service（AccountService、CustomerService、SupplierService、PersonService、ProjectService、CategoryService、RuleService、ReceivableService、PayableService、CrudServiceBase），每个 Service 的模式统一为：

```csharp
await _repository.AddAsync(entity);     // 仅追踪
await _unitOfWork.SaveChangesAsync();   // 统一保存（仅一次）
```

### 1.3 迁移步骤

| 步骤 | 操作 | 影响范围 |
|------|------|---------|
| 1 | 创建 IUnitOfWork + ITransactionScope（Domain） | 无破坏 |
| 2 | 创建 UnitOfWork + EfTransactionScope（Infrastructure） | 无破坏 |
| 3 | 注册 IUnitOfWork DI（Program.cs） | 无破坏 |
| 4 | 重构 Repository 接口和实现 + 所有 Service | **破坏性**，需一次性完成 |
| 5 | 修改 Application.csproj 删除 Infrastructure 引用 | 编译验证 |
| 6 | 创建 AddInfrastructure/AddApplicationServices 扩展方法 | 重构 Program.cs |
| 7 | 更新全部 455 个测试 | Mock IUnitOfWork 替代 AppDbContext |

### 1.4 风险

| 风险 | 等级 | 缓解 |
|------|------|------|
| 步骤 4 是全局破坏性变更 | 高 | 在独立分支操作，一次性提交 |
| Application 仍依赖 EF Core NuGet | 中 | 务实折衷，否则改动量 10 倍 |
| 测试需同步更新 | 中 | 提取 MockUnitOfWork 辅助类 |

---

## 二、拆分交易与财务服务（P0 - 高优先级）

### 2.1 问题现状

- `TransactionService` 1,271 行，17 个公共方法，9 个构造函数依赖
- 承载 5 个职责：CRUD、分摊、余额、转账、统计
- Include 链重复 73 处，事务 try/commit/rollback 模式重复 4 处

### 2.2 拆分为 6 个服务

```
TransactionService (核心 CRUD, ~300 行)
  ├── IAllocationService (分摊验证+创建, ~120 行)
  ├── IAccountBalanceService (余额查询+更新, ~100 行)
  ├── ITransactionQueryService (分页+多维查询, ~250 行)
  ├── ITransferService (转账, ~200 行)
  └── ITransactionStatisticsService (统计+关联查询, ~150 行)
```

#### 新增接口定义

```csharp
// IAllocationService.cs
public interface IAllocationService
{
    void ValidateAllocations(List<CreateAllocationRequest> allocations, decimal totalAmount);
    decimal CalculateAmountFromRate(decimal totalAmount, decimal rate);
    Task CreateAllocationsAsync(long transactionId, List<CreateAllocationRequest> allocations, decimal totalAmount);
    Task ReplaceAllocationsAsync(Transaction transaction, List<CreateAllocationRequest>? allocations);
}

// IAccountBalanceService.cs
public interface IAccountBalanceService
{
    Task<decimal> GetAccountBalanceAsync(long accountId);
    void AdjustBalanceWithoutSave(Account account, decimal amount, TransactionType type);
}

// ITransactionQueryService.cs
public interface ITransactionQueryService
{
    Task<PageResponse<TransactionDto>> GetPagedAsync(PageRequest request);
    Task<TransactionDto> GetByIdAsync(long id);
    Task<List<TransactionDto>> GetByAccountAsync(long accountId);
    Task<List<TransactionDto>> GetByProjectAsync(long projectId);
    Task<List<TransactionDto>> GetByCategoryAsync(long categoryId);
    Task<List<TransactionDto>> GetByCustomerAsync(long customerId);
    Task<List<TransactionDto>> GetBySupplierAsync(long supplierId);
    Task<List<TransactionDto>> GetByPersonAsync(long personId);
}

// ITransferService.cs
public interface ITransferService
{
    Task<TransferResultDto> CreateTransferAsync(CreateTransferRequest request);
}

// ITransactionStatisticsService.cs
public interface ITransactionStatisticsService
{
    Task<TransactionStatisticsDto> GetStatisticsAsync();
    Task<RelatedFinanceRecordDto> GetRelatedFinanceRecordsAsync(long transactionId);
}

// ITransactionService.cs（瘦身后仅保留 CRUD）
public interface ITransactionService
{
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request);
    Task<TransactionDto> UpdateAsync(long id, UpdateTransactionRequest request);
    Task DeleteAsync(long id);
}
```

#### 提取 Include 链扩展方法

```csharp
// Application/Extensions/TransactionQueryExtensions.cs
public static class TransactionQueryExtensions
{
    public static IQueryable<Transaction> IncludeFullDetails(this IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.Person)
            .Include(t => t.RelatedTransaction).ThenInclude(rt => rt!.Account)
            .Include(t => t.Allocations).ThenInclude(a => a.Project)
            .Include(t => t.Allocations).ThenInclude(a => a.Person);
    }
}
```

消除 73 处重复 Include，每个查询方法仅需一行 `.IncludeFullDetails()`。

#### Controller 改造

```csharp
// TransactionsController 注入 5 个服务
public TransactionsController(
    ITransactionService transactionService,
    ITransactionQueryService queryService,
    ITransferService transferService,
    ITransactionStatisticsService statisticsService,
    IAccountBalanceService balanceService,
    ILogger<TransactionsController> logger) { ... }

// 路由映射
// GET    /api/transactions           → _queryService.GetPagedAsync
// GET    /api/transactions/{id}      → _queryService.GetByIdAsync
// POST   /api/transactions           → _transactionService.CreateAsync
// PUT    /api/transactions/{id}      → _transactionService.UpdateAsync
// DELETE /api/transactions/{id}      → _transactionService.DeleteAsync
// POST   /api/transactions/transfer  → _transferService.CreateTransferAsync
// GET    /api/transactions/statistics→ _statisticsService.GetStatisticsAsync
```

### 2.3 分 4 阶段迁移

| 阶段 | 内容 | 验证点 |
|------|------|--------|
| 0（准备） | 创建 TransactionQueryExtensions + 在 ServiceBase 添加 ExecuteInTransactionAsync | 零行为变更，全量测试通过 |
| 1（底层） | 提取 AllocationService + AccountBalanceService | 编译通过 + 测试 |
| 2（查询） | 提取 TransactionQueryService | ITransactionService 移除查询方法 |
| 3（转账+统计） | 提取 TransferService + TransactionStatisticsService | 最终验证 |

### 2.4 收益预估

| 指标 | 重构前 | 重构后 |
|------|--------|--------|
| 最大 Service 行数 | 1,271 | ~300 |
| 构造函数依赖数 | 11 | 6 |
| Include 重复 | 73 处 | 0 |
| 事务模板重复 | 4 处 | 0 |
| 可独立测试的服务 | 1 | 6 |

---

## 三、统一控制器管线（P1 - 中优先级）

### 3.1 问题现状

- `CrudControllerBase` 已定义但 **0 个控制器继承**
- 16 个控制器全部继承 `BaseApiController`
- 9 个 CRUD 控制器重复 45 个标准方法，约 1,300-1,800 行重复代码
- 根本原因：各 Service 接口未继承 `ICrudService`

### 3.2 方案设计

#### 步骤 1：让 9 个 Service 接口继承 ICrudService

```csharp
// 例：IAccountService
public interface IAccountService
    : ICrudService<AccountDto, CreateAccountRequest, UpdateAccountRequest>
{
    // 移除 5 个重复的 CRUD 方法签名
    // 仅保留特殊方法
    Task<List<AccountDto>> GetActiveAccountsAsync();
    Task<List<AccountDto>> GetMaturingAccountsAsync(int days = 30);
    Task<BalanceTrendResponse> GetBalanceTrendAsync(long id, int months = 6);
}

// 带批量操作的接口额外继承 IBatchService
public interface ICustomerService
    : ICrudService<CustomerDto, CreateCustomerRequest, UpdateCustomerRequest>,
      IBatchService<CustomerDto, CreateCustomerRequest>
{
    Task<List<CustomerDto>> GetActiveCustomersAsync();
}
```

#### 步骤 2：增强 CrudControllerBase

```csharp
[Authorize]
public abstract class CrudControllerBase<TDto, TCreateRequest, TUpdateRequest, TService>
    : BaseApiController
    where TService : ICrudService<TDto, TCreateRequest, TUpdateRequest>
{
    protected readonly TService Service;
    protected readonly ILogger Logger;
    protected abstract string EntityDisplayName { get; }

    // 5 个标准 CRUD 方法，带默认授权：
    // GetPaged:  [Authorize(Roles = "Admin,Accountant,Viewer")]
    // GetById:   [Authorize(Roles = "Admin,Accountant,Viewer")]
    // Create:    [Authorize(Roles = "Admin,Accountant")]
    // Update:    [Authorize(Roles = "Admin,Accountant")]
    // Delete:    [Authorize(Roles = "Admin")]
    // 每个方法含 try-catch + 日志模板
}
```

#### 步骤 3：新增 BatchCrudControllerBase

```csharp
public abstract class BatchCrudControllerBase<TDto, TCreateRequest, TUpdateRequest, TService>
    : CrudControllerBase<TDto, TCreateRequest, TUpdateRequest, TService>
    where TService : ICrudService<...>, IBatchService<...>
{
    // BatchCreate: 验证数量 + 调用 Service.BatchCreateAsync
    // BatchImport: 验证文件格式 + 调用子类 ParseExcelRows + BatchCreateAsync
    protected abstract List<TCreateRequest> ParseExcelRows(ExcelWorksheet worksheet, int rowCount);
}
```

#### 步骤 4：控制器分类迁移

| 类型 | 控制器 | 基类 |
|------|--------|------|
| 标准 CRUD | Account, Category*, Rule*, Receivable, Payable | CrudControllerBase |
| CRUD + 批量 | Customer, Supplier, Person, Project | BatchCrudControllerBase |
| 不迁移 | Transaction, Auth, Dashboard, Report, Import, Config, AuditLog | BaseApiController |

*Category 和 Rule 需 override Create/Update 方法以收紧权限为 Admin Only。

#### 迁移后示例

```csharp
// AccountController：从 172 行 → ~55 行
[ApiController]
[Route("api/[controller]")]
public class AccountController
    : CrudControllerBase<AccountDto, CreateAccountRequest, UpdateAccountRequest, IAccountService>
{
    protected override string EntityDisplayName => "账户";

    public AccountController(IAccountService service, ILogger<AccountController> logger)
        : base(service, logger) { }

    // 仅保留 3 个特殊端点：GetActive, GetMaturing, GetBalanceTrend
}

// CustomerController：从 268 行 → ~45 行
[ApiController]
[Route("api/[controller]")]
public class CustomerController
    : BatchCrudControllerBase<CustomerDto, CreateCustomerRequest, UpdateCustomerRequest, ICustomerService>
{
    protected override string EntityDisplayName => "客户";
    public CustomerController(ICustomerService service, ILogger<CustomerController> logger)
        : base(service, logger) { }
    protected override List<CreateCustomerRequest> ParseExcelRows(...) { ... }

    // 仅保留 GetActive 端点
}
```

### 3.3 收益

| 指标 | 迁移前 | 迁移后 |
|------|--------|--------|
| 9 个控制器总行数 | ~1,959 | ~470 |
| 新增基类 | 0 | ~200 |
| **净减少** | | **~1,290 行** |

---

## 四、重构前端导航与权限（P1 - 中优先级）

### 4.1 问题现状

- 菜单在 MainLayout 第 6-120 行硬编码
- 28 个路由都硬编码 `roles` 数组（重复率 100%）
- 路由守卫（203-266 行）手动校验
- `v-permission` 指令定义了但未在菜单中使用
- 菜单权限用 `userStore.isAdmin`，路由权限用 `meta.roles`，两套系统独立维护

### 4.2 方案设计

#### 步骤 1：创建权限常量

```typescript
// frontend/src/constants/permissions.ts
export enum UserRole {
  Admin = 'Admin',
  Accountant = 'Accountant',
  Viewer = 'Viewer'
}

export const PermissionGroups = {
  ALL: [UserRole.Admin, UserRole.Accountant, UserRole.Viewer],
  EDIT: [UserRole.Admin, UserRole.Accountant],
  ADMIN_ONLY: [UserRole.Admin]
} as const
```

#### 步骤 2：增强路由 meta，增加菜单所需字段

```typescript
// router/index.ts -- 扩展 meta 类型
declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    requiresAuth?: boolean
    roles?: string[]
    icon?: string           // 菜单图标
    hidden?: boolean        // 是否在菜单中隐藏（详情页等）
    group?: string          // 菜单分组名
    order?: number          // 菜单排序
    activeMenu?: string     // 高亮的菜单路径（详情页指向列表页）
  }
}

// 路由定义示例
{
  path: 'transactions',
  name: 'Transactions',
  component: () => import('@/views/transactions/TransactionList.vue'),
  meta: {
    title: '交易记录',
    roles: PermissionGroups.ALL,
    icon: 'Tickets',
    group: '财务管理',
    order: 2
  }
},
{
  path: 'transactions/:id',
  name: 'TransactionDetail',
  component: () => import('@/views/transactions/TransactionDetail.vue'),
  meta: {
    title: '交易详情',
    roles: PermissionGroups.ALL,
    hidden: true,
    activeMenu: '/transactions'
  }
}
```

#### 步骤 3：封装 useAuth composable

```typescript
// frontend/src/composables/useAuth.ts
export function useAuth() {
  const userStore = useUserStore()
  const router = useRouter()

  const hasPermission = (roles?: string[]) => {
    if (!roles || roles.length === 0) return true
    return !!userStore.user?.role && roles.includes(userStore.user.role)
  }

  const isAdmin = computed(() => userStore.user?.role === 'Admin')
  const canEdit = computed(() => hasPermission(PermissionGroups.EDIT))
  const canDelete = computed(() => hasPermission(PermissionGroups.ADMIN_ONLY))

  // 从路由配置自动生成菜单
  const menuItems = computed(() => {
    const routes = router.getRoutes()
    return routes
      .filter(r => r.meta?.group && !r.meta?.hidden && hasPermission(r.meta?.roles as string[]))
      .sort((a, b) => (a.meta?.order ?? 99) - (b.meta?.order ?? 99))
      .reduce((groups, route) => {
        const group = route.meta!.group as string
        if (!groups[group]) groups[group] = []
        groups[group].push({
          path: route.path,
          title: route.meta!.title as string,
          icon: route.meta!.icon as string
        })
        return groups
      }, {} as Record<string, MenuItem[]>)
  })

  return { hasPermission, isAdmin, canEdit, canDelete, menuItems }
}
```

#### 步骤 4：MainLayout 从硬编码改为动态渲染

```vue
<!-- MainLayout.vue：菜单区域从 ~100 行硬编码 → ~20 行动态渲染 -->
<template>
  <el-menu :default-active="activeMenu" router>
    <template v-for="(items, group) in menuItems" :key="group">
      <el-menu-item-group :title="group">
        <el-menu-item v-for="item in items" :key="item.path" :index="item.path">
          <el-icon><component :is="item.icon" /></el-icon>
          <span>{{ item.title }}</span>
        </el-menu-item>
      </el-menu-item-group>
    </template>
  </el-menu>
</template>

<script setup>
const { menuItems } = useAuth()
const route = useRoute()
const activeMenu = computed(() => route.meta.activeMenu || route.path)
</script>
```

#### 步骤 5：路由守卫和 v-permission 统一复用 useAuth

```typescript
// 路由守卫
router.beforeEach((to, from, next) => {
  const { hasPermission } = useAuth()
  if (to.meta.requiresAuth && !hasPermission(to.meta.roles)) {
    ElMessage.error('无权访问')
    next('/')
    return
  }
  next()
})

// v-permission 指令
app.directive('permission', {
  mounted(el, binding) {
    const { hasPermission } = useAuth()
    if (!hasPermission(binding.value)) {
      el.style.display = 'none'
    }
  }
})
```

### 4.3 迁移步骤

1. 创建 `constants/permissions.ts` 和 `composables/useAuth.ts`
2. 为所有路由补充 `icon`、`group`、`order`、`hidden`、`activeMenu` meta
3. 改造 MainLayout 为动态菜单渲染
4. 路由守卫和 v-permission 改用 `useAuth().hasPermission`
5. 逐步移除 `userStore.isAdmin`/`userStore.canEdit` 的直接使用

---

## 五、整合 API 客户端与类型（P1 - 中优先级）

### 5.1 问题现状

- `api/base/crudFactory.ts`（100 行）和 `api/generated/api.ts`（39K+ token）完全未被使用
- 15 个手写 API 文件（935 行）重复 CRUD 逻辑
- `ApiResponse` 在 `utils/request.ts` 和 `types/common.ts` 中定义不一致

### 5.2 方案设计

**决策：采用 CRUD Factory + 手写特殊 API 的混合方案**

#### 步骤 1：清理死代码

- 删除 `api/generated/api.ts`（零引用）
- 删除 `types/common.ts` 中重复的 `ApiResponse` 定义

#### 步骤 2：完善 crudFactory

```typescript
// api/base/crudFactory.ts
export interface CrudFactoryConfig {
  baseUrl: string
  features?: {
    getActive?: boolean      // GET {baseUrl}/active
    batchCreate?: boolean    // POST {baseUrl}/batch
    batchImport?: boolean    // POST {baseUrl}/batch-import
  }
}

export function createCrudApi<T, C, U>(config: CrudFactoryConfig) {
  const { baseUrl } = config
  const api = {
    getList: (params?: PageRequest) => request<ApiResponse<PageResponse<T>>>({ url: baseUrl, method: 'get', params }),
    getById: (id: number) => request<ApiResponse<T>>({ url: `${baseUrl}/${id}`, method: 'get' }),
    create: (data: C) => request<ApiResponse<T>>({ url: baseUrl, method: 'post', data }),
    update: (id: number, data: U) => request<ApiResponse<T>>({ url: `${baseUrl}/${id}`, method: 'put', data }),
    remove: (id: number) => request<ApiResponse<void>>({ url: `${baseUrl}/${id}`, method: 'delete' }),
  }
  if (config.features?.getActive) {
    (api as any).getActive = () => request<ApiResponse<T[]>>({ url: `${baseUrl}/active`, method: 'get' })
  }
  if (config.features?.batchCreate) {
    (api as any).batchCreate = (data: { items: C[] }) =>
      request<ApiResponse<any>>({ url: `${baseUrl}/batch`, method: 'post', data })
  }
  return api
}
```

#### 步骤 3：迁移 API 文件（保留向后兼容具名导出）

```typescript
// api/customer.ts -- 从 57 行 → ~15 行
import { createCrudApi } from '@/api/base/crudFactory'
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '@/types/customer'

const api = createCrudApi<Customer, CreateCustomerRequest, UpdateCustomerRequest>({
  baseUrl: '/customer',
  features: { getActive: true, batchCreate: true }
})

// 向后兼容具名导出（31 个 vue 文件无需改动）
export const getCustomers = api.getList
export const getCustomerById = api.getById
export const createCustomer = api.create
export const updateCustomer = api.update
export const deleteCustomer = api.remove
export const getActiveCustomers = (api as any).getActive
export const batchCreateCustomers = (api as any).batchCreate
```

#### 迁移范围

| 分类 | 文件 | 操作 |
|------|------|------|
| 工厂化 | customer, supplier, person, category, rule, account, project, receivable, payable | 迁移到 crudFactory |
| 保持手写 | auth, dashboard, report, import, auditLog, config | 仅统一类型引用 |

---

## 六、建立 Feature Store/Composable（P1 - 中优先级）

### 6.1 问题现状

- TransactionList.vue（768 行）、FinanceManagement.vue（829 行）等大组件
- `useListPage` composable 已定义但 **未被任何地方使用**
- 所有列表页面手动实现分页逻辑（重复 10+ 次）
- 仅有 1 个 user store，缺少业务 store
- `optionsCache` 是简单 Map，无 TTL、无命名空间、无自动失效

### 6.2 方案设计

#### A. 增强 useListPage composable

增加以下能力（当前缺失）：
- `handleDelete`：删除确认 + API 调用 + 重新加载
- `handleSizeChange` / `handlePageChange`：分页事件
- `transformParams`：搜索参数预处理（日期范围转换、null 过滤）
- `onDeleteSuccess`：删除后回调（刷新统计等）

使用示例：
```typescript
const { loading, tableData, pagination, searchForm, loadData,
        handleSearch, handleReset, handleDelete } =
  useListPage<Transaction, TransactionSearchForm>({
    fetchData: getTransactions,
    deleteData: deleteTransaction,
    initialSearchForm: { accountId: null, categoryId: null, dateRange: null },
    transformParams: (params) => {
      // 处理日期范围、过滤 null 字段
    }
  })
```

#### B. 创建 5 个业务 Store（取代 optionsCache）

```
frontend/src/stores/
  user.ts       (已有)
  account.ts    (新建)
  project.ts    (新建)
  category.ts   (新建)
  customer.ts   (新建)
  supplier.ts   (新建)
```

每个 Store 遵循统一模式：

```typescript
export const useAccountStore = defineStore('account', () => {
  const accounts = ref<Account[]>([])
  const loading = ref(false)
  const lastFetchTime = ref(0)
  const CACHE_TTL = 10 * 60 * 1000 // 10 分钟

  const loadOptions = async (force = false) => {
    if (!force && accounts.value.length > 0 && Date.now() - lastFetchTime.value < CACHE_TTL) return
    loading.value = true
    try {
      const { data } = await getAccounts({ page: 1, pageSize: 1000 })
      accounts.value = data.data.items
      lastFetchTime.value = Date.now()
    } finally { loading.value = false }
  }

  const invalidateCache = () => { lastFetchTime.value = 0 }
  const getItemById = (id: number) => accounts.value.find(a => a.id === id)
  const activeAccounts = computed(() => accounts.value.filter(a => a.isActive))

  return { accounts, activeAccounts, loading, loadOptions, invalidateCache, getItemById }
})
```

CRUD 操作后自动失效：
```typescript
const handleFormSuccess = () => {
  loadData()                        // 重新加载列表
  accountStore.invalidateCache()    // 使下拉缓存失效
}
```

#### C. 大组件拆分

**TransactionList.vue (768 行) → 5 个文件**

| 文件 | 职责 | 约行数 |
|------|------|--------|
| TransactionList.vue | 主容器，组装子组件 | ~120 |
| components/TransactionStatCards.vue | 统计卡片 | ~100 |
| components/TransactionFilters.vue | 筛选 + Tab | ~80 |
| components/TransactionTable.vue | 表格 + 分页 | ~120 |
| composables/useTransactionList.ts | 列表逻辑（基于 useListPage） | ~80 |

**FinanceManagement.vue (829 行) → 5 个文件**

| 文件 | 职责 | 约行数 |
|------|------|--------|
| FinanceManagement.vue | 主容器 | ~80 |
| components/FinanceSummaryCards.vue | 统计卡片 | ~120 |
| components/FinanceCharts.vue | ECharts 图表 | ~200 |
| components/FinanceFilters.vue | 筛选表单 | ~80 |
| composables/useFinanceData.ts | 数据加载 + 图表配置 | ~100 |

### 6.3 分阶段迁移

| 阶段 | 内容 |
|------|------|
| 1（基础） | 增强 useListPage + 创建 5 个 Store（不改动现有页面） |
| 2（简单页面） | CategoryList → RuleList → PersonList → SupplierList → CustomerList → AccountList |
| 3（复杂页面） | TransactionList 拆分 → FinanceManagement 拆分 |
| 4（清理） | 移除 optionsCache 使用，统一格式化函数 |

---

## 七、改进用户持久化体验（P2 - 低优先级）

### 7.1 问题现状

- `stores/user.ts` 仅持久化 token（localStorage），用户对象刷新后丢失
- F5 后主界面先闪到登录页再重定向（路由守卫触发 fetchCurrentUser）

### 7.2 方案设计

**方案 A（推荐）：使用 pinia-plugin-persistedstate**

```bash
npm install pinia-plugin-persistedstate
```

```typescript
// stores/user.ts
export const useUserStore = defineStore('user', () => {
  // ... 现有代码 ...
}, {
  persist: {
    pick: ['token', 'user'], // 仅持久化 token 和 user 对象
    storage: localStorage,
  }
})

// main.ts
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)
```

**方案 B：路由守卫中等待 fetchCurrentUser 完成**

```typescript
// router/index.ts
let isInitialized = false

router.beforeEach(async (to, from, next) => {
  const userStore = useUserStore()

  if (!isInitialized && userStore.token && !userStore.user) {
    try {
      await userStore.fetchCurrentUser()
    } catch {
      userStore.logout()
      next('/login')
      return
    }
    isInitialized = true
  }

  // ... 后续权限检查 ...
})
```

**推荐同时使用 A + B**：A 解决闪烁问题（从 localStorage 恢复用户信息），B 确保数据最终一致（后台刷新）。

---

## 附录：数据库初始化逻辑修复

### 问题

- Program.cs 第 257-284 行：无迁移时调用 `EnsureCreated`，与后续迁移冲突
- Migrations 目录不存在，完全依赖 SQL 脚本
- docker-compose 只挂载第一个 SQL 脚本

### 修复方案

#### 1. 生成 EF Core 初始迁移

```bash
cd backend
dotnet ef migrations add InitialCreate \
  --project FinanceApp.Infrastructure \
  --startup-project FinanceApp.Api \
  --output-dir Data/Migrations
```

#### 2. 修改 Program.cs 初始化逻辑

```csharp
if (!dbContext.Database.IsRelational())
{
    // InMemory（测试环境）
    await dbContext.Database.EnsureCreatedAsync();
}
else
{
    // 关系型数据库：仅使用迁移，彻底移除 EnsureCreated
    var pending = await dbContext.Database.GetPendingMigrationsAsync();
    if (pending.Any())
    {
        logger.LogInformation("执行 {Count} 个待处理迁移", pending.Count());
        await dbContext.Database.MigrateAsync();
    }
}
```

#### 3. 已有数据的生产数据库标记基线

```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL PRIMARY KEY,
    "ProductVersion" varchar(32) NOT NULL
);
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('<InitialCreate_ID>', '8.0.x');
```

#### 4. 环境变量管理敏感配置

```csharp
// Program.cs 启动验证
if (!builder.Environment.IsDevelopment())
{
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
        throw new InvalidOperationException("生产环境必须配置 Jwt__Secret 环境变量（至少 32 字符）");
}
```

---

## 执行总览

| 优先级 | 改进项 | 核心变更 | 复杂度 |
|--------|--------|---------|--------|
| P0 | 恢复分层边界 | IUnitOfWork + Repository 重构 + csproj 引用 | 高 |
| P0 | 拆分交易服务 | 1 个类 → 6 个类 + Include 扩展 | 高 |
| P1 | 统一控制器管线 | 9 个控制器继承基类，减少 ~1,290 行 | 中 |
| P1 | 前端导航权限 | useAuth composable + 动态菜单 | 中 |
| P1 | 整合 API 客户端 | crudFactory + 删死代码 + 统一类型 | 中 |
| P1 | Feature Store | useListPage + 5 个 Store + 组件拆分 | 中 |
| P2 | 用户持久化 | pinia-plugin-persistedstate | 低 |
| P2 | 数据库初始化 | EF Core 迁移 + 移除 EnsureCreated | 中 |

建议执行顺序：P0 两项可并行（后端分层 + 交易拆分），完成后再做 P1（控制器、前端权限、API、Store 可并行），最后 P2。
