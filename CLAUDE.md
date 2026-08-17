# CLAUDE.md

本文件给 AI 编码助手提供仓库协作约定，重点是“如何在现有文档结构下工作”，不是重复维护一份完整产品说明。

## 事实源顺序

1. 代码与配置
2. [`docs/README.md`](docs/README.md) 及其链接到的 Active 文档
3. [`docs/DOCUMENTATION_DECISIONS.md`](docs/DOCUMENTATION_DECISIONS.md)
4. `docs/90_Archive/` 仅作历史参考，不作当前事实源

## 当前关键事实

- 认证主链路是 Cookie 会话，不是前端持久化 JWT
- API 基础路径是 `/api/*`，不是 `/api/v1/*`
- 开发演示账号是 `admin / DemoOnly_ChangeMe!`（仅 Development，不能用于生产）
- 数据库资产已拆分到 `database/schema/`、`database/seed/`、`database/manual_sql/legacy/`
- 旧 `deploy/` 目录内容已归档，生产事实源在根目录 `docker-compose.yml`、`scripts/*.sh`
- **本地开发数据库**：PostgreSQL，运行在 Docker 中，端口 `localhost:5432`（开发环境，非 SQLite）
- **EF Core 迁移规则**：手动创建迁移**必须同时创建对应的 `.Designer.cs` 文件**（含 `[Migration]` 和 `[DbContext]` attribute），否则 EF Core 无法识别该迁移。Designer.cs 可复制上一个迁移的 Designer.cs 再修改 Migration 名称和类名。
- **运行迁移命令**（本地）：先确保 Docker 中 postgres 容器运行，再执行 `dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Api`（使用 Development 环境，连接 localhost:5432）
- **后端 Phase 1 模块化已完成**：`Application` 层按 6 个业务模块组织（`Modules/Identity/MasterData/TransactionProcessing/Reconciliation/FinanceSettlement/Reporting/`），DI 注册已拆分为 6 个模块注册方法，新后端功能应创建在对应 `Modules/<模块>/` 下
- **前端 Phase 1 模块化已完成**：采用 `features/` 四层架构，业务功能按 auth/dashboard/master-data/transactions/finance/import/reconciliation/reporting/system 分模块，路由已拆分为各模块 `routes.ts`，新前端功能应创建在 `features/<对应模块>/` 下

## 协作要求

- 处理中文文档时优先使用 UTF-8，避免把中文内容写成 GBK/ANSI 混合编码
- 如需整理文档结构，保留关键中文业务背景，不直接删除有追溯价值的历史资料
- 重要整理决策写入 [`docs/DOCUMENTATION_DECISIONS.md`](docs/DOCUMENTATION_DECISIONS.md)
- 不确定项直接在 backlog 或 known_issues 中跟踪

## 文档命名规范

- **主文档**：`<序号>_<主题>.md`（如 `01_overview.md`）
- **时间线文档**：`<主题>_<type>_YYYY-MM-DD.md`（如 `feature_design_2026-04-03.md`）
- **文档类型后缀**：
  - 设计规格：`*_design.md`
  - 实施计划：`*_plan.md`
  - 实施总结：`*_summary.md`
  - 分析报告：`*_report.md`
- **归档文档**：保持原始命名，通过目录层级标注归档时间

## 常用入口

- 文档导航：[`docs/README.md`](docs/README.md)
- 开发入门：[`docs/04_Development/01_onboarding.md`](docs/04_Development/01_onboarding.md)
- 脚本索引：[`docs/04_Development/02_scripts.md`](docs/04_Development/02_scripts.md)
- 测试说明：[`docs/04_Development/04_testing.md`](docs/04_Development/04_testing.md)
- 生产部署：[`docs/05_Operations/01_deployment.md`](docs/05_Operations/01_deployment.md)
