# 文档导航

状态：Active  
适用对象：产品 / 开发 / 测试 / 运维 / AI  
事实源级别：Primary  
最后核对日期：2026-04-24

## 文档原则

- 运行事实以代码、配置和迁移为准。
- 当前业务事实以 `docs/01_Product/`、`docs/02_Architecture/`、`docs/03_API/`、`docs/04_Development/`、`docs/05_Operations/` 为准。
- 历史报告、阶段计划、修复总结、Prompt 模板统一归档到 `docs/90_Archive/`。
- 可执行 SQL 与种子脚本放在 `database/`，文档目录只保留说明和链接。

## 现有文档清单

### 根入口与局部说明

- `README.md`：项目概览、快速启动、文档入口
- `CLAUDE.md`：AI 协作约定
- `frontend/README.md`：前端局部说明
- `scripts/README.md`：脚本局部说明
- `backend/tests/README.md`、`backend/tests/FinanceApp.Infrastructure.Tests/README.md`、`backend/tests/FinanceApp.Api.Tests/Integration/README.md`：测试局部说明
- `database/reference/er_diagram.md`：数据库参考图

### 产品

- [系统概览](01_Product/01_overview.md)
- [模块与业务规则](01_Product/02_modules_and_rules.md)
- [导入规则与使用限制](01_Product/03_import_rules.md)
- [业务决策记录](01_Product/04_business_decisions.md)
- [已知问题](01_Product/05_known_issues.md)
- [功能状态清单](01_Product/06_feature_status.md)

### 架构

- [系统架构](02_Architecture/01_architecture.md)
- [认证与权限](02_Architecture/02_auth_and_permissions.md)
- [数据模型](02_Architecture/03_data_model.md)
- [模块化重构方案](02_Architecture/04_modularization_refactor_plan.md)

### API

- [API 约定](03_API/01_api_conventions.md)
- [Swagger 与接口示例](03_API/02_swagger_and_examples.md)

### 开发

- [开发入门](04_Development/01_onboarding.md)
- [脚本索引](04_Development/02_scripts.md)
- [日志规范](04_Development/03_logging.md)
- [测试说明](04_Development/04_testing.md)
- [待办清单](04_Development/05_backlog.md)
- [模块化开发指导](04_Development/06_modularization_guide.md)
- [对方实体模式](04_Development/07_counterparty_pattern.md)
- [性能优化方案](04_Development/08_performance_optimization_plan.md)

### 运维

- [生产部署](05_Operations/01_deployment.md)
- [配置参考](05_Operations/02_configuration_reference.md)
- [上线检查清单](05_Operations/03_checklist.md)
- [运维脚本与日常维护](05_Operations/04_scripts_and_maintenance.md)
- [演示数据说明](05_Operations/05_demo_data.md)

### 归档

- [归档目录说明](90_Archive/README.md)
- 归档内包含旧需求、旧 API、旧部署、历史 AI 开发记录、复杂度报告、修复总结和已完成实施计划。

## 代码组织对照

| 主题 | 当前代码位置 |
| --- | --- |
| 后端业务模块 | `backend/FinanceApp.Application/Modules/{Identity,MasterData,TransactionProcessing,Reconciliation,FinanceSettlement,Reporting}` |
| 后端 API 控制器 | `backend/FinanceApp.Api/Controllers/{Identity,MasterData,TransactionProcessing,Reconciliation,FinanceSettlement,Reporting}` |
| 前端业务模块 | `frontend/src/features/{auth,dashboard,master-data,transactions,finance,import,reconciliation,reporting,system}` |
| 数据模型 | `backend/FinanceApp.Domain/Entities`、`backend/FinanceApp.Infrastructure/Data/Migrations`、`database/schema/01_database_schema.sql` |
| 测试 | `backend/tests/*`、`frontend/tests/*` |

## 维护约定

- 新的当前事实写入主文档，已完成计划归档到 `docs/90_Archive/implementation/`。
- `docs/superpowers/` 不作为长期活跃文档目录；计划完成后归档。
- `docs/04_Development/05_backlog.md` 只放未完成事项；已完成能力写入 [功能状态清单](01_Product/06_feature_status.md)。
- 如发现文档链接到已归档或已删除文件，应改为链接主文档或归档路径。

## 最近整理

### 2026-04-24

- 新增 [功能状态清单](01_Product/06_feature_status.md)，集中记录已完成、已修复和待开发功能。
- 将 2026-04-09 已完成的详情页、应收应付记录、交易详情访问、代码审查修复计划归档到 `docs/90_Archive/implementation/superpowers_2026-04-09/`。
- 规范顶层文档目录编号：`03_API`、`04_Development`、`05_Operations`。
- 将冗长的模块化历史计划归档，主区保留当前演进状态。
- 移除归档中的重复备份文件 `data_permission_control_plan.md.backup`。
- 删除低追溯价值的部署快照文件。
- 将散落在 `backend/` 的日志优化总结移入归档。
- 修正根 README 中已不存在的待确认事项链接。

### 2026-04-09

- 数据模型补充标签、标签规则、定期存款、应付款类型等实体。
- 模块文档新增标签管理、分类规则、标签规则章节。
- 已完成计划与历史报告迁入 `docs/90_Archive/`。
