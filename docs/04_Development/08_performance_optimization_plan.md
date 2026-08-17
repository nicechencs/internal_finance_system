# 性能优化方案（保留 .NET 8）

状态：Active  
适用对象：开发 / 测试 / AI  
事实源级别：Primary  
最后核对日期：2026-08-16  
代码依据：`ImportService.ConfirmAsync`、`ReportService`、`DashboardService`、`TransactionStatisticsService`、`ProjectFinancialSummaryService`

## 背景

当前瓶颈不在运行时语言，而在数据访问模式：

- 导入确认对每一行执行 2 次 `SaveChanges`
- 报表 / 统计大量 `ToListAsync()` 后在内存聚合
- 项目利润报表对每个项目再打 3 次 `SUM`

本轮只改这些热点，不换语言、不加 Redis、不改 API 契约、不改表结构。

## 目标与非目标

### 目标

- 导入确认：常见成功路径从 `O(2N)` 次提交降到常数次提交
- 报表 / 仪表盘 / 交易统计：聚合下推到 SQL，避免整表物化
- 项目利润：一次批量汇总，消灭 N+1
- 对外 DTO、权限过滤、导入部分成功语义保持不变

### 非目标

- 不用 Go 重写
- 不引入 Redis / 消息队列 / 新中间件
- 不改 Cookie 会话、`/api/*` 路径、前端页面
- 不做交易列表 `Include` 拆分（P2，另开）
- 不做导入预览缓存外置（P1 基础设施，另开）

## 范围拆分（可并行）

三个任务文件不重叠，可由多个 agent 同时改。

| 任务 | 模块 | 主文件 | 测试 |
|---|---|---|---|
| A 导入批量写入 | Reconciliation | `ImportService.cs` | `ImportServiceTests`、`ExcelImportIntegrationTests` |
| B 报表 SQL 聚合 | Reporting | `ReportService.cs`、`DashboardService.cs` | `ReportServiceTests`、`DashboardServiceTests` |
| C 项目汇总 + 交易统计 | Reporting / TransactionProcessing | `IProjectFinancialSummaryService.cs`、`ProjectFinancialSummaryService.cs`、`ReportService.GetProjectProfitReportAsync`、`TransactionStatisticsService.cs` | `ProjectFinancialSummaryServiceTests`、`ReportServiceTests` 项目利润段、`TransactionStatisticsServiceTests` |

任务 B 与 C 都可能改 `ReportService.cs`：B 负责月报 / 现金流 / 人员 / 供应商 / 年度；C 只改 `GetProjectProfitReportAsync`。合并时按方法块合入，不要互相覆盖。

## 任务 A：导入确认批量写入

### 现状

`ConfirmAsync` 在事务内对每一行：

1. 可恢复行逐条 `FirstOrDefaultAsync(UniqueHash)`
2. 新行 `Add` 银行流水后立刻 `SaveChanges` 拿自增 ID
3. 再 `Add` 业务交易并再次 `SaveChanges`
4. 用 savepoint 保证单行失败不影响其他行

500 行 ≈ 1000 次提交。

### 方案

保留外层事务、部分成功（`PartialCompleted`）和账户乐观并发。

1. 先筛出选中且非重复、非文件冲突的行。
2. 可恢复行的 `UniqueHash` 一次 `WHERE hash IN (...)` 预加载，做成字典。
3. 成功路径：
   - 内存组装全部 `BankTransaction`
   - 循环 `AddAsync`（只入 ChangeTracker）后 **一次** `SaveChangesAsync` 拿齐 ID
   - 内存组装全部 `Transaction`，再 **一次** `SaveChangesAsync`
   - 账户余额按成功行一次累加，与批次状态一起最后一次保存
4. 失败回退：批量 `SaveChanges` 抛 `DbUpdateException` 时，`ClearChangeTracker()`，再走现有逐行 savepoint 路径，保证部分成功语义不丢。
5. 内存校验失败（可恢复行找不到流水等）仍记入 `errorDetails`，不进入批量插入集合。

### 不变量

- 未选中 / 重复 / 文件冲突行仍跳过
- 可恢复行只补业务交易，不重复插银行流水
- 收入加余额、支出减余额
- 全失败 → `Failed`；有成功有失败 → `PartialCompleted`；全成功 → `Completed`
- 外层异常仍回滚，并把批次标 `Failed`（并发冲突文案保持原样）

### 验收

- 现有导入单测 / 集成测试通过
- 成功路径对 N 行新记录最多 3 次 `SaveChanges`（银行流水、业务交易、账户+批次）
- 人为制造一行唯一约束冲突时，仍能部分成功或正确回退，不能把整批账户余额写坏

## 任务 B：报表与仪表盘 SQL 聚合

### 月度利润 `GetMonthlyProfitReportAsync`

用权限过滤后的 `IQueryable`：

- `SumAsync` 分别求收入 / 支出（空集用 `(decimal?)` + `?? 0`）
- `GroupBy(TransactionType + Category.Name)` 求分类金额
- 无分类名的行不进分类明细（与现逻辑一致：`Category != null`）
- 转账不计入收入 / 支出

禁止 `Include(Category).ToListAsync()` 后再 `Sum`。

### 现金流 `GetCashflowReportAsync`

- 账户期初：`SumAsync(a => (decimal?)a.CurrentBalance)`，不要对计算属性 `Balance` 做 `SumAsync`（EF 可能无法翻译）
- 收支：SQL `SumAsync`
- 月明细：`GroupBy(Year, Month, TransactionType)` 后在内存拼 12 个月骨架和滚动余额
- 无交易的月份仍输出，期初 / 期末沿用上一月（现有测试依赖这一点）

### 人员成本 `GetPersonCostReportAsync`

人员列表可继续查出（主数据量小）。交易改为投影：

`PersonId, Amount, CategoryName`

再在内存按分类名关键字（工资 / 薪资 / 提成 / 佣金 / 报销 / 分红）归类。禁止 `Include(Person).Include(Category).ToListAsync()`。

无交易人员仍返回 0 成本行。

### 供应商支出 `GetSupplierExpenseReportAsync`

- SQL `GroupBy(SupplierId)` 求 `Sum` / `Count`
- 再查供应商名称
- `TotalExpense == 0` 的供应商不出现
- 按金额降序后写 `Rank`

### 年度总览 `GetAnnualOverviewReportAsync`

- 收支：`SumAsync`
- 月趋势：`GroupBy(Month, Type)`，内存补齐 1–12 月
- Top10：SQL `GroupBy` + `OrderByDescending` + `Take(10)`，用导航名，不要 `Include` 整实体
- 未结应收 / 应付：`SumAsync(RemainingAmount)`，不要整表 `ToList`

### 仪表盘月度 `GetMonthlyStatsAsync`

- `GroupBy(Year, Month, TransactionType)` + `Sum`
- 内存补齐请求的 N 个月，无数据月份为 0
- `GetSummaryAsync` / 分类统计已是 SQL 聚合，不改行为

### 验收

- `ReportServiceTests`、`DashboardServiceTests` 全部通过
- 数值、月份骨架、Top N、空数据与现测试一致
- 查询不再对交易整表 `ToList`

## 任务 C：项目汇总批量 + 交易统计下推

### 项目财务汇总

在 `IProjectFinancialSummaryService` 增加：

```csharp
Task<IReadOnlyDictionary<long, ProjectFinancialSummary>> GetProjectSummariesAsync(
    IReadOnlyCollection<long> projectIds);
```

实现要点：

- 一次查出这些项目（带权限过滤）
- 应收按 `ProjectId` 一次 `GroupBy`：`ReceivedAmount`、`RemainingAmount`
- 应付明细按 `Payable.ProjectId` 一次 `GroupBy`：`Amount`
- 内存组装 `ProjectFinancialSummary`，公式与单条方法完全一致
- `GetProjectSummaryAsync(id)` 改为调用批量方法取单条，避免两套算法

`ReportService.GetProjectProfitReportAsync`：

- 一次查出项目 `Id/Name/Customer.Name`
- 一次 `GetProjectSummariesAsync`
- 汇总行算法不变（`AvgProfitRate` 仍是各项目利润率的平均）

`ProjectService` 继续走单条方法即可。

### 交易统计

`BuildStatistics` 需要转账方向，方向规则在 `TransactionBalanceHelper`（字段 / 摘要「转账至」「转账自」/ 关联 ID），不能无损下推到纯 SQL。

采用混合策略：

1. 收入 / 支出：`GroupBy(TransactionType)` 做 `Count` + `Sum`，不下发整表
2. 转账：只投影 `Id, Amount, Description, TransferDirection, RelatedTransactionId, TransactionType`，内存套用现有 Helper
3. `TotalCount` = 收入数 + 支出数 + 全部转账数（含方向不明）
4. `includeAllTransferRows == false` 时，`TotalTransfer` / `TransferCount` 仍只计 `TransferDirection.Out`
5. `LogTransferDirectionWarnings` 只扫转账投影，不再加载全表
6. `GetRelatedFinanceRecordsAsync` 本轮不改

`GetPersonStatisticsAsync` 的过滤条件（直接关联或分摊到该人员）保持原 `IQueryable`，只把最后的 `ToList` + `BuildStatistics` 换成上述聚合。

### 验收

- `ProjectFinancialSummaryServiceTests` 单条结果不变
- 新增批量方法测试：多项目一次查询，结果与逐条调用一致
- `ReportServiceTests` 项目利润三段：改为 mock `GetProjectSummariesAsync`（或同时兼容单条，但实现必须走批量）
- `TransactionStatisticsServiceTests` 全部通过，尤其是「只统计转账至」和 Viewer 权限

## 风险与回退

| 风险 | 处理 |
|---|---|
| 批量插入遇到唯一约束，整批失败 | 回退逐行 savepoint |
| EF `GroupBy(Date.Year/Month)` 在 PostgreSQL 可翻译，InMemory 单测也可跑 | 保持现有 MockQueryable / InMemory 测试 |
| `Account.Balance` 是计算属性 | SQL 侧只用 `CurrentBalance` |
| 转账方向规则复杂 | 统计路径只对转账子集做内存判定 |
| `ReportService.cs` 被 B/C 同时改 | 按方法合入，C 只动项目利润方法 |

## 本轮不做

- 交易列表重 `Include` 拆投影
- 导入预览 `ConcurrentDictionary` 迁 Redis
- 规则引擎预编译 / 分桶
- 物化视图或读模型表
- 水平扩容与多实例会话

## 验证命令

```bash
dotnet test backend/FinanceApp.sln --filter "FullyQualifiedName~ImportService|FullyQualifiedName~ExcelImport|FullyQualifiedName~ReportService|FullyQualifiedName~DashboardService|FullyQualifiedName~ProjectFinancialSummary|FullyQualifiedName~TransactionStatistics"
```

全量：

```bash
dotnet test backend/FinanceApp.sln
```
