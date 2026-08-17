# 标签规则功能设计

> 日期：2026-04-08
> 状态：设计完成，待实施

## 概述

在现有分类规则（ClassificationRule）基础上，新增**独立的标签体系**，包括标签管理、标签规则管理、实体打标功能。支持自由标签，一条交易/实体可有多个标签，用于筛选和报表统计。

## 需求摘要

| 维度 | 决策 |
|------|------|
| 标签类型 | 自由标签，用户可随意创建 |
| 作用范围 | 所有业务实体（交易、应收应付、客户、供应商、人员） |
| 与分类规则关系 | 完全独立 |
| 一条规则打标数 | 可打多个标签 |
| 触发时机 | 仅手动触发（规则重跑） |
| 标签创建方式 | 混合：可预先定义，也可在规则中即时创建 |
| 标签属性 | 名称 + 颜色 |
| 业务用途 | 筛选查找 + 报表统计 |

## 数据模型

### 1. `tags` 表 — 标签定义

| 列 | 类型 | 说明 |
|---|---|---|
| id | bigint PK | 主键 |
| name | varchar(50) | 标签名称，全局唯一 |
| color | varchar(7) | 颜色值，如 `#FF5733` |
| is_active | bool | 是否启用，默认 true |
| created_by | bigint FK → users | 创建人 |
| created_at | timestamp | 创建时间 |
| updated_at | timestamp | 更新时间 |
| deleted_at | timestamp nullable | 删除时间 |
| is_deleted | bool | 软删除标记，默认 false |

索引：
- `idx_tags_name`：唯一索引 on `name`，filtered by `is_deleted = false`
- `idx_tags_created_by`：on `created_by`，filtered by `is_deleted = false`

### 2. `tag_rules` 表 — 标签规则

| 列 | 类型 | 说明 |
|---|---|---|
| id | bigint PK | 主键 |
| rule_name | varchar(100) | 规则名称 |
| priority | int | 优先级（降序匹配，值越大越优先） |
| target_entity_type | varchar(50) | 目标实体类型 |
| match_field | varchar(50) | 匹配字段（复用 RuleMatchField 枚举） |
| match_operator | varchar(20) | 匹配操作符（复用 RuleMatchOperator 枚举） |
| match_value | text | 匹配值 |
| is_active | bool | 是否启用，默认 true |
| created_by | bigint FK → users | 创建人 |
| created_at | timestamp | 创建时间 |
| updated_at | timestamp | 更新时间 |
| deleted_at | timestamp nullable | 删除时间 |
| is_deleted | bool | 软删除标记，默认 false |

索引：
- `idx_tag_rules_priority`：on `(priority)`，filtered by `is_active = true`
- `idx_tag_rules_entity_type`：on `(target_entity_type)`，filtered by `is_active = true`
- `idx_tag_rules_created_by`：on `(created_by)`，filtered by `is_deleted = false`

### 3. `tag_rule_tags` 表 — 规则与标签多对多

| 列 | 类型 | 说明 |
|---|---|---|
| tag_rule_id | bigint FK → tag_rules | 规则 ID |
| tag_id | bigint FK → tags | 标签 ID |

- 联合主键：`(tag_rule_id, tag_id)`
- 外键均 Cascade Delete（规则删除时自动清除关联）

### 4. `entity_tags` 表 — 实体与标签关联（多态）

| 列 | 类型 | 说明 |
|---|---|---|
| id | bigint PK | 主键 |
| entity_type | varchar(50) | 实体类型枚举值 |
| entity_id | bigint | 实体 ID |
| tag_id | bigint FK → tags | 标签 ID |
| source | varchar(20) | 来源：`Manual` 或 `Rule` |
| tag_rule_id | bigint FK → tag_rules, nullable | 来源规则 ID（source=Rule 时） |
| created_at | timestamp | 打标时间 |

约束：
- 唯一约束：`(entity_type, entity_id, tag_id)` — 防止同一实体重复打同一标签
- 索引：`idx_entity_tags_entity` on `(entity_type, entity_id)` — 查询某实体所有标签
- 索引：`idx_entity_tags_tag` on `(tag_id)` — 按标签反查实体

## 枚举定义

### EntityType

```
BankTransaction
Receivable
Payable
Customer
Supplier
Person
```

### TagSource

```
Manual
Rule
```

### 各实体类型可用匹配字段

| 实体类型 | 可用 MatchField |
|---|---|
| BankTransaction | CounterpartyName, Description, Memo, Amount |
| Receivable | Description, Amount |
| Payable | Description, Amount |
| Customer | Name（新增） |
| Supplier | Name（新增） |
| Person | Name（新增） |

> 注：Customer/Supplier/Person 的匹配需要新增 `Name` 到 `RuleMatchField` 枚举。

## 后端架构

### 领域层（Domain）

新增实体：
- `Tag` — 标签定义
- `TagRule` — 标签规则，含 `TargetEntityType` 属性
- `TagRuleTag` — 规则-标签关联
- `EntityTag` — 实体-标签关联

新增枚举：
- `EntityType` — 目标实体类型
- `TagSource` — 标签来源

### 应用层（Application）

模块位置：`Modules/MasterData/`

#### ITagService

- `GetPagedAsync(PageRequest)` — 分页查询标签
- `GetByIdAsync(long id)` — 获取单个标签
- `CreateAsync(CreateTagRequest)` — 创建标签
- `UpdateAsync(long id, UpdateTagRequest)` — 更新标签
- `DeleteAsync(long id)` — 软删除
- `GetActiveTagsAsync()` — 获取所有活跃标签
- `GetOrCreateAsync(string name, string? color)` — 按名称获取或创建（支持规则表单即时创建）

#### ITagRuleService

- `GetPagedAsync(PageRequest)` — 分页查询标签规则
- `GetByIdAsync(long id)` — 获取单个规则（含关联标签）
- `CreateAsync(CreateTagRuleRequest)` — 创建规则（含标签 ID 列表，支持新标签名自动创建）
- `UpdateAsync(long id, UpdateTagRuleRequest)` — 更新规则
- `DeleteAsync(long id)` — 软删除
- `RunRulesAsync(EntityType, List<long>? entityIds)` — 手动执行规则
  - entityIds 为 null 时对该类型全部实体重跑
  - 返回 `TagRuleRunResult { AddedCount, SkippedCount }`

#### IEntityTagService

- `AddTagsAsync(EntityType, long entityId, List<long> tagIds)` — 手动打标
- `RemoveTagAsync(EntityType, long entityId, long tagId)` — 移除标签
- `GetTagsByEntityAsync(EntityType, long entityId)` — 获取实体标签列表
- `GetEntitiesByTagAsync(long tagId, EntityType?, PageRequest)` — 按标签反查实体

### API 层

#### TagController — `/api/tag`

| 方法 | 路径 | 权限 | 说明 |
|---|---|---|---|
| GET | `/api/tag` | Admin, Accountant, Viewer | 分页查询 |
| GET | `/api/tag/{id}` | Admin, Accountant, Viewer | 获取详情 |
| POST | `/api/tag` | Admin | 创建 |
| PUT | `/api/tag/{id}` | Admin | 更新 |
| DELETE | `/api/tag/{id}` | Admin | 删除 |
| GET | `/api/tag/active` | Admin, Accountant, Viewer | 获取活跃标签 |

#### TagRuleController — `/api/tag-rule`

| 方法 | 路径 | 权限 | 说明 |
|---|---|---|---|
| GET | `/api/tag-rule` | Admin, Accountant, Viewer | 分页查询 |
| GET | `/api/tag-rule/{id}` | Admin, Accountant, Viewer | 获取详情 |
| POST | `/api/tag-rule` | Admin | 创建 |
| PUT | `/api/tag-rule/{id}` | Admin | 更新 |
| DELETE | `/api/tag-rule/{id}` | Admin | 删除 |
| POST | `/api/tag-rule/run` | Admin | 手动重跑规则 |

#### EntityTagController — `/api/entity-tag`

| 方法 | 路径 | 权限 | 说明 |
|---|---|---|---|
| GET | `/api/entity-tag/{entityType}/{entityId}` | Admin, Accountant, Viewer | 查询实体标签 |
| POST | `/api/entity-tag/{entityType}/{entityId}` | Admin, Accountant | 手动打标 |
| DELETE | `/api/entity-tag/{entityType}/{entityId}/{tagId}` | Admin, Accountant | 移除标签 |
| GET | `/api/entity-tag/by-tag/{tagId}` | Admin, Accountant, Viewer | 按标签反查实体 |

## 规则重跑机制

1. 加载指定 `target_entity_type` 的所有活跃标签规则，按 `priority DESC, id ASC` 排序
2. 加载目标实体数据（全量或指定 ID 列表）
3. 逐条规则匹配每个实体：
   - 根据 `match_field` 从实体提取值
   - 用 `match_operator` 执行匹配
   - 匹配成功：将规则关联的所有标签打到实体上
4. 打标时跳过已存在的 `(entity_type, entity_id, tag_id)` 组合
5. 重跑**不会清除**手动标签（`source = Manual`）
6. 重跑**不会清除**之前规则打的标签（只增不删，避免丢失有价值的历史标签）
7. 返回结果摘要：`{ addedCount, skippedCount }`

## 前端设计

### 新增页面

#### 1. 标签管理页 — `features/master-data/tags/`

- 路由：`/tags`，归属 Master Data 分组
- 列表展示：标签名（带颜色色块）、状态、创建时间
- 新增/编辑弹窗：名称（必填）、颜色选择器（提供预设色板 + 自定义）、状态切换

#### 2. 标签规则页 — `features/reconciliation/pages/TagRuleListPage.vue`

- 路由：`/tag-rules`，归属自动化分组，与现有 `/rules` 并列
- 列表展示：规则名、目标实体类型、匹配字段、操作符、匹配值、关联标签（彩色色块）、优先级、状态
- 规则重跑按钮：选择实体类型后执行，展示结果摘要

#### 3. 标签规则表单 — `features/reconciliation/components/TagRuleForm.vue`

- 目标实体类型下拉（选择后动态更新可用匹配字段）
- 匹配条件：字段、操作符、值（与现有规则表单一致）
- 关联标签：多选组件，可从已有标签选择，也可输入新名称即时创建
- 优先级输入（0-999）
- 状态切换（编辑模式）

### 现有页面增强

#### 各实体列表页增加标签列

- 以彩色小标签（tag chip）形式展示
- 点击标签可快速筛选同标签数据
- 列表页增加标签筛选下拉

#### 各实体详情/编辑增加标签区域

- 展示当前标签（区分手动/规则来源）
- Admin/Accountant 可手动添加/移除标签

#### 报表页面

- 标签维度筛选条件
- 按标签汇总统计

## 实施范围与优先级

### Phase 1（核心功能）
- 数据模型与迁移
- 标签 CRUD（后端 + 前端）
- 标签规则 CRUD（后端 + 前端）
- 规则重跑（仅 BankTransaction）
- 交易列表页标签展示与筛选
- 手动打标/移除

### Phase 2（扩展）
- 规则重跑支持其他实体类型
- 其他实体列表页标签展示与筛选
- 报表标签维度统计
