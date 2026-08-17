# 代码审查报告

**审查日期**: 2026-03-14
**审查范围**: 财务管理系统全栈代码
**审查人**: Claude Code Review

---

## 一、严重问题（必须修复）

### S-01: 注册接口未设置访问控制，且默认角色为 Admin

- **文件**: `backend/FinanceApp.Api/Controllers/AuthController.cs` 第 39-56 行
- **文件**: `backend/FinanceApp.Application/Services/AuthService.cs` 第 139 行
- **问题描述**: `POST /api/auth/register` 端点没有 `[Authorize]` 属性，任何人都可以注册。更严重的是，注册时默认角色硬编码为 `UserRole.Admin`，这意味着任何匿名用户都可以创建管理员账户，完全绕过权限体系。
- **建议修复**:
  1. 注册接口应仅限 Admin 用户调用（添加 `[Authorize(Roles = "Admin")]`），或
  2. 如果需要开放注册，默认角色应改为 `UserRole.Viewer`
  3. 生产环境建议关闭开放注册

### S-02: JWT Secret 硬编码在 appsettings.json 中且未被 .gitignore 排除

- **文件**: `backend/FinanceApp.Api/appsettings.json` 第 6 行
- **文件**: `backend/FinanceApp.Api/appsettings.Development.json` 第 6 行
- **问题描述**: JWT Secret 明文存储在配置文件中（`your-secret-key-must-be-at-least-32-characters-long-for-security` 和 `dev-secret-key-...`），且 appsettings.json 未被 .gitignore 排除。这些文件会被提交到版本控制系统中。
- **建议修复**:
  1. 生产环境的 JWT Secret 应通过环境变量或 Azure Key Vault / Docker Secrets 注入
  2. 将 `appsettings.Local.json` 加入 .gitignore（已存在该文件，但需确认排除策略）
  3. 更换现有 Secret 值为强随机字符串

### S-03: CORS 配置允许所有来源

- **文件**: `backend/FinanceApp.Api/Program.cs` 第 158-166 行、第 300 行
- **问题描述**: CORS 策略名为 `AllowAll`，配置了 `AllowAnyOrigin()` + `AllowAnyMethod()` + `AllowAnyHeader()`。虽然这是内部系统，但这种配置使得任何网站都可以向 API 发起跨域请求，存在 CSRF 攻击风险。
- **建议修复**: 限制为前端部署的域名，例如 `policy.WithOrigins("http://localhost:5173", "https://finance.internal.company.com")`

### S-04: 多个 Service 缺少数据权限检查（GetById/Update/Delete）

- **问题描述**: 以下 Service 虽然继承了 `ServiceBase`，但在 GetById、Update、Delete 方法中没有调用 `EnsureCanAccess`、`EnsureCanEdit`、`EnsureCanDelete`。仅 `AccountService` 和 `TransactionService` 正确实现了这些检查。
- **受影响的 Service**:
  - `CustomerService` - GetById/Update/Delete 均无权限检查
  - `SupplierService` - GetById/Update/Delete 均无权限检查
  - `PersonService` - GetById/Update/Delete 均无权限检查
  - `ProjectService` - GetById/Update/Delete 均无权限检查
  - `CategoryService` - GetById/Update/Delete 均无权限检查
  - `RuleService` - GetById/Update/Delete 均无权限检查
  - `ReceivableService` - GetById/Update 均无权限检查
  - `PayableService` - GetById/Update 均无权限检查
- **影响**: 虽然 Controller 层有 `[Authorize(Roles = ...)]` 角色限制，但 Service 层缺少细粒度的数据权限检查（如 Viewer 不应修改数据），导致防御纵深不足。
- **建议修复**: 在所有 Service 的 GetById 方法中加入 `EnsureCanAccess(entity)`，Update 中加入 `EnsureCanEdit(entity)`，Delete 中加入 `EnsureCanDelete(entity)`

---

## 二、中等问题（建议修复）

### M-01: ImportService 使用 static Dictionary 缓存预览数据（内存泄漏风险）

- **文件**: `backend/FinanceApp.Application/Services/ImportService.cs` 第 28 行
- **问题描述**: `private static readonly Dictionary<long, List<BankTransactionPreviewDto>> _previewCache = new();` 使用静态字典缓存预览数据。没有过期清理机制，如果用户上传预览但不确认导入，缓存数据将永远不会被释放。
- **建议修复**: 使用 `IMemoryCache` 替代，设置过期时间（如 30 分钟自动清除）

### M-02: 大量重复的 Include 链（TransactionService）

- **文件**: `backend/FinanceApp.Application/Services/TransactionService.cs`
- **问题描述**: 在 `GetPagedAsync`、`GetByIdAsync`、`GetByAccountAsync`、`GetByProjectAsync`、`GetByCategoryAsync`、`GetByCustomerAsync`、`GetBySupplierAsync`、`GetByPersonAsync` 共 8 个方法中，相同的 Include 链（7 个 Include + 4 个 ThenInclude）被完整重复。
- **建议修复**: 提取为私有方法 `GetTransactionQueryWithIncludes()` 统一调用

### M-03: Repository.GetQueryable() 未默认过滤软删除记录

- **文件**: `backend/FinanceApp.Infrastructure/Repositories/Repository.cs` 第 85-88 行
- **问题描述**: `GetQueryable()` 方法直接返回 `_dbSet.AsQueryable()`。虽然 EF Core 全局查询过滤器 (`HasQueryFilter`) 会在 DbContext 中自动过滤 `IsDeleted == false`，这一点是正确的。但 `GetByIdAsync` 方法使用 `FirstOrDefaultAsync(e => e.Id == id)` 而非 `FindAsync`，这会受到全局过滤器影响——这是期望行为。`ExistsAsync` 也同理。总体上软删除过滤是完整的。
- **备注**: 此项经审查后确认为误报，全局查询过滤器已正确配置，无需修改。

### M-04: AccountService.GetActiveAccountsAsync 和类似方法缺少权限过滤

- **文件**: `backend/FinanceApp.Application/Services/AccountService.cs` 第 270-292 行
- **文件**: `backend/FinanceApp.Application/Services/CustomerService.cs` `GetActiveCustomersAsync` 方法
- **文件**: `backend/FinanceApp.Application/Services/SupplierService.cs` `GetActiveSuppliersAsync` 方法
- **问题描述**: 这些 `GetActive*` 方法直接查询数据库，未调用 `ApplyPermissionFilter`。虽然这些数据通常对所有角色可见（作为下拉选项），但与其他列表查询的权限控制不一致。
- **建议修复**: 如果设计上所有用户可见则无需修改（建议添加注释说明），否则应添加权限过滤

### M-05: Controller 中大量重复的 try-catch-log-throw 模式

- **文件**: 所有 Controller（AccountController、TransactionController 等）
- **问题描述**: 每个 Controller Action 都有相同的 try-catch 模式：捕获异常 -> 记录日志 -> 重新抛出。而 `GlobalExceptionHandlerMiddleware` 已经统一处理了异常。Controller 中的 catch 是多余的，因为异常会在 Middleware 中被捕获并记录。
- **建议修复**: 移除 Controller 中的 try-catch 块，依赖全局异常处理中间件。如果需要 Controller 级别的日志，可考虑使用 Action Filter

### M-06: 交易删除时的余额回滚依赖字符串匹配

- **文件**: `backend/FinanceApp.Application/Services/TransactionService.cs` 第 447、466 行
- **问题描述**: 转账交易删除时，通过 `transaction.Description?.Contains("转账自")` 和 `Contains("转账至")` 来判断交易方向。如果描述被修改或使用其他语言，余额回滚会出错。
- **建议修复**: 使用 `TransactionType` 或添加专门的字段（如 `TransferDirection`）来标识转账方向，而非依赖描述文本

### M-07: 数据库密码硬编码在配置文件中

- **文件**: `backend/FinanceApp.Api/appsettings.json` 第 3 行
- **文件**: `backend/FinanceApp.Api/appsettings.Development.json` 第 3 行
- **问题描述**: PostgreSQL 连接字符串中包含明文密码 `Password=postgres`。
- **建议修复**: 生产环境应通过环境变量注入连接字符串

### M-08: BaseApiController.GetCurrentUserId() 可能抛出 FormatException

- **文件**: `backend/FinanceApp.Api/Controllers/BaseApiController.cs` 第 16 行
- **问题描述**: `long.Parse(userIdClaim)` 在 claim 值不是有效数字时会抛出 `FormatException`。虽然 JWT 生成时写入的是 `user.Id.ToString()`，理论上不会出现非数字情况，但使用 `long.TryParse` 更为安全。
- **建议修复**: 使用 `long.TryParse` 并在失败时抛出 `UnauthorizedAccessException`

---

## 三、低优先级问题（可选优化）

### L-01: CrudControllerBase 和 CrudServiceBase 未被使用

- **文件**: `backend/FinanceApp.Api/Controllers/Base/CrudControllerBase.cs`
- **文件**: `backend/FinanceApp.Application/Services/Base/CrudServiceBase.cs`
- **问题描述**: 这两个基类定义了通用的 CRUD 操作，但没有任何子类继承它们。所有 Controller 和 Service 都直接实现各自的方法。这些是冗余代码。
- **建议修复**: 考虑删除或在后续重构中使用它们来减少重复代码

### L-02: DataPermissionService.CanAccess 中 CreatedBy 类型检查不正确

- **文件**: `backend/FinanceApp.Infrastructure/Services/DataPermissionService.cs` 第 31 行
- **问题描述**: `if (createdBy is int createdById)` 检查 `int` 类型，但 `BaseEntity.CreatedBy` 是 `long?` 类型。应该检查 `long` 类型。由于类型不匹配，Viewer 角色的数据权限过滤在 CanAccess 方法中实际上永远不会生效（会默认返回 true）。
- **建议修复**: 将 `is int createdById` 改为 `is long createdById`

### L-03: TransactionService 中创建和分摊不在同一个事务中

- **文件**: `backend/FinanceApp.Application/Services/TransactionService.cs` 第 217-242 行
- **问题描述**: 创建交易记录时先保存交易（`SaveChangesAsync`），再保存分摊记录，最后更新账户余额。这三步不在同一个数据库事务中。如果中途失败，数据可能不一致。
- **建议修复**: 使用 `IDbContextTransaction` 包装所有操作，确保原子性

### L-04: 交易关联查询（GetByAccount/GetByProject 等）未分页

- **文件**: `backend/FinanceApp.Application/Services/TransactionService.cs` 第 511-743 行
- **问题描述**: `GetByAccountAsync`、`GetByProjectAsync` 等 6 个方法返回 `List<TransactionDto>`，没有分页参数。对于关联交易数量较大的账户或项目，可能加载过多数据。
- **建议修复**: 添加分页支持，或至少添加 `.Take(limit)` 限制返回数量

### L-05: 批量导入的 Excel 解析逻辑重复

- **文件**: `backend/FinanceApp.Api/Controllers/CustomerController.cs` 第 173-267 行
- **文件**: `backend/FinanceApp.Api/Controllers/SupplierController.cs` 第 173-268 行
- **文件**: `backend/FinanceApp.Api/Controllers/PersonController.cs` 第 173-275 行
- **文件**: `backend/FinanceApp.Api/Controllers/ProjectsController.cs` 第 193-315 行
- **问题描述**: 四个 Controller 中的 batch-import 方法有大量重复代码（文件校验、EPPlus 初始化、工作表读取逻辑等）。
- **建议修复**: 提取 Excel 解析的通用逻辑到一个公共辅助类中

### L-06: 前端 isLoggedIn 仅检查 token 存在，不验证有效性

- **文件**: `frontend/src/stores/user.ts` 第 24-26 行
- **问题描述**: `isLoggedIn()` 方法仅检查 `!!token.value`，不验证 token 是否过期或格式是否有效。如果 token 已过期，用户仍会被认为已登录，直到发起 API 请求后才被重定向到登录页。
- **建议修复**: 在 `isLoggedIn` 中解码 JWT 检查 `exp` 字段，或在前端设置定时器自动刷新/清除过期 token

### L-07: AccountService.GetBalanceTrendAsync 存在潜在 N+1 问题

- **文件**: `backend/FinanceApp.Application/Services/AccountService.cs` 第 325-402 行
- **问题描述**: 该方法分两次查询加载时间范围内和之前的所有交易（`priorTransactions` 和 `transactions`），然后在内存中遍历计算。对于交易量大的账户，这可能导致加载大量数据到内存中。
- **建议修复**: 使用数据库聚合查询（如 `GroupBy` + `Sum`）按月汇总，减少内存占用

### L-08: 前端路由守卫中分类和规则管理只允许 Admin 访问

- **文件**: `frontend/src/router/index.ts` 第 41、47 行
- **问题描述**: 路由配置中 Categories 和 Rules 页面的 `roles` 仅包含 `['Admin']`。但后端 Controller 中 GET（查看）接口允许 `Admin,Accountant,Viewer`。前后端权限定义不一致——Accountant 和 Viewer 无法在前端访问分类查看页面，但后端 API 允许他们查看。
- **建议修复**: 确认业务需求后统一前后端权限配置。如果分类和规则页面确实仅 Admin 可管理，建议将路由 roles 改为 `['Admin', 'Accountant', 'Viewer']` 允许查看，或在后端 GET 接口也限制为 Admin

---

## 四、审查总结

### 权限控制

| 维度 | 状态 |
|------|------|
| Controller [Authorize] 属性 | 完整，所有 Controller 均有类级别和方法级别的角色控制 |
| Service 层权限过滤（列表查询） | 完整，所有 Service 的 GetPaged 方法均调用 ApplyPermissionFilter |
| Service 层权限检查（单实体操作） | **不完整**，仅 AccountService/TransactionService 实现了 EnsureCanAccess/Edit/Delete |
| 前端按钮权限控制 | 完整，所有增删改按钮均有 v-if 权限判断 |
| 前端路由守卫 | 基本完整，但分类/规则页面与后端不一致 |

### 安全性

| 维度 | 状态 |
|------|------|
| SQL 注入 | **安全** - 使用 EF Core 参数化查询，未发现原生 SQL |
| XSS | **安全** - API 返回 JSON，前端使用 Vue3 自动转义 |
| JWT 配置 | 基本安全（HMAC-SHA256，验证 Issuer/Audience/Lifetime），但 Secret 管理需改进 |
| 密码存储 | **安全** - 使用 BCrypt 哈希 |
| CORS | **需改进** - AllowAnyOrigin 过于宽松 |
| 注册端点 | **严重** - 匿名可注册 Admin 账户 |

### 代码质量

| 维度 | 状态 |
|------|------|
| 命名规范 | 一致，遵循 C#/.NET 命名约定 |
| 错误处理 | 统一的异常处理中间件 + 自定义异常类型 |
| 重复代码 | Controller try-catch 模式重复、TransactionService Include 链重复、Excel 导入逻辑重复 |
| 软删除 | 完整，EF Core 全局查询过滤器 + Repository 层实现 |
| 审计日志 | 完整，所有增删改操作均记录审计日志 |

### 性能

| 维度 | 状态 |
|------|------|
| N+1 查询 | 基本无问题，使用 Include 预加载关联数据 |
| 分页 | 主查询均有分页，但关联查询（GetByAccount 等）未分页 |
| 索引 | 数据库层面已配置合理索引 |
| 静态缓存 | ImportService 的 static Dictionary 有内存泄漏风险 |
