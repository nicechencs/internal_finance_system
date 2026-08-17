# 标签管理功能改造方案

状态：Implemented  
适用对象：开发 / 架构 / 测试 / AI  
事实源级别：Primary  
最后核对日期：2026-03-26  
实施完成日期：2026-04-03  
代码依据：`backend/FinanceApp.Domain/Entities/Transaction.cs`、`backend/FinanceApp.Domain/Entities/TransactionAllocation.cs`、`backend/FinanceApp.Application/Modules/TransactionProcessing/Services/TransactionQueryService.cs`、`backend/FinanceApp.Application/Modules/TransactionProcessing/Services/TransactionStatisticsService.cs`、`backend/FinanceApp.Application/Modules/Reporting/Services/ReportService.cs`、`backend/FinanceApp.Infrastructure/Data/Configurations/TransactionConfiguration.cs`、`backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableConfiguration.cs`、`backend/FinanceApp.Infrastructure/Data/Configurations/PayableConfiguration.cs`

## 1. 目标与原则

本文档定义“标签管理功能”的最小侵入改造方案，覆盖以下对象：

- 交易记录标签
- 项目标签
- 人员标签
- 客户标签
- 供应商标签

目标：

- 各类对象均支持多标签（多对多）
- 支持基于标签的统一筛选与查询（AND / OR）
- 支持标签维度的数据统计分析
- 支持标签交叉分析
- 尽量不破坏现有模块边界、现有查询路径和现有数据库主体结构

设计原则：

- 最小侵入：优先新增表和新增查询适配，不直接改现有业务主表结构
- 单一归属：标签定义与绑定写入由一个独立能力统一管理
- 读写分离：标签写入和标签查询/统计通过不同服务接口暴露
- 口径一致：列表、统计、报表复用同一套标签过滤逻辑
- 渐进增强：先支持标签管理与查询，再扩展到统计快照和交叉分析

## 2. 现状与约束

### 2.1 当前数据关系特点

- `Transaction` 已直接关联 `Project`、`Customer`、`Supplier`、`Person`
- `TransactionAllocation` 已承担项目、人员分摊口径
- `Receivable` / `Payable` 已采用“多来源互斥关联”模式处理主体关系
- 交易分页、交易统计、报表服务入口较集中，适合统一接入标签过滤

### 2.2 当前改造约束

- 不能大幅修改 `Transaction`、`Project`、`Person`、`Customer`、`Supplier` 主表
- 不能把标签逻辑散落到多个现有大 Service 中
- 交易与统计口径必须兼容“直接关联 + 分摊关联”两类路径
- 现有权限体系基于 `CreatedBy` 过滤，标签本身不宜复用该模型直接代替业务权限

## 3. 模块归属与推荐落点

### 3.1 主归属模块

建议将“标签定义 + 标签绑定管理”归属到主数据域，由新能力模块承载：

- `MasterData` 负责标签定义、标签绑定、标签字典读取
- `TransactionProcessing` 负责交易查询中应用标签过滤
- `Reporting` 负责标签维度统计与交叉分析

### 3.2 推荐服务拆分

建议新增以下接口，而不是继续扩展现有热点服务：

- `ITagService`：标签定义 CRUD
- `ITagBindingService`：对象打标、解绑、批量覆盖
- `ITagQueryService`：标签筛选辅助查询
- `ITagAnalyticsService`：标签统计与交叉分析

### 3.3 低风险接入方式

- 第一阶段仅新增表、配置、接口、查询扩展方法
- 第二阶段再逐步接入交易列表、主数据列表、统计接口
- 第三阶段按需增加标签报表与预计算能力

## 4. 最小侵入的数据模型设计

## 4.1 方案结论

采用“统一标签表 + 统一多对象关联表”的模式：

- 一张标签定义表：`tags`
- 一张标签绑定表：`tag_bindings`
- 可选一张统计快照表：`tag_daily_summaries`

不建议为每一种对象分别建立独立标签主表，也不建议把标签直接塞入各业务表的 JSON 字段。

## 4.2 标签定义表 `tags`

建议字段：

| 字段 | 说明 |
| --- | --- |
| `id` | 主键 |
| `scope` | 标签作用域：`transaction / project / person / customer / supplier` |
| `name` | 标签名称 |
| `code` | 可选编码，便于导入和外部对接 |
| `color` | 展示颜色 |
| `sort_order` | 排序 |
| `description` | 描述 |
| `is_active` | 是否启用 |
| `is_system` | 是否系统标签 |
| `created_at / updated_at / is_deleted / deleted_at / created_by` | 审计字段 |

建议约束：

- 唯一键：`(scope, name)`
- 可选唯一键：`(scope, code)`
- 软删场景下使用部分唯一索引

## 4.3 标签绑定表 `tag_bindings`

建议字段：

| 字段 | 说明 |
| --- | --- |
| `id` | 主键 |
| `tag_id` | 关联标签 |
| `owner_type` | 绑定对象类型：`transaction / project / person / customer / supplier` |
| `owner_id` | 绑定对象主键 |
| `created_at / updated_at / is_deleted / deleted_at / created_by` | 审计字段 |

建议约束：

- 唯一键：`(owner_type, owner_id, tag_id)`
- 外键：`tag_id -> tags.id`
- 应用层强校验：`tags.scope == owner_type`

说明：

- `owner_id` 不建议在第一阶段做数据库级多态外键
- 第一阶段使用应用层存在性校验即可，避免引入多套关联表或复杂触发器
- 如后续对一致性要求更高，可在 PostgreSQL 中补充 trigger 校验

## 4.4 可选统计快照表 `tag_daily_summaries`

仅用于高频统计场景，建议第二阶段再引入。

建议字段：

| 字段 | 说明 |
| --- | --- |
| `id` | 主键 |
| `summary_date` | 统计日期 |
| `tag_id` | 标签 |
| `metric_scope` | 指标作用域，如 `transaction` |
| `income_amount` | 收入金额 |
| `expense_amount` | 支出金额 |
| `net_amount` | 净额 |
| `transaction_count` | 笔数 |
| `version` | 重算版本 |

## 5. 不同标签类型的隔离与扩展方案

## 5.1 隔离方式

用 `scope` 作为标签天然隔离边界：

- 交易记录标签只能绑定到交易
- 项目标签只能绑定到项目
- 人员标签只能绑定到人员
- 客户标签只能绑定到客户
- 供应商标签只能绑定到供应商

这意味着：

- 同名标签可以存在于不同 `scope`
- 前端下拉和管理页按 `scope` 读取
- 应用层在绑定时校验 `tag.scope == owner_type`

## 5.2 扩展方式

未来如果增加新的标签对象（如应收、应付、合同、分类规则），只需要：

1. 扩展 `TagScope` 枚举
2. 在标签管理页增加对应作用域入口
3. 在绑定校验中支持新 `owner_type`
4. 在查询层增加新对象的标签过滤适配

数据模型本身无需重做。

## 5.3 权限建议

- 标签定义读取：所有登录用户可读
- 标签定义管理：`Admin` / `Accountant`
- 标签绑定管理：继承 owner 对象的编辑权限
- 标签统计读取：和现有统计/报表权限保持一致

## 6. 对现有查询逻辑的改造方式

## 6.1 统一过滤模型

建议新增标签过滤模型，而不是为每个接口拼接分散参数。

建议结构：

```text
TagFilterGroup
- Scope
- TagIds[]
- MatchMode (and/or)
```

建议规则：

- 同一组内使用 `AND` 或 `OR`
- 多组之间默认使用 `AND`

示例：

- 交易标签：`已对账 OR 高优先级`
- 项目标签：`区域A AND 战略项目`
- 人员标签：`销售`

表示为三组过滤条件，最终按组间 `AND` 合并。

## 6.2 请求模型改造建议

当前大量分页与统计接口共用 `PageRequest`。为兼顾最小侵入与可扩展性，建议：

- 第一阶段：在 `PageRequest` 中增加可选 `TagFilters` 字段
- 不新增多组扁平字段，不引入 `ProjectTagIds`、`CustomerTagIds` 这种横向膨胀参数
- 各业务接口按需消费 `TagFilters`，未使用的接口保持不变

这样可以：

- 兼容现有控制器和分页模型
- 让交易查询支持多组跨对象标签过滤
- 让项目/人员/客户/供应商列表只消费本对象对应 `scope`

## 6.3 查询实现建议

建议新增统一扩展方法：

- `ApplyTagFiltersForOwners<T>()`
- `ApplyTagFiltersForTransactions()`

### 对主数据列表的改造

- `ProjectService.GetPagedAsync` / `GetStatisticsAsync`：支持项目标签过滤
- `PersonService.GetPagedAsync` / `GetStatisticsAsync`：支持人员标签过滤
- `CustomerService.GetPagedAsync` / `GetStatisticsAsync`：支持客户标签过滤
- `SupplierService.GetPagedAsync` / `GetStatisticsAsync`：支持供应商标签过滤

实现方式：

- 根据 `owner_type + owner_id` 在 `tag_bindings` 中做 `EXISTS` / `GROUP BY HAVING`
- 不修改对应实体主表结构

### 对交易查询的改造

交易查询是本次改造重点。建议支持以下五类标签同时参与过滤：

- 交易记录标签
- 项目标签
- 人员标签
- 客户标签
- 供应商标签

其中：

- 交易标签：过滤 `Transaction.Id`
- 客户/供应商标签：过滤 `Transaction.CustomerId`、`Transaction.SupplierId`
- 项目/人员标签：必须覆盖“直接字段 + 分摊记录”两种路径

项目/人员过滤口径建议与现有交易查询保持一致：

- 项目：`Transaction.ProjectId == projectId` 或 `Transaction.Allocations.Any(a => a.ProjectId == projectId)`
- 人员：`Transaction.PersonId == personId` 或 `Transaction.Allocations.Any(a => a.PersonId == personId)`

标签过滤也应沿用这套口径，否则交易列表与现有项目/人员统计会出现偏差。

## 6.4 AND / OR 的数据库实现建议

### OR

同一作用域下任意命中即可：

- `EXISTS (SELECT 1 FROM tag_bindings ... WHERE tag_id IN (...))`

### AND

同一作用域下必须全部命中：

- 按 `owner_id` 分组
- `HAVING COUNT(DISTINCT tag_id) = 标签数量`

该实现同时适用于主数据对象和交易对象。

## 7. 对现有统计与报表逻辑的改造方式

## 7.1 统一入口原则

标签过滤不要直接散写到每个统计方法内，建议：

1. 先构造基础业务查询
2. 再统一应用 `ApplyTagFilters...`
3. 最后做 `Sum / Count / GroupBy`

这样可以保证：

- 列表与统计口径一致
- 后续报表支持标签维度时复用同一套逻辑
- 查询优化点集中，方便后续替换为预计算表

## 7.2 交易统计改造建议

`TransactionStatisticsService` 增加标签过滤支持：

- 交易总收入
- 交易总支出
- 净额
- 收入/支出/划转笔数

改造方式：

- 复用交易查询同一套标签过滤表达式
- 先过滤，再统计
- 保持未传标签条件时行为不变

## 7.3 标签维度统计建议

建议新增独立分析接口，而不是把所有逻辑继续塞进 `ReportService`：

- `GetTagSummary(scope, filters)`
- `GetTagBreakdown(scope, filters)`
- `GetTagCrossAnalysis(rowScope, columnScope, filters)`

建议输出指标：

- 交易笔数
- 收入金额
- 支出金额
- 净额
- 占比

## 7.4 交叉分析口径建议

示例：

- 项目标签 × 人员标签
- 客户标签 × 供应商标签
- 交易标签 × 项目标签

建议以“交易事实”作为统计基准，再按标签维度映射到行列。

注意：多标签天然不是互斥维度，因此：

- 标签分组统计的分组小计之和可能大于总体汇总
- 交叉分析中的同一交易也可能命中多个单元格

该口径需要在文档和前端展示中明确说明。

## 8. 性能优化方案

## 8.1 基础索引

建议索引：

### `tags`

- `idx_tags_scope_name(scope, name)`
- `idx_tags_scope_active(scope, is_active)`

### `tag_bindings`

- `idx_tag_bindings_owner(owner_type, owner_id, tag_id)`
- `idx_tag_bindings_tag(tag_id, owner_type, owner_id)`
- 唯一索引 `ux_tag_bindings_owner_tag(owner_type, owner_id, tag_id)`

在 PostgreSQL 下，若交易标签数据量显著更大，可增加部分索引：

- `WHERE owner_type = 'transaction'`

## 8.2 查询优化

- 优先在 `tag_bindings` 上缩小候选集，再回表到业务对象
- 尽量使用 `EXISTS` 和半连接思路，避免大范围 `IN (subquery)` 嵌套
- 标签查询与主业务 `Include` 链分离，避免把标签加载耦合进现有详情查询

## 8.3 缓存建议

适合缓存：

- 标签字典（按 `scope`）
- 标签详情映射（`tag_id -> tag_name/color`）
- 热门标签统计结果

建议：

- TTL 5 到 15 分钟
- 标签增删改、绑定变更后按 `scope` 精准失效
- 不缓存带强权限差异的业务对象集合，只缓存标签元数据和公共分析结果

## 8.4 预计算建议

第一阶段不做重型预计算。

第二阶段如出现以下情况，再引入 `tag_daily_summaries`：

- 标签统计成为首页或高频报表入口
- 同一统计条件被频繁重复查询
- 大量交叉分析导致查询时间不可接受

预计算建议：

- 按日聚合
- 支持增量重算
- 保留 `version` 字段便于纠错和回滚

## 9. 旧数据兼容与迁移方案

## 9.1 兼容策略

- 该方案是纯增量改造，不影响现有业务主表和历史数据
- 老数据即使没有标签，也不影响现有列表、统计、报表使用
- 未传标签参数时，系统行为与当前保持一致

## 9.2 建议迁移顺序

### 阶段一：建模

- 新增 `tags`
- 新增 `tag_bindings`
- 新增索引、唯一约束、外键

### 阶段二：管理能力

- 上线标签定义管理 API
- 上线标签绑定/解绑 API
- 上线按对象查询标签 API

### 阶段三：查询接入

- 主数据分页查询支持标签过滤
- 交易分页查询支持标签过滤
- 交易统计支持标签过滤

### 阶段四：分析增强

- 新增标签维度统计
- 新增交叉分析
- 视性能情况引入快照表

## 9.3 历史数据处理建议

不建议在第一阶段强制回填历史标签。

推荐做法：

- 先支持人工打标与批量导入打标
- 如后续存在明确业务规则，再做历史标签回填
- 回填过程应是幂等的、可重复执行的脚本

可选回填来源：

- Excel 导入文件中的已有标记列
- 现有业务规则映射
- 人工维护的项目/人员/客户/供应商名单

## 9.4 数据校验与巡检

建议增加定时巡检任务，检查：

- 重复绑定
- `tag.scope` 与 `owner_type` 不一致
- owner 不存在但仍有绑定
- 已软删标签仍被活跃绑定

## 10. 边界风险与规避建议

## 10.1 风险一：继续放大现有热点 Service

如果直接把标签 CRUD、标签过滤、标签统计都塞进现有 `ProjectService`、`TransactionQueryService`、`ReportService`，会进一步放大热点文件。

规避建议：

- 标签定义与绑定用独立服务
- 查询层通过扩展方法或独立查询服务复用
- 报表层通过 `ITagAnalyticsService` 聚合结果

## 10.2 风险二：交易标签口径与项目/人员分摊口径不一致

如果交易标签查询只看 `Transaction.ProjectId` / `Transaction.PersonId`，则会漏掉通过 `transaction_allocations` 命中的交易。

规避建议：

- 对项目、人员相关标签过滤统一复用现有 direct + allocation 口径

## 10.3 风险三：多标签统计被误读为互斥维度

标签天然允许多选，因此分组汇总可能重复计数。

规避建议：

- 在 API 文档和前端图表说明中明确“标签分组非互斥”
- 总计和标签分组小计不要强制做相等假设

## 11. 推荐实施清单

按优先级建议如下：

### P1：最小可交付

- 新增 `tags`、`tag_bindings`
- 新增标签定义与绑定 API
- 交易列表支持标签过滤
- 项目/人员/客户/供应商列表支持标签过滤
- 交易统计支持标签过滤

### P2：分析增强

- 标签维度汇总统计
- 标签筛选条件与其他维度条件联合分析
- 标签交叉分析

### P3：性能增强

- 缓存标签字典
- 缓存高频统计结果
- 引入 `tag_daily_summaries`

## 12. 最终建议

综合当前系统结构，推荐落地方案如下：

- 使用统一标签表 `tags`
- 使用统一多对象绑定表 `tag_bindings`
- 用 `scope` 做标签类型隔离
- 用独立标签服务承接标签定义和绑定写入
- 用统一标签过滤扩展方法接入现有交易查询、主数据查询、统计与报表
- 第一阶段不改现有业务主表、不做重型预计算、不强制回填历史数据

该方案兼顾了：

- 最小侵入
- 可落地
- 可扩展
- 与现有系统架构兼容

