# 模块化重构方案

状态：Phase 1 Completed  
适用对象：开发 / 架构 / 测试 / AI  
事实源级别：Primary  
最后核对日期：2026-03-24  
Phase 1 完成日期：2026-03-24  
代码依据：`backend/`, `frontend/src/`, `docs/02_Architecture/01_architecture.md`

## 目标

本文档用于指导当前财务系统从“按技术层分层”逐步演进为“按业务能力模块化”的结构，降低以下问题：

- 大 Service / 大页面持续膨胀
- 前后端业务边界不清，跨模块修改成本高
- 查询、权限、会话、副作用散落在多个位置
- 业务规则难沉淀，报表口径和领域行为容易漂移
- 新功能接入时只能复制粘贴既有模式，而不是复用模块能力

本文档重点提供：

- 当前问题诊断
- 目标模块边界设计
- 后端与前端的目录重组建议
- 分阶段重构路线图
- 每个阶段的验收标准与风险控制

## 适用范围与非目标

### 范围

- `backend/FinanceApp.Api`
- `backend/FinanceApp.Application`
- `backend/FinanceApp.Domain`
- `backend/FinanceApp.Infrastructure`
- `frontend/src`

### 本次重构的非目标

- 不在第一阶段切换数据库或 ORM
- 不在第一阶段引入微服务或多仓库
- 不在第一阶段追求完整 DDD 战术模式
- 不在第一阶段整体替换 UI 组件库或状态管理方案

本次建议采用“单体内模块化单体（Modular Monolith）”路线，而不是一次性大拆大立。

## 现状诊断

## 一、总体判断

当前项目已经具备明显的技术分层：

- 后端：`Api -> Application -> Domain`，`Infrastructure` 提供实现
- 前端：`views / api / stores / components / utils / router`

这意味着项目并不是“无结构”，但当前结构的主边界仍然是技术层，不是业务能力边界。

实际结果是：

- 同一个业务需求会同时修改多个横向目录
- 复杂能力会集中堆到少数大文件中
- 共享代码与业务代码混放，导致复用边界越来越模糊

## 二、后端主要问题

### 1. 应用层泄漏持久化细节

当前 `Application` 层直接依赖 EF Core，并通过 `IRepository.GetQueryable()` 暴露 `IQueryable`，使应用层可以直接写：

- `Include(...)`
- `ToListAsync()`
- `CountAsync()`
- `FirstOrDefaultAsync()`

这会导致：

- 应用层与 EF Core 强绑定
- 查询行为散落在多个 Service 中
- 仓储接口失去真正的边界意义
- 未来更换查询实现、引入缓存读模型时成本很高

### 2. 大服务承担过多职责

以下服务已经表现出“协调器过胖”特征：

- `TransactionService`
- `ImportService`
- `LinkService`
- `ReportService`
- `ProjectService`

典型症状：

- 依赖多个仓储与多个服务
- 同时负责校验、流程编排、状态变更、查询拼装、审计记录
- 一个修改点会影响多个能力

这类类文件已经接近“隐性模块”，但边界没有被显式提炼出来。

### 3. 领域对象偏贫血

`Domain` 中的实体目前主要是属性容器，很多业务不变量没有收敛到领域模型中，例如：

- 交易的状态流转
- 分摊与非分摊互斥规则
- 转账双边一致性
- 账户余额变更约束
- 项目利润口径

结果是：

- 业务规则只能堆在 Service 中
- 同一规则容易在多个 Service 中重复实现
- 测试只能围绕流程写，难以围绕领域规则写

### 4. 横切能力以“隐式副作用”存在

当前存在若干隐式行为：

- 权限规则集中在 `DataPermissionService`
- `CreatedBy` 自动填充在 `AppDbContext`
- 软删除过滤在 `AppDbContext`
- 审计记录散在各个 Service

这类设计短期方便，但长期会产生两个问题：

- 调试成本高，行为触发点不直观
- 新模块很难知道应该接入哪些隐式机制

### 5. 报表与交易口径耦合过深

`DashboardService`、`ReportService`、`ProjectService`、`TransactionStatisticsService` 都在消费交易与财务数据，但目前读模型和写模型没有显式分离。

风险包括：

- 同一指标在不同位置使用不同筛选条件
- 报表查询拖慢写模型演进
- 统计逻辑难以缓存与测试

## 三、前端主要问题

### 1. 页面承担过多职责

复杂页面如：

- `views/transactions/TransactionList.vue`
- `views/transactions/TransactionForm.vue`
- `views/import/ImportPage.vue`
- `views/finance/FinanceManagement.vue`

同时承担：

- 页面布局
- API 调用
- 选项加载
- 表格逻辑
- 筛选状态
- 弹窗编排
- 业务流程触发

这会让页面逐渐演变成“前端版应用服务”。

### 2. 页面与页面直接复用

`FinanceManagement` 直接嵌入 `ReceivableList` 与 `PayableList`，说明：

- 真正可复用的是“列表能力”
- 但当前被实现成“整页组件”

这会迫使页面为了兼容嵌入场景增加：

- `embedded`
- `externalFilters`
- `triggerReload`

这类参数一多，page 与 reusable module 的边界就会越来越模糊。

### 3. 共享组件和领域组件混放

当前 `src/components` 下同时存在：

- 通用展示组件
- 强业务语义的 Dialog / Chart / Finance 组件

例如：

- `TransferDialog.vue`
- `BatchLinkDialog.vue`
- `ImportDialog.vue`
- `ProfitAnalysisCharts.vue`

这类组件更适合归入对应业务模块，而不是继续挂在全局共享层。

### 4. 数据访问与 UI 副作用耦合

`src/utils/request.ts` 当前同时负责：

- HTTP 传输
- 错误映射
- `ElMessage`
- 用户登出
- 路由跳转

这会导致：

- 请求层难以复用到非页面场景
- 路由守卫与请求拦截器都参与会话流程
- 状态恢复链路不清晰

### 5. 部分抽象未形成统一规范

当前已有一些正确方向的抽象：

- `createCrudApi`
- `useListPage`
- options store

但这些抽象没有全面落地：

- 有的页面使用 `useListPage`
- 有的页面继续手写分页与删除逻辑
- 有的场景走 store 缓存
- 有的场景直接页面内请求

结果是团队理解成本高，代码风格逐渐分叉。

## 重构原则

## 一、业务模块优先于技术分层

未来主边界应是业务能力，而不是：

- `Services`
- `DTOs`
- `Controllers`
- `Views`

这些技术概念仍然保留，但它们应当存在于模块内部，而不是成为最外层结构。

## 二、模块之间只通过显式接口通信

禁止以下边界穿透：

- 应用层直接操作 ORM 查询细节
- 页面跨页面直接复用业务逻辑
- 通用组件依赖后端特定响应结构
- 模块内部状态被其他模块随意读取和修改

## 三、共享代码必须进入“共享层准入清单”

只有同时满足以下条件的代码，才允许进入共享层：

- 至少被两个以上业务模块复用
- 不包含模块专属业务语义
- 输入输出契约稳定
- 可以单独测试

否则应优先放在模块内部。

## 四、先收敛边界，再优化实现

重构顺序建议：

1. 先调整目录和依赖方向
2. 再拆大类、大页面
3. 再沉淀领域规则
4. 最后优化代码风格与复用

不要一开始就追求完全的领域建模，否则会把重构周期拉得过长。

## 目标模块划分

## 一、后端目标模块

建议在单体内形成以下业务模块：

### 1. Identity

职责：

- 登录、登出、会话校验
- 当前用户上下文
- 用户管理
- 账户安全策略

拥有的内容：

- 用户认证 API
- 用户管理 API
- 会话服务
- 密码策略
- 角色与权限模型

主要对应现有代码：

- `AuthController`
- `UsersController`
- `AuthService`
- `UserManagementService`
- `AuthSessionService`
- `CurrentUserService`

### 2. MasterData

职责：

- 基础档案维护
- 提供交易、导入、应收应付所依赖的静态/低频数据

拥有的内容：

- 账户
- 分类
- 客户
- 供应商
- 人员
- 项目
- 系统配置

主要对应现有代码：

- `AccountService`
- `CategoryService`
- `CustomerService`
- `SupplierService`
- `PersonService`
- `ProjectService`
- `ConfigService`

### 3. TransactionProcessing

职责：

- 交易新增、修改、删除
- 分摊处理
- 转账处理
- 账户余额联动
- 交易查询与统计

主要对应现有代码：

- `TransactionService`
- `TransactionQueryService`
- `TransferService`
- `AllocationService`
- `AccountBalanceService`
- `TransactionStatisticsService`

### 4. Reconciliation

职责：

- 导入银行流水/第三方流水
- 预览与确认导入
- 规则匹配
- 交易关联与批量关联
- 规则重跑

主要对应现有代码：

- `ImportService`
- `RuleService`
- `LinkService`
- `Import/*Parser`

### 5. FinanceSettlement

职责：

- 应收
- 应付
- 收款/付款动作
- 项目财务状态
- 对外往来分析

主要对应现有代码：

- `ReceivableService`
- `PayableService`
- 与项目财务强相关的 `ProjectService` 能力

### 6. Reporting

职责：

- Dashboard
- 月度利润
- 现金流
- 项目利润
- 人员成本
- 供应商支出
- 年度总览

主要对应现有代码：

- `DashboardService`
- `ReportService`

### 7. Platform

职责：

- 日志
- 中间件
- 异常处理
- 数据库连接
- 持久化实现
- 模块注册

说明：

`Platform` 不是业务模块，而是承载基础设施能力的平台层。

## 二、前端目标模块

建议采用 `app / core / shared / features` 四层：

### 1. `app`

放置应用级装配：

- `main.ts`
- 全局样式
- 路由组合根
- Pinia 创建

### 2. `core`

放置平台级能力：

- request client
- session 管理
- 路由守卫
- 权限判断
- 全局错误处理

### 3. `shared`

放置通用能力：

- 纯 UI 组件
- 无业务语义 composable
- formatter / date helper
- 通用类型

### 4. `features`

按业务模块拆分，例如：

- `features/auth`
- `features/master-data`
- `features/transactions`
- `features/reconciliation`
- `features/finance`
- `features/reporting`

每个模块内部自带：

- `pages`
- `components`
- `api`
- `stores`
- `composables`
- `types`
- `routes.ts`

## 目标目录草案

## 一、后端目录草案

```text
backend/
  FinanceApp.Api/
    Modules/
      Identity/
      MasterData/
      Transactions/
      Reconciliation/
      Finance/
      Reporting/
    Middleware/
    Program.cs

  FinanceApp.Application/
    Modules/
      Identity/
        Commands/
        Queries/
        Dtos/
        Interfaces/
      MasterData/
      Transactions/
      Reconciliation/
      Finance/
      Reporting/
    Common/

  FinanceApp.Domain/
    Modules/
      Identity/
      MasterData/
      Transactions/
      Reconciliation/
      Finance/
    Common/

  FinanceApp.Infrastructure/
    Persistence/
    Modules/
      Identity/
      MasterData/
      Transactions/
      Reconciliation/
      Finance/
      Reporting/
    Extensions/
```

说明：

- 第一阶段不必真的新增多个项目文件
- 可以先在现有 4 个项目内按 `Modules/` 分组
- 等边界稳定后，再考虑是否拆分程序集

## 二、前端目录草案

```text
frontend/src/
  app/
    main.ts
    router.ts
    store.ts

  core/
    request/
    session/
    auth/
    router/

  shared/
    ui/
    components/
    composables/
    utils/
    types/

  features/
    auth/
      pages/
      api/
      stores/
      routes.ts

    master-data/
      accounts/
      categories/
      customers/
      suppliers/
      persons/
      projects/

    transactions/
      pages/
      components/
      api/
      stores/
      composables/
      types/
      routes.ts

    reconciliation/
      import/
      link/
      rules/

    finance/
      receivables/
      payables/
      overview/

    reporting/
      dashboard/
      reports/
```

## 模块边界规则

## 一、后端依赖规则

### 允许

- `Api -> Application`
- `Application -> Domain`
- `Infrastructure -> Application + Domain`
- 模块内 `Command -> Domain`
- 模块内 `Query -> ReadModel / RepositoryPort`

### 禁止

- `Application` 直接依赖 `DbContext`
- `Application` 暴露 `IQueryable`
- 一个业务模块直接读另一个模块的持久化实现
- `ReportService` 直接拼装多个模块内部规则

### 推荐通信方式

- 同模块：直接通过内部服务/领域对象协作
- 跨模块：通过 Application 层显式接口协作
- 报表：通过读模型接口访问，不复用写模型流程

## 二、前端依赖规则

### 允许

- `app -> core/shared/features`
- `core -> shared`
- `features -> core/shared`
- 一个 feature 的 page 依赖本 feature 的 component/composable/api/store

### 禁止

- 一个 feature 的 page 直接 import 另一个 feature 的 page
- `shared` 依赖 `features`
- 通用组件读取后端响应壳结构
- request client 直接操作页面消息和路由跳转

## 模块内设计建议

## 一、后端模块内推荐分层

每个模块内部建议分为：

- `Commands`：写操作，关注事务与状态变更
- `Queries`：读操作，关注筛选、排序、投影
- `Dtos`：模块内 DTO
- `Policies`：权限/校验策略
- `Ports`：仓储/上下文/外部依赖接口
- `DomainServices`：跨实体规则

这样做的目标是把“读”和“写”分开，把“规则”和“流程编排”分开。

## 二、前端模块内推荐分层

每个 feature 内部建议分为：

- `pages`：路由页面，只负责页面编排
- `components`：模块专属业务组件
- `api`：模块数据访问
- `stores`：模块状态
- `composables`：模块逻辑复用
- `types`：模块类型
- `routes.ts`：模块路由定义

页面只做三件事：

- 绑定路由参数
- 装配模块组件
- 调用模块级 composable

尽量不在 page 中直写复杂业务流程。

## 针对当前重点代码的拆分建议

## 一、后端重点拆分对象

### 1. `TransactionService`

建议拆成：

- `TransactionCommandService`
- `TransactionQueryService`
- `TransferService`
- `AllocationService`
- `TransactionStatisticsService`

其中：

- 创建、更新、删除只进 `CommandService`
- 查询、列表、详情只进 `QueryService`
- 转账保持独立
- 余额联动作为交易领域能力的一部分，不对外散落暴露

### 2. `ImportService`

建议拆成：

- `ImportParseService`
- `ImportPreviewService`
- `ImportConfirmService`
- `ImportDedupService`
- `ImportBatchQueryService`

目标是让导入从“一个巨流程类”变为“可测试的子流程”。

### 3. `LinkService`

建议拆成：

- `LinkPreviewService`
- `LinkConfirmService`
- `BatchLinkService`
- `RuleRerunService`

同时把“规则匹配”和“人工确认”边界分开。

### 4. `ProjectService` 与 `ReportService`

建议明确：

- `ProjectService` 只维护项目域的主数据与必要聚合视图
- 利润分析进入 `Reporting`
- 如需持久化利润快照，应明确快照生成策略和事实来源

## 二、前端重点拆分对象

### 1. `TransactionList.vue`

建议拆成：

- `pages/TransactionListPage.vue`
- `components/TransactionTable.vue`
- `components/TransactionFilters.vue`
- `components/TransactionActions.vue`
- `composables/useTransactionList.ts`

### 2. `TransactionForm.vue`

建议拆成：

- `pages/TransactionFormPage.vue`
- `components/TransactionBaseForm.vue`
- `components/AllocationEditor.vue`
- `composables/useTransactionForm.ts`
- `composables/useTransactionOptions.ts`

### 3. `FinanceManagement.vue`

建议拆成：

- `pages/FinanceOverviewPage.vue`
- `components/FinanceSummaryCards.vue`
- `components/FinanceTrendCharts.vue`
- `components/ReceivableTable.vue`
- `components/PayableTable.vue`
- `composables/useFinanceOverview.ts`

### 4. `ImportPage.vue`

建议拆成：

- `pages/ImportPage.vue`
- `components/ImportUploader.vue`
- `components/ImportPreviewTable.vue`
- `components/ImportBatchList.vue`
- `composables/useImportPreview.ts`
- `composables/useImportBatches.ts`

## 分阶段重构路线图

## Phase 0：建立重构护栏

目标：在不大改业务的前提下，先建立重构安全网。

任务：

- 盘点高复杂度文件与跨模块依赖
- 为重构目标模块建立文档与边界约束
- 对关键流程补测试基线：登录、交易创建、导入预览/确认、收款/付款、核心报表
- 约定目录与命名规范

产出：

- 本文档
- 复杂度清单
- 重构任务看板
- 关键回归用例列表

验收标准：

- 团队能明确说出每个改动属于哪个模块
- 后续提交不再新增跨页面复用 page 的模式

## Phase 1：先做“表面模块化”

目标：先把结构收拢，不立即深改业务逻辑。

后端任务：

- 在 `Application` / `Domain` / `Infrastructure` 内引入 `Modules/` 目录
- 把现有 Service、DTO、Interface 按业务能力归档
- 将 `AddApplicationServices()` 拆为多个模块注册方法

前端任务：

- 新建 `features/` 目录
- 路由拆为模块 `routes.ts`
- 将明显的领域组件从 `src/components` 迁入 feature 内

产出：

- 模块目录骨架
- 模块注册骨架
- 路由模块化入口

验收标准：

- 新增功能优先进入 `features/*` 或 `Modules/*`
- 顶层目录不再继续膨胀

## Phase 2：收紧后端边界

目标：解决后端最关键的边界泄漏问题。

任务：

- 逐步废弃 `GetQueryable()` 在应用层的直接使用
- 提炼模块级查询端口与命令端口
- 拆分 `TransactionService`
- 拆分 `ImportService`
- 拆分 `LinkService`
- 收口 `Project` 利润口径

重点原则：

- 先在新增代码中禁用边界泄漏
- 再对核心热点模块做存量替换

验收标准：

- 核心模块中应用层不再直接写 `Include(...)`
- 交易、导入、关联三个大服务完成第一轮拆分

## Phase 3：前端 Feature 化

目标：把高复杂页面中的业务逻辑抽到 feature 内部。

任务：

- 拆 `TransactionList`、`TransactionForm`
- 拆 `FinanceManagement`
- 拆 `ImportPage`
- 把模块专属 Dialog 迁入 feature `components`
- 统一 options 数据访问策略
- 统一列表页抽象策略

验收标准：

- 页面文件明显变薄
- 不再存在一个 page import 另一个 page 的情况
- 模块逻辑优先沉淀到 composable/store/component 中

## Phase 4：治理横切能力

目标：显式化权限、会话、审计、副作用。

任务：

- request 层剥离 UI 副作用
- 会话恢复链路收口到 session 模块
- 权限判断从“类型反射 + 隐式规则”升级为显式 policy
- 审计记录规范化为模块可复用机制

验收标准：

- session 流程只有一套主链路
- 权限规则可按模块文档说明和测试验证

## Phase 5：领域规则沉淀

目标：在结构稳定后，将核心业务规则沉到领域层。

任务：

- 提炼交易不变量
- 提炼转账一致性规则
- 提炼分摊规则
- 提炼应收/应付状态流转规则
- 提炼报表事实来源与口径文档

验收标准：

- 关键业务规则可在领域测试中独立验证
- 同一规则不再在多个 Service / 页面重复实现

## 推荐优先级

## P0：必须优先处理

- 后端 `GetQueryable()` 边界泄漏
- `TransactionService` / `ImportService` / `LinkService` 过胖
- 前端 `FinanceManagement` 页面复用页面
- 前端 request 层副作用过多

## P1：紧随其后

- 模块路由拆分
- 领域组件迁移到 feature 内
- 统一列表抽象
- 统一 options 数据来源

## P2：结构稳定后推进

- 领域模型增强
- 报表读模型优化
- 生成式 API 客户端落地
- 更细粒度的模块测试基线

## 风险与控制措施

## 一、风险

- 目录大搬迁导致冲突过多
- 重构中行为回归但短期不易发现
- 过度抽象导致开发速度下降
- 一次性改动过大，主线开发被阻塞

## 二、控制措施

- 坚持分阶段提交，避免“超级 PR”
- 优先重构最重的 20% 文件，而不是全量搬迁
- 先建模块骨架，再迁移高价值能力
- 每轮只处理一个主模块，保证可回归
- 对跨模块行为设回归清单

## 架构验收指标

建议在重构过程中持续跟踪以下指标：

- 单个 Application Service 文件是否持续下降
- 单个页面文件是否持续下降
- page 是否仍 import page
- `shared` 是否出现明显业务语义代码
- 应用层中 `Include(...)` 的使用次数是否持续下降
- 新增代码是否按模块目录进入正确位置

## 重构落地建议

## 一、建议先从这 4 个点开工

1. 后端把 DI 注册拆为模块注册方法  
2. 前端把 router 拆成模块 route 文件  
3. 拆 `TransactionService` 第一轮职责  
4. 拆 `FinanceManagement` 为 page + feature components

## 二、建议的重构节奏

- 每轮聚焦一个模块
- 每轮同时覆盖后端 + 前端边界
- 每轮都包含最少量的文档更新
- 每轮都输出“迁移前后边界变化”说明

## 三、建议的提交粒度

推荐按以下粒度提交：

- `docs`: 文档与规则
- `scaffold`: 模块骨架与注册拆分
- `move`: 目录迁移但尽量不改行为
- `split`: 大文件拆分
- `cleanup`: 删除旧抽象与重复实现

这样便于回溯与回滚。

## 结论

当前项目最需要的不是“再补几个工具类”，而是把业务模块边界正式建立起来。

重构的核心不是把代码从一个目录移动到另一个目录，而是明确回答以下问题：

- 谁拥有这段业务能力
- 谁可以依赖它
- 谁不应该知道它的内部细节
- 哪些代码是共享层，哪些代码只是暂时相似

只要按“先立边界、再拆大类、后沉规则”的顺序推进，这个项目完全可以在保持单体架构的前提下，逐步演进为可持续维护的模块化单体。

## 执行状态

> 最后更新：2026-03-24

| 阶段 | 状态 | 说明 |
|---|---|---|
| **Phase 0**：建立重构护栏 | ✅ 已完成 | 分析文档、复杂度清单、验收检查表均已产出 |
| **Phase 1 后端**：表面模块化 | ✅ 已完成 | `Application` 层引入 `Modules/` 目录（6 个业务模块）；DI 注册拆分为 6 个模块注册方法；`Api` 层 Controllers 按模块分组；690 个测试全部通过 |
| **Phase 1 前端**：表面模块化 | ✅ 已完成 | `features/` 目录结构建立（auth/dashboard/master-data/transactions/finance/import/reconciliation/reporting/system）；路由从单文件拆分为各模块 `routes.ts`；import 路径全部更新；构建验证通过 |
| **Phase 2**：收紧后端边界 | ⏳ 待后续推进 | 废弃 `GetQueryable()` 应用层直接使用、拆分大 Service 等 |
| **Phase 3**：前端 Feature 化 | ⏳ 待后续推进 | 大页面拆分、领域组件迁入 feature、统一列表抽象等 |
| **Phase 4**：治理横切能力 | ⏳ 待后续推进 | request 层剥离 UI 副作用、会话收口、审计规范化等 |
| **Phase 5**：领域规则沉淀 | ⏳ 待后续推进 | 提炼交易不变量、转账一致性规则等核心业务规则 |

## 相关文档

- [系统架构](../../02_Architecture/01_architecture.md)
- [认证与权限](../../02_Architecture/02_auth_and_permissions.md)
- [数据模型](../../02_Architecture/03_data_model.md)
- [模块化开发指导](../../04_Development/06_modularization_guide.md)
- [开发待办](../../04_Development/05_backlog.md)
