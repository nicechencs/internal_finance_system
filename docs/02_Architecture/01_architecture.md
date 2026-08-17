# 系统架构

状态：Active
适用对象：开发 / 测试 / 运维 / AI
事实源级别：Primary
最后核对日期：2026-04-24
代码依据：[`backend/FinanceApp.Api`](../../backend/FinanceApp.Api), [`frontend/src`](../../frontend/src), [`docker-compose.yml`](../../docker-compose.yml)

## 技术栈

- 后端：.NET 8 + ASP.NET Core Web API + EF Core
- 前端：Vue 3 + TypeScript + Vite + Element Plus + Pinia
- 数据库：PostgreSQL
- 部署：Docker + Nginx

## 仓库结构

- `backend/`：后端代码与测试
- `frontend/`：前端代码与测试
- `scripts/`：开发、部署、备份、恢复、测试环境脚本
- `database/`：Schema、种子脚本、历史手工 SQL
- `docs/`：当前有效说明与归档材料

## 后端结构

- `FinanceApp.Api`：控制器、中间件、启动配置；Controllers 按业务模块分组
- `FinanceApp.Application`：应用服务、DTO、契约；已完成 Phase 1 模块化，内含 `Modules/` 目录
- `FinanceApp.Domain`：实体、枚举、领域接口
- `FinanceApp.Infrastructure`：数据访问、配置、实现

### Application 层模块结构（Phase 1 已完成）

`FinanceApp.Application/Modules/` 下按 6 个业务模块组织，每个模块含 `Services/`、`Interfaces/`、`DTOs/` 子目录：

| 模块 | 职责范围 |
|---|---|
| `Identity` | 认证、会话、用户管理、权限 |
| `MasterData` | 账户、分类、客户、供应商、人员、项目、定期存款、标签、分类规则、标签规则、配置 |
| `TransactionProcessing` | 交易增删改查、分摊、转账、余额联动、统计 |
| `Reconciliation` | 导入流水、解析器、导入批次 |
| `FinanceSettlement` | 应收、应付、收付款动作、结算交易绑定、智能关联 |
| `Reporting` | 仪表盘、月度报表、现金流、项目利润、人员成本、供应商支出 |

DI 注册已拆分为 6 个模块注册方法，不再使用单一 `AddApplicationServices()`。

## 前端结构

### Phase 1 已完成：features/ 四层架构

`frontend/src/` 采用 `app / core / shared / features` 四层结构（首阶段完成 `shared/` 和 `features/`）：

- `shared/`：纯 UI 组件、无业务语义 composable、formatter/工具函数、通用类型
- `features/`：按业务域拆分的功能模块，每个模块自带完整子结构

`features/` 下各业务模块：

| 模块目录 | 对应业务 |
|---|---|
| `auth/` | 登录、会话 |
| `dashboard/` | 仪表盘 |
| `master-data/` | 账户、分类、客户、供应商、人员、项目、定期存款、标签 |
| `transactions/` | 交易记录、转账、分摊 |
| `finance/` | 应收、应付管理 |
| `import/` | Excel 导入、批次管理 |
| `reconciliation/` | 分类规则、标签规则、规则重跑 |
| `reporting/` | 报表 API 封装；独立页面仍待接入 |
| `system/` | 系统配置、用户管理 |

每个 feature 目录内含：`pages/`、`components/`、`api/`、`types/`、`stores/`、`routes.ts`

路由已从单一 `router/index.ts` 拆分为各模块的 `routes.ts` 文件，在入口处统一组合。

## 运行拓扑

### 本地开发

- 前端开发服务器：`http://localhost:5173`
- 后端 API：`http://localhost:5187`
- 数据库：Docker 启动的 PostgreSQL 开发库

### 生产部署

- `web` 容器对外提供访问
- `api` 容器在内部网络暴露
- 默认连接外部 PostgreSQL

## 相关文档

- [认证与权限](02_auth_and_permissions.md)
- [数据模型](03_data_model.md)
- [模块化重构方案](04_modularization_refactor_plan.md)
- [生产部署](../05_Operations/01_deployment.md)
