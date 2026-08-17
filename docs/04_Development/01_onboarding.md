# 开发入门

状态：Active
适用对象：开发 / AI
事实源级别：Primary
最后核对日期：2026-08-17
代码依据：[`scripts/Start-Dev.ps1`](../../scripts/Start-Dev.ps1), [`docker-compose.dev.yml`](../../docker-compose.dev.yml), [`frontend/package.json`](../../frontend/package.json)

## 分支约定

- 默认开发分支：`dev`
- 发布分支：`release`
- 版本发布使用 `v*` 标签，由 `.github/workflows/release.yml` 构建并推送 GHCR 镜像 `finance-api` / `finance-web`

## 快速开始

### Windows

- 运行 `start-dev.bat`

### Linux / macOS

- 当前没有 `start-dev.sh`
- 请手动执行 `docker-compose -f docker-compose.dev.yml up -d postgres`
- 后端执行 `cd backend/FinanceApp.Api && dotnet watch run`
- 前端执行 `cd frontend && npm install && npm run dev`

## 默认开发访问地址

- 前端：`http://localhost:5173`
- API：`http://localhost:5187`
- Swagger：`http://localhost:5187/swagger`

## 默认开发账号

- 用户名：`admin`
- 密码：`DemoOnly_ChangeMe!`
- 仅用于 Development。生产引导管理员必须使用独立强密码；应用会拒绝该演示占位符。

## 环境模板

- 本地开发：`.env.example`
- 生产：`.env.production.example`
- 测试环境：`.env.testing.example`

## 新功能开发位置（模块化约定）

项目已完成 Phase 1 模块化重构，新功能请按以下约定创建文件：

### 后端

新功能应创建在 `backend/FinanceApp.Application/Modules/<对应模块>/` 下：

```
Modules/
  Identity/          → 认证、用户管理相关
  MasterData/        → 账户、分类、客户、供应商、人员、项目
  TransactionProcessing/ → 交易、转账、分摊、统计
  Reconciliation/    → 导入、规则匹配、关联
  FinanceSettlement/ → 应收、应付
  Reporting/         → 报表、仪表盘
```

每个模块内含 `Services/`、`Interfaces/`、`DTOs/` 子目录，并在对应模块注册方法中完成 DI 注册。Controller 请放在 `FinanceApp.Api/Controllers/<模块名>/` 下。

### 前端

新功能应创建在 `frontend/src/features/<对应模块>/` 下：

```
features/
  auth/              → 登录、会话
  dashboard/         → 仪表盘
  master-data/       → 基础档案
  transactions/      → 交易记录
  finance/           → 应收应付
  import/            → Excel 导入
  reconciliation/    → 关联规则
  reporting/         → 报表
  system/            → 系统配置
```

每个模块内含 `pages/`、`components/`、`api/`、`types/`、`stores/`、`routes.ts`，新路由在模块的 `routes.ts` 中注册。

> 详见：[模块化重构方案](../02_Architecture/04_modularization_refactor_plan.md) 与 [模块化开发指导](06_modularization_guide.md)

## 相关文档

- [脚本索引](02_scripts.md)
- [测试说明](04_testing.md)
- [生产部署](../05_Operations/01_deployment.md)
