# 模块化演进现状

状态：Active  
适用对象：开发 / 架构 / 测试 / AI  
事实源级别：Primary  
最后核对日期：2026-04-24  
历史计划：`../90_Archive/architecture/modularization_refactor_plan_2026-03-24.md`

## 当前结论

项目采用模块化单体路线。后端仍保持 `Api / Application / Domain / Infrastructure` 四个项目，业务边界主要在 Application 与 Api 层按模块分组；前端按 `features/` 组织业务能力。

## 已完成

- 后端 Application 层已拆为 6 个业务模块：`Identity`、`MasterData`、`TransactionProcessing`、`Reconciliation`、`FinanceSettlement`、`Reporting`。
- 后端 Controllers 已按业务模块分组。
- DI 注册已拆分到各模块扩展方法。
- 前端已建立 `features/` 目录，并按 9 个业务模块拆分路由。
- 大量原全局业务组件已迁入对应 feature。

## 当前模块边界

| 模块 | 后端职责 | 前端目录 |
| --- | --- | --- |
| Identity | 认证、会话、用户、审计 | `features/auth`、`features/system` |
| MasterData | 账户、分类、客户、供应商、人员、项目、标签、规则、配置 | `features/master-data`、`features/reconciliation` |
| TransactionProcessing | 交易、分摊、转账、余额、统计 | `features/transactions` |
| Reconciliation | 导入、解析、批次、规则匹配 | `features/import`、`features/reconciliation` |
| FinanceSettlement | 应收、应付、收付款、结算绑定 | `features/finance` |
| Reporting | 仪表盘、项目利润、现金流、经营报表 | `features/dashboard`、`features/reporting` |

## 后续演进

### Phase 2：收紧后端边界

- 减少 Application 层直接使用 `GetQueryable()`。
- 为复杂查询提炼模块级查询接口。
- 拆分仍偏大的 `TransactionService`、`ImportService`、`LinkService`、`ReceivableService`、`PayableService`。
- 明确报表读模型与交易写模型边界。

### Phase 3：继续前端 Feature 化

- 拆分仍偏大的列表页、详情页和表单页。
- 统一列表筛选、排序、分页和导出模式。
- 前端独立报表页接入现有报表 API。

### Phase 4：治理横切能力

- request 层剥离 UI 消息和路由跳转副作用。
- 会话恢复、权限判断和审计记录形成显式流程。
- 新模块接入权限、审计、日志时有固定模板。

### Phase 5：沉淀领域规则

- 交易、转账、分摊、应收应付状态流转规则进入领域对象或领域服务。
- 核心业务不变量用领域测试覆盖。

## 新增代码规则

- 新后端业务能力优先进入 `backend/FinanceApp.Application/Modules/<Module>`。
- 新 API 控制器优先进入 `backend/FinanceApp.Api/Controllers/<Module>`。
- 新前端页面、API、类型、组件优先进入 `frontend/src/features/<feature>`。
- 只有跨两个以上业务模块复用且不含业务语义的代码，才进入 `shared` 或公共层。
- 阶段计划完成后归档，主文档只保留当前有效规则和未完成路线。

## 关联文档

- [系统架构](01_architecture.md)
- [模块化开发指导](../04_Development/06_modularization_guide.md)
- [待办清单](../04_Development/05_backlog.md)
