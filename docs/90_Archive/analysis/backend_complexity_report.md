# 后端复杂度与跨模块依赖清单

状态：Completed  
适用对象：开发 / 架构 / AI  
事实源级别：Secondary  
最后核对日期：2026-03-21

> 生成日期：2026-03-24
> 分析范围：`FinanceApp.Application/Services/`、`FinanceApp.Api/Controllers/`、`FinanceApp.Infrastructure/`

---

## 一、复杂度排名表

按行数、依赖数（构造函数注入参数，不含 Logger）、`GetQueryable` 调用数综合排序。

| 排名 | Service | 行数 | 依赖数 | GetQueryable | 模块归属 | 继承 ServiceBase |
|---:|:---|---:|---:|---:|:---|:---:|
| 1 | **ImportService** | 916 | 8 | 6 | Reconciliation | - |
| 2 | **LinkService** | 872 | 12 | 17 | Reconciliation | ✅ |
| 3 | **ProjectService** | 676 | 8 | 13 | MasterData | ✅ |
| 4 | **ReportService** | 645 | 8 | 11 | Reporting | ✅ |
| 5 | **TransactionService** | 636 | 13 | 9 | TransactionProcessing | ✅ |
| 6 | **PayableService** | 483 | 7 | 6 | FinanceSettlement | ✅ |
| 7 | **ReceivableService** | 472 | 7 | 6 | FinanceSettlement | ✅ |
| 8 | **CategoryService** | 469 | 5 | 5 | MasterData | ✅ |
| 9 | **RuleService** | 460 | 6 | 0 | Reconciliation | ✅ |
| 10 | **PersonService** | 454 | 7 | 10 | MasterData | ✅ |
| 11 | **AccountService** | 453 | 6 | 5 | MasterData | ✅ |
| 12 | **SupplierService** | 390 | 5 | 4 | MasterData | ✅ |
| 13 | **CustomerService** | 384 | 5 | 4 | MasterData | ✅ |
| 14 | **TransactionQueryService** | 348 | 2 | 8 | TransactionProcessing | ✅ |
| 15 | **DashboardService** | 282 | 3 | 6 | Reporting | ✅ |
| 16 | **AuthService** | 224 | 5 | 1 | Identity | - |
| 17 | **UserManagementService** | 214 | 4 | 4 | Identity | - |
| 18 | **TransactionStatisticsService** | 182 | 3 | 6 | TransactionProcessing | ✅ |
| 19 | **AuditLogService** | 159 | 3 | 1 | Platform | - |
| 20 | **AllocationService** | 155 | 2 | 1 | TransactionProcessing | - |
| 21 | **TransferService** | 145 | 4 | 0 | TransactionProcessing | - |
| 22 | **ConfigService** | 140 | 2 | 3 | MasterData | - |
| 23 | **AccountBalanceService** | 68 | 1 | 0 | TransactionProcessing | - |
| - | ServiceBase (基类) | 80 | 2 | 0 | Platform | - |

**统计汇总**：25 个 Service 文件，总计 **9,307 行**代码，**127 次** `GetQueryable` 调用。

---

## 二、模块归属映射表

### Identity（认证与用户管理）

| 文件 | 类型 | 位置 |
|:---|:---|:---|
| AuthController | Controller | Api/Controllers/ |
| UsersController | Controller | Api/Controllers/ |
| AuthService | Service | Application/Services/ |
| UserManagementService | Service | Application/Services/ |
| AuthSessionService | Infrastructure | Infrastructure/Services/ |
| CurrentUserService | Infrastructure | Infrastructure/Services/ |
| PasswordService | Infrastructure | Infrastructure/Services/ |
| CookieSessionValidationEvents | Infrastructure | Infrastructure/Services/ |

### MasterData（基础数据）

| 文件 | 类型 |
|:---|:---|
| AccountController / AccountService | 账户管理 |
| CategoryController / CategoryService | 分类管理 |
| CustomerController / CustomerService | 客户管理 |
| SupplierController / SupplierService | 供应商管理 |
| PersonController / PersonService | 人员管理 |
| ProjectsController / ProjectService | 项目管理 |
| ConfigController / ConfigService | 系统配置 |

### TransactionProcessing（交易处理）

| 文件 | 类型 |
|:---|:---|
| TransactionsController | Controller（统一入口） |
| TransactionService | 核心编排器（Facade） |
| TransactionQueryService | 查询拆分 |
| TransactionStatisticsService | 统计拆分 |
| TransferService | 转账拆分 |
| AllocationService | 分摊逻辑 |
| AccountBalanceService | 余额调整 |

### Reconciliation（对账与规则引擎）

| 文件 | 类型 |
|:---|:---|
| ImportController / ImportService | Excel 导入 |
| RuleController / RuleService | 分类规则 |
| LinkController / LinkService | 一键关联 + 规则重跑 |

### FinanceSettlement（应收应付）

| 文件 | 类型 |
|:---|:---|
| ReceivablesController / ReceivableService | 应收款 |
| PayablesController / PayableService | 应付款 |

### Reporting（报表）

| 文件 | 类型 |
|:---|:---|
| DashboardController / DashboardService | 仪表盘 |
| ReportController / ReportService | 多维报表 |

### Platform（平台基础设施）

| 文件 | 类型 |
|:---|:---|
| ServiceBase | Service 基类（权限+审计序列化） |
| AuditLogController / AuditLogService | 审计日志 |
| GlobalExceptionHandlerMiddleware | 异常处理中间件 |
| CorrelationIdMiddleware | 请求追踪中间件 |
| SecurityHeadersMiddleware | 安全头中间件 |
| PerformanceLoggingMiddleware | 性能监控中间件 |
| AppDbContext / UnitOfWork | 数据库上下文 |
| Repository / RepositoryExtensions | 仓储实现 |
| DataPermissionService | 数据权限 |
| 16 个 Entity Configuration | 实体映射配置 |
| DbInitializer / LegacySchemaUpgrader | 数据初始化 |

---

## 三、每个 Service 的依赖注入详情

### 3.1 TransactionService（最高依赖数：13）
```
IRepository<Transaction>
IRepository<TransactionAllocation>
IRepository<Account>
IRepository<ReceivableDetail>
IRepository<PayableDetail>
IUnitOfWork
IMapper
IAuditLogService               → Platform
IAllocationService             → TransactionProcessing (同模块)
IAccountBalanceService         → TransactionProcessing (同模块)
ITransactionQueryService       → TransactionProcessing (同模块)
ITransferService               → TransactionProcessing (同模块)
ITransactionStatisticsService  → TransactionProcessing (同模块)
+ ServiceBase(ICurrentUserService, IDataPermissionService)
```

### 3.2 LinkService（12 个依赖）
```
IRepository<Transaction>       → TransactionProcessing 实体
IRepository<BankTransaction>   → Reconciliation 实体
IRepository<Customer>          → MasterData 实体
IRepository<Supplier>          → MasterData 实体
IRepository<Person>            → MasterData 实体
IRepository<Project>           → MasterData 实体
IRepository<Account>           → MasterData 实体
IRepository<Category>          → MasterData 实体
IRuleService                   → Reconciliation (同模块)
IAuditLogService               → Platform
IUnitOfWork
+ ServiceBase(ICurrentUserService, IDataPermissionService)
```

### 3.3 ImportService（8 个依赖）
```
IRepository<ImportBatch>
IRepository<Account>           → MasterData 实体
IRepository<Category>          → MasterData 实体
IRepository<BankTransaction>
IRepository<Transaction>       → TransactionProcessing 实体
IUnitOfWork
IRuleService                   → Reconciliation (同模块)
IAuditLogService               → Platform
```

### 3.4 ReportService（8 个依赖）
```
IRepository<Transaction>       → TransactionProcessing 实体
IRepository<Project>           → MasterData 实体
IRepository<Person>            → MasterData 实体
IRepository<Supplier>          → MasterData 实体
IRepository<Customer>          → MasterData 实体
IRepository<Account>           → MasterData 实体
IRepository<Receivable>        → FinanceSettlement 实体
IRepository<Payable>           → FinanceSettlement 实体
+ ServiceBase(ICurrentUserService, IDataPermissionService)
```

### 3.5 ProjectService（8 个依赖）
```
IRepository<Project>
IRepository<Customer>          → MasterData (同模块)
IRepository<Transaction>       → TransactionProcessing 实体
IRepository<TransactionAllocation> → TransactionProcessing 实体
IMapper
IAuditLogService               → Platform
IUnitOfWork
+ ServiceBase(ICurrentUserService, IDataPermissionService)
```

### 3.6 PayableService / ReceivableService（各 7 个依赖）
```
PayableService:
  IRepository<Payable>, IRepository<PayableDetail>
  IRepository<Supplier>          → MasterData 实体
  IRepository<Project>           → MasterData 实体
  IMapper, IAuditLogService, IUnitOfWork
  + ServiceBase

ReceivableService:
  IRepository<Receivable>, IRepository<ReceivableDetail>
  IRepository<Project>           → MasterData 实体
  IRepository<Customer>          → MasterData 实体
  IMapper, IAuditLogService, IUnitOfWork
  + ServiceBase
```

### 3.7 PersonService（7 个依赖）
```
IRepository<Person>
IRepository<Transaction>       → TransactionProcessing 实体
IRepository<TransactionAllocation> → TransactionProcessing 实体
IMapper, IAuditLogService, IUnitOfWork
+ ServiceBase
```

### 3.8 AccountService（6 个依赖）
```
IRepository<Account>
IRepository<Transaction>       → TransactionProcessing 实体
IMapper, IAuditLogService, IUnitOfWork
+ ServiceBase
```

### 3.9 CategoryService / CustomerService / SupplierService（各 5 个依赖）
```
各自的主 Repository
IMapper, IAuditLogService, IUnitOfWork
+ ServiceBase
```

### 3.10 RuleService（6 个依赖）
```
IRepository<ClassificationRule>
IRepository<Category>          → MasterData 实体
IMapper, IAuditLogService, IUnitOfWork
+ ServiceBase
```

### 3.11 AuthService（5 个依赖）
```
IRepository<User>
IPasswordService               → Platform
IMapper, IUnitOfWork
IOptions<AuthOptions>
```

### 3.12 UserManagementService（4 个依赖）
```
IRepository<User>
IPasswordService               → Platform
IUnitOfWork, IMapper
IOptions<AuthOptions>
```

### 3.13 TransferService（4 个依赖）
```
IRepository<Transaction>
IRepository<Account>           → MasterData 实体
IUnitOfWork
ITransactionQueryService       → TransactionProcessing (同模块)
IAuditLogService               → Platform
```

### 3.14 DashboardService（3 个 Repository 依赖）
```
IRepository<Transaction>       → TransactionProcessing 实体
IRepository<Account>           → MasterData 实体
IRepository<Project>           → MasterData 实体
+ ServiceBase
```

### 3.15 TransactionStatisticsService（3 个 Repository 依赖）
```
IRepository<Transaction>
IRepository<ReceivableDetail>  → FinanceSettlement 实体
IRepository<PayableDetail>     → FinanceSettlement 实体
+ ServiceBase
```

### 3.16 AuditLogService（3 个依赖）
```
IRepository<AuditLog>
IRepository<User>              → Identity 实体
IHttpContextAccessor
IUnitOfWork
```

### 3.17 ConfigService / AllocationService（各 2 个依赖）
```
ConfigService: IRepository<SystemConfig>, IUnitOfWork
AllocationService: IRepository<TransactionAllocation>, IUnitOfWork
```

### 3.18 AccountBalanceService（1 个依赖）
```
IRepository<Account>
```

### 3.19 TransactionQueryService（2 个依赖）
```
IRepository<Transaction>
IMapper
+ ServiceBase
```

---

## 四、跨模块依赖矩阵

行 = 依赖方模块，列 = 被依赖模块。单元格列出具体依赖的 Repository 或 Service 接口。

| 依赖方 ↓ \ 被依赖 → | Identity | MasterData | TransactionProc. | Reconciliation | FinanceSettlement | Reporting | Platform |
|:---|:---|:---|:---|:---|:---|:---|:---|
| **Identity** | - | - | - | - | - | - | IPasswordService |
| **MasterData** | - | (模块内) | Repo\<Transaction\>, Repo\<TransactionAllocation\> | - | - | - | IAuditLogService |
| **TransactionProc.** | - | Repo\<Account\> | (模块内) | - | Repo\<ReceivableDetail\>, Repo\<PayableDetail\> | - | IAuditLogService |
| **Reconciliation** | - | Repo\<Account\>, Repo\<Category\>, Repo\<Customer\>, Repo\<Supplier\>, Repo\<Person\>, Repo\<Project\> | Repo\<Transaction\>, Repo\<BankTransaction\> | (模块内：IRuleService) | - | - | IAuditLogService |
| **FinanceSettlement** | - | Repo\<Supplier\>, Repo\<Project\>, Repo\<Customer\> | - | - | (模块内) | - | IAuditLogService |
| **Reporting** | - | Repo\<Account\>, Repo\<Project\>, Repo\<Person\>, Repo\<Supplier\>, Repo\<Customer\> | Repo\<Transaction\> | - | Repo\<Receivable\>, Repo\<Payable\> | (模块内) | - |
| **Platform** | Repo\<User\> | - | - | - | - | - | (模块内) |

### 跨模块依赖统计

| 模块 | 被依赖次数 | 依赖其他模块次数 | 耦合度评级 |
|:---|---:|---:|:---|
| **MasterData** | 15 | 1 | 🟢 低耦合（纯被依赖） |
| **Platform** | 7 | 1 | 🟢 低耦合（基础设施） |
| **TransactionProcessing** | 5 | 2 | 🟡 中耦合 |
| **FinanceSettlement** | 3 | 1 | 🟡 中耦合 |
| **Reconciliation** | 1 | 3 | 🔴 高扇出 |
| **Reporting** | 0 | 4 | 🔴 高扇出（纯消费者） |
| **Identity** | 1 | 1 | 🟢 低耦合 |

---

## 五、高风险耦合点清单

### 🔴 风险等级：高

| # | 耦合点 | 涉及文件 | 风险描述 | 建议 |
|---:|:---|:---|:---|:---|
| 1 | **LinkService 依赖 8 个跨模块 Repository** | LinkService.cs | 单个 Service 直接注入了 Transaction、BankTransaction、Customer、Supplier、Person、Project、Account、Category 共 8 种 Repository，横跨 3 个目标模块。任何实体变更都可能波及此文件。 | 考虑拆分：EntityLinkService（实体关联）+ RuleRerunService（规则重跑），减少单文件职责 |
| 2 | **ReportService 依赖 8 个跨模块 Repository** | ReportService.cs | 读取 Transaction、Project、Person、Supplier、Customer、Account、Receivable、Payable，覆盖了几乎所有业务模块。 | 可为每种报表创建独立查询服务，或引入视图/物化视图减少 Repository 直接依赖 |
| 3 | **TransactionService 承担 Facade 角色（13 个依赖）** | TransactionService.cs | 虽然已拆分为 QueryService/StatisticsService/TransferService，但 TransactionService 仍需注入所有子 Service + 5 个 Repository，构造函数参数最多。 | 已完成第一轮拆分，状态尚可。后续可考虑 CQRS 模式彻底分离命令/查询 |
| 4 | **ImportService 静态缓存 `_previewCache`** | ImportService.cs | 使用 `static ConcurrentDictionary` 缓存预览数据，多实例部署时缓存不共享，长期运行存在内存泄漏风险。 | 迁移到 Redis/分布式缓存，或增加内存上限保护 |

### 🟡 风险等级：中

| # | 耦合点 | 涉及文件 | 风险描述 | 建议 |
|---:|:---|:---|:---|:---|
| 5 | **MasterData 模块反向依赖 TransactionProcessing** | PersonService, ProjectService, AccountService | 这三个 MasterData Service 注入了 `IRepository<Transaction>` / `IRepository<TransactionAllocation>` 来计算成本/趋势/利润等聚合数据。理论上 MasterData 应是被依赖方。 | 将成本计算、利润分析等方法抽取到 Reporting 或独立的 AnalyticsService，保持 MasterData 只做 CRUD |
| 6 | **TransactionStatisticsService 依赖 FinanceSettlement 实体** | TransactionStatisticsService.cs | 注入 `IRepository<ReceivableDetail>` 和 `IRepository<PayableDetail>` 用于查询关联应收应付。TransactionProcessing 模块本不应知道 FinanceSettlement 的细节。 | 将 `GetRelatedFinanceRecordsAsync` 移至 FinanceSettlement 模块或创建独立的 FinanceRecordQueryService |
| 7 | **RuleService 依赖 MasterData（Category Repository）** | RuleService.cs | 规则引擎的匹配结果是 CategoryId，因此注入了 Category Repository 做存在性验证。这是合理但需注意的耦合点。 | 可接受，但需确保 Category 表结构变更时同步更新 Rule 相关逻辑 |
| 8 | **FinanceSettlement 反向依赖 MasterData** | PayableService, ReceivableService | 应付依赖 Supplier+Project，应收依赖 Customer+Project，用于外键存在性验证。 | 合理耦合，可通过领域事件解耦但 ROI 不高 |

### 🟢 风险等级：低（需关注但无需立即处理）

| # | 耦合点 | 涉及文件 | 风险描述 |
|---:|:---|:---|:---|
| 9 | IAuditLogService 被 18 个 Service 依赖 | 几乎所有 Service | 横切关注点，如果接口变更影响面最广。但作为 Platform 基础设施这是合理设计 |
| 10 | ServiceBase 被 16 个 Service 继承 | 大部分 Service | 基类变更影响所有子类。但当前基类足够稳定（仅权限+序列化） |
| 11 | AuditLogService 依赖 Identity 的 User Repository | AuditLogService.cs | 审计日志需要关联操作人用户名，注入了 `IRepository<User>`。这是合理但跨模块的依赖 |

---

## 六、模块依赖方向图（文本版）

```
                    ┌──────────────┐
                    │   Platform   │
                    │ (AuditLog,   │
                    │  ServiceBase,│
                    │  Permission) │
                    └──────┬───────┘
                           │ 被几乎所有模块依赖
          ┌────────────────┼────────────────┐
          │                │                │
          ▼                ▼                ▼
   ┌──────────┐    ┌──────────────┐  ┌──────────┐
   │ Identity │    │  MasterData  │  │ Finance  │
   │          │    │  (Account,   │  │Settlement│
   └──────────┘    │  Category,   │  │(Recv/Pay)│
                   │  Customer,   │  └────┬─────┘
                   │  Supplier,   │       │
                   │  Person,     │       │
                   │  Project,    │       │
                   │  Config)     │       │
                   └──────┬───────┘       │
                          │               │
          ┌───────────────┼───────────────┤
          │               │               │
          ▼               ▼               ▼
  ┌───────────────┐ ┌───────────┐ ┌───────────┐
  │ Transaction   │ │Reconcilia-│ │ Reporting │
  │ Processing    │ │   tion    │ │(Dashboard,│
  │(Tx, Transfer, │ │(Import,   │ │  Report)  │
  │ Alloc, Stats) │ │ Rule,Link)│ └───────────┘
  └───────────────┘ └───────────┘
```

**核心原则检查**：
- ✅ Platform → 被所有模块依赖（正确方向）
- ✅ MasterData → 被大部分模块依赖（正确方向）
- ⚠️ MasterData 的 3 个 Service 反向依赖 TransactionProcessing（需优化）
- ⚠️ TransactionProcessing 依赖 FinanceSettlement（需优化）
- ✅ Reporting 纯消费者，无反向依赖（正确方向）

---

## 七、GetQueryable 热点分析

调用次数 ≥ 8 的文件为查询热点，数据库性能优化应优先关注：

| Service | 调用次数 | 主要查询场景 |
|:---|---:|:---|
| LinkService | 17 | 一键关联预览、批量关联扫描、规则重跑 |
| ProjectService | 13 | 分页/详情/利润报表/利润分析/统计 |
| ReportService | 11 | 月度利润/现金流/项目利润/人员成本/供应商支出/年度概览 |
| PersonService | 10 | 分页/详情/成本汇总/统计/批量创建 |
| TransactionService | 9 | 创建/更新/删除/转账候选/转换转账 |
| TransactionQueryService | 8 | 分页/详情/按账户/项目/分类/客户/供应商/人员查询 |

---

## 八、总结与优化建议

### 当前架构优势

1. **已完成的 CQRS 拆分**：TransactionService 已拆分为 Query/Statistics/Transfer/Allocation/Balance 五个子服务，降低了单文件复杂度
2. **统一的权限模型**：ServiceBase 提供了一致的权限检查入口
3. **审计日志全覆盖**：所有业务 Service 均集成了 IAuditLogService

### 优先优化方向

1. **拆分 LinkService**（872 行，12 依赖，17 次 GetQueryable）→ 拆分为 EntityLinkService + RuleRerunService
2. **抽取 MasterData 中的分析逻辑**（PersonService.GetPersonCostSummary、ProjectService.GetProjectProfitReport/ProfitAnalysis、AccountService.GetBalanceTrend）→ 移至 Reporting 或新建 AnalyticsService
3. **ImportService 缓存替换**：`static ConcurrentDictionary` → 分布式缓存（Redis）
4. **TransactionStatisticsService.GetRelatedFinanceRecords** → 移至 FinanceSettlement 模块
