# 模块化开发指导

状态：Active  
适用对象：开发 / 架构 / AI  
事实源级别：Primary  
最后核对日期：2026-03-24  
关联方案：`docs/02_Architecture/04_modularization_refactor_plan.md`

## 目的

本文档用于指导后续开发在当前仓库内如何落代码、如何评审模块边界、如何避免继续恶化现有耦合问题。

如果把 `04_modularization_refactor_plan.md` 看作“重构蓝图”，那么本文档就是“日常开发落地规则”。

对应的 Codex skill：`modular-refactor-guardrails`。

建议在下次会话中直接使用类似提示：

- `使用 $modular-refactor-guardrails 评审这个需求应该落在哪个模块`
- `使用 $modular-refactor-guardrails 检查这次改动是否破坏模块边界`
- `使用 $modular-refactor-guardrails 给我一个低风险的重构拆分方案`

## 一句话原则

- 新代码优先进入业务模块，而不是继续堆到横向公共目录。
- 页面只负责编排，业务逻辑优先进入模块组件、模块 composable、模块 service。
- 应用层只写业务流程，不泄漏 ORM / HTTP / UI 细节。
- 共享层只放稳定、无业务语义、跨模块复用的内容。

## 开发前先回答 4 个问题

开始编码前，先明确：

1. 这次需求属于哪个业务模块？
2. 这是写操作、读操作，还是展示编排？
3. 这段代码是否真的应该放到共享层？
4. 这次修改是否会新增跨模块依赖？

如果这 4 个问题回答不清楚，不建议直接开写。

## 模块归属速查

## 后端模块

### `Identity`

放这里的需求：

- 登录、登出、会话校验
- 用户管理
- 密码、安全、角色、权限

### `MasterData`

放这里的需求：

- 账户、分类、客户、供应商、人员、项目、配置
- 下拉数据、启用停用、基础档案维护

### `TransactionProcessing`

放这里的需求：

- 交易新增/修改/删除
- 分摊
- 转账
- 余额联动
- 交易列表、详情、统计

### `Reconciliation`

放这里的需求：

- 导入流水
- 导入预览与确认
- 去重
- 规则匹配
- 手工关联、批量关联、规则重跑

### `FinanceSettlement`

放这里的需求：

- 应收
- 应付
- 收款/付款
- 财务往来状态

### `Reporting`

放这里的需求：

- Dashboard
- 趋势图
- 月度利润
- 现金流
- 项目利润分析
- 年度总览

## 前端模块

### `auth`

- 登录页
- 会话恢复
- 当前用户信息
- 权限展示

### `master-data`

- accounts / categories / customers / suppliers / persons / projects

### `transactions`

- 交易列表、详情、表单
- 分摊编辑
- 转账弹窗
- 交易统计卡片

### `reconciliation`

- 导入页面
- 导入预览表格
- 关联弹窗
- 规则重跑

### `finance`

- 应收列表、应付列表
- Finance overview
- 收款/付款流程

### `reporting`

- Dashboard
- 报表页
- 图表组件

## 放置规则

## 一、后端放置规则

### Controller 放置

- Controller 只放 API 层
- Controller 只做参数接收、权限声明、调用应用服务、返回响应
- Controller 不直接写业务规则
- Controller 不直接写 EF 查询

### Application 放置

- 写操作放模块 `Commands` / `CommandService`
- 读操作放模块 `Queries` / `QueryService`
- DTO 放模块内 `Dtos`
- 权限/校验策略放模块 `Policies`

### Domain 放置

- 实体自身状态约束放 Domain
- 跨实体但同模块的规则放 `DomainServices`
- 不要把领域不变量散落到多个 Application Service 中

### Infrastructure 放置

- EF Core 实现
- Repository 实现
- DbContext
- 模块读模型实现
- 外部技术集成

## 二、前端放置规则

### Page 放置

Page 只负责：

- 路由参数
- 页面布局
- 组合模块组件
- 调用模块级 composable

Page 不负责：

- 直接堆大量 API 调用
- 维护过多业务状态
- 内联复杂流程逻辑
- 复用另一个 page 作为子组件

### Component 放置

模块专属组件放 feature 内部：

- `TransferDialog`
- `BatchLinkDialog`
- `ImportPreviewTable`
- `ReceivableTable`

只有纯通用组件才放共享层：

- 基础选择器
- 无业务语义表格壳
- 通用表单容器

### Composable 放置

以下逻辑优先抽到 feature composable：

- 列表筛选与分页
- 表单初始化与提交
- 选项加载
- 图表数据转换
- 组合多个 API 的页面流程

### Store 放置

Store 只保存：

- 跨页面复用状态
- 模块级缓存
- 当前会话下需要复用的上下文

不要为一次性页面局部状态创建 store。

## 禁止事项

## 后端禁止事项

- Application 层新增 `DbContext` 直接依赖
- Application 层新增 `IQueryable` 泄漏
- 一个 Service 同时承担 CRUD、统计、流程编排、报表拼装四类职责
- 将模块专属查询逻辑继续堆进公共 `Common` 目录
- 直接在多个 Service 重复实现同一业务规则

## 前端禁止事项

- 一个 page import 另一个 page
- request 层直接处理页面级业务分支
- 通用组件读取 `response.data.data` 之类的后端响应壳
- 将模块专属 dialog / chart 挂在全局 `components`
- 每个列表页各写一套近似相同的分页/删除逻辑

## 共享层准入规则

一段代码只有同时满足以下条件，才能进入共享层：

- 至少两个模块会复用
- 没有明显业务语义
- 输入输出稳定
- 不依赖某个 feature 的 store、api、route
- 能单独测试或独立推理

如果拿不准，默认先放模块内部。

## 典型坏味道清单

出现以下任一情况，说明代码很可能放错了位置：

- 新增页面超过 300 行且还在持续增长
- 一个 Service 构造函数依赖很多仓储或服务
- 一个组件既拉数据又处理表单又弹窗又做图表
- 修改一个需求要同时改 `views + api + store + utils + components` 多个横向层
- 为复用一个表格，把整个 page 当组件嵌入另一个 page
- 同一业务规则在后端两个 Service 和前端一个页面里都有实现

## 评审清单

提交前或评审时，请检查：

### 边界

- 这次改动是否明确属于一个业务模块？
- 是否新增了跨模块直接依赖？
- 是否把本该在模块内的代码放到了共享层？

### 后端

- Controller 是否保持薄？
- Application 是否只负责编排？
- 是否引入了新的 ORM 细节泄漏？
- 是否把读写职责继续混在一个大类里？

### 前端

- page 是否只做编排？
- 是否出现 page 复用 page？
- 是否把模块逻辑落在了 `utils` 或根级 `components`？
- 请求层是否新增了 UI 副作用？

### 可持续性

- 新代码是否符合目标目录结构？
- 下一位开发者是否能一眼判断这段代码归谁负责？

## 常见任务落地建议

## 一、增加新页面

优先顺序：

1. 先判断归属哪个 feature
2. 先创建 page 壳
3. 再拆 feature component
4. 再抽 composable
5. 最后决定是否需要 store

不要直接先写一个 400 行 page。

## 二、增加新接口

后端：

- 先判断属于哪个模块
- 再判断是 command 还是 query
- 再决定 DTO、接口、实现位置

前端：

- API 放 feature `api`
- 页面调用经 composable 汇总
- 如果多个页面复用，再考虑 store 或共享抽象

## 三、做列表页

优先复用统一列表模式：

- 筛选组件
- 表格组件
- 列表 composable
- 分页逻辑统一封装

不要每个列表页都重写一遍查询、删除、分页、重置流程。

## 四、做弹窗流程

如果弹窗直接对应业务动作，例如：

- 转账
- 导入确认
- 批量关联
- 收款/付款

则应进入 feature 组件，而不是根级共享组件。

## 建议的开发流程

1. 先看需求属于哪个模块  
2. 对照目标结构决定代码放置位置  
3. 避免新增共享层污染  
4. 如果涉及大页面/大服务，先拆后改  
5. 提交前跑一次边界自检  
6. 在 PR 描述中说明“本次改动归属的模块”

## PR 模板建议

建议以后在 PR 描述里增加以下字段：

- 所属模块：
- 改动类型：Command / Query / Page / Component / Composable / Store / Refactor
- 是否新增共享代码：是 / 否
- 是否新增跨模块依赖：是 / 否
- 是否触及高风险文件：是 / 否

## 与重构方案的关系

- 本文档负责“日常开发动作”
- `04_modularization_refactor_plan.md` 负责“阶段性重构路线”

当两者冲突时，以更严格的模块边界为准。

## 相关文档

- [模块化重构方案](../02_Architecture/04_modularization_refactor_plan.md)
- [系统架构](../02_Architecture/01_architecture.md)
- [开发待办](05_backlog.md)
