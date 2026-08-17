# 归档文档目录

状态：Archive  
用途：历史追溯，不作为当前事实源  
最后核对日期：2026-08-17

## 说明

本目录存放已被新文档替代或不再适用的历史文档，仅供追溯使用。

公开前已对归档快照做机械脱敏：历史工程名与路径已改为现行公开标识 `FinanceApp` / `finance*`。技术经过仍保留，但不再作为当前命名或部署事实源。

当前有效文档请查看 [docs/README.md](../README.md)。文档治理决策请查看 [DOCUMENTATION_DECISIONS.md](../DOCUMENTATION_DECISIONS.md)。

## 目录结构

### `api/`

旧版 API 文档，已被 `docs/03_API/` 替代。

### `deployment/`

旧版部署文档，已被 `docs/05_Operations/` 替代。

### `implementation/`

历史实施记录、阶段计划和修复报告。

- `05_AI_Dev_legacy/`：旧版 AI 开发记录，当前不再维护对应活跃目录。
- `audit_fixes_2026-03/`：2026 年 3 月审计修复详细报告。
- `superpowers_2026-04-03/`、`superpowers_2026-04-08/`、`superpowers_2026-04-09/`：已完成的阶段设计和实施计划。
- `log_optimization_summary_2026-03-14.md`：历史日志优化总结。

### `analysis/`

复杂度分析和架构评估报告。

### `architecture/`

已完成或被当前架构文档接管的历史架构方案。

- `modularization_refactor_plan_2026-03-24.md`：长版模块化重构计划，当前主文档只保留现状与后续路线。

### `product/`

旧版产品需求文档，已被 `docs/01_Product/` 替代。

### `prompts/`

旧版 Prompt 模板。

### `reviews/`

历史评审和专项分析。

## 归档原则

- 保留有业务背景、历史决策、实施追溯价值的文档。
- 明显重复的备份文件、临时快照和空目录可以删除。
- 归档文档不作为当前开发依据；若与代码或主文档冲突，以代码和主文档为准。

## 最近归档

### 2026-04-24

- 归档 2026-04-09 已完成的详情页、应收应付记录、交易详情访问、代码审查修复计划。
- 删除重复备份 `data_permission_control_plan.md.backup`。
- 删除低追溯价值的部署目录快照 `DIRECTORY_TREE.txt`、`FILES_SUMMARY.txt`。
- 将 `backend/log_optimization_summary.txt` 迁入归档。

### 2026-04-09

- 归档已完成的标签规则、应收项目金额提示和相关阶段计划。

### 2026-04-03

- 归档临时修复报告、复杂度报告、定期存款评估、转账账户分析和 7 个已完成的 superpowers 文档。
