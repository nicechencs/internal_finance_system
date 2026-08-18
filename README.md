# 财务管理系统

用于资金流、项目收支、应收应付和经营分析的财务管理系统。当前文档已按“产品 / 架构 / API / 开发 / 运维”重新整理，历史资料统一归档，运行事实以代码和配置为准。

## 当前事实

- 后端：.NET 8 + ASP.NET Core Web API + PostgreSQL
- 前端：Vue 3 + TypeScript + Vite + Element Plus
- 认证方式：服务端 Cookie 会话，不以 JWT 作为当前主链路
- API 基础路径：`/api/*`
- 开发演示账号：`admin / DemoOnly_ChangeMe!`（仅 Development；生产环境会拒绝该占位符）
- 数据库 Schema、种子脚本已移到 `database/`
- 本仓库默认分支为 `dev`；发布分支为 `release`；版本使用 `v*` 标签
- 站点名称默认为「财务管理系统」，管理员可在「系统设置 → 站点设置」中修改并持久化

## 文档入口

- 文档导航：[`docs/README.md`](docs/README.md)
- 开发入门：[`docs/04_Development/01_onboarding.md`](docs/04_Development/01_onboarding.md)
- API 约定：[`docs/03_API/01_api_conventions.md`](docs/03_API/01_api_conventions.md)
- 生产部署：[`docs/05_Operations/01_deployment.md`](docs/05_Operations/01_deployment.md)
- 决策记录：[`docs/DOCUMENTATION_DECISIONS.md`](docs/DOCUMENTATION_DECISIONS.md)
- 功能状态：[`docs/01_Product/06_feature_status.md`](docs/01_Product/06_feature_status.md)

## 本地开发

### Windows 一键启动

```powershell
start-dev.bat
```

### Linux / macOS 手动启动

当前仓库没有 `start-dev.sh`。如果在非 Windows 环境开发，请按下面步骤手动启动：

```bash
# 终端 1：启动数据库
docker-compose -f docker-compose.dev.yml up -d postgres

# 终端 2：启动后端
cd backend/FinanceApp.Api && dotnet watch run

# 终端 3：启动前端
cd frontend && npm install && npm run dev
```

### 访问地址

- 前端：`http://localhost:5173`
- API：`http://localhost:5187`
- Swagger：`http://localhost:5187/swagger`

### 演示数据

- Windows：`init-demo-data.bat`
- Linux / macOS：`./init-demo-data.sh`
- 种子脚本：`database/seed/seed_demo_data.sql`
- `start-dev.bat` 会在数据库里仅存在默认管理员时自动尝试导入演示数据

## 目录结构

- `docs/`：当前有效文档与治理记录
- `docs/90_Archive/`：历史需求、旧 API 文档、旧部署说明、旧 AI 开发记录
- `database/`：Schema、种子数据、手工 SQL
- `backend/`：后端代码与测试
- `frontend/`：前端代码
- `scripts/`：PowerShell 和 Shell 运维脚本

## 生产部署

当前生产事实源不再使用旧 `deploy/` 目录，而是以下内容：

- 编排文件：`docker-compose.yml`
- 自动发布：`.github/workflows/release.yml`（推送 `v*` 标签，或手动 `workflow_dispatch`）
- 发布镜像：`ghcr.io/<owner>/finance-api`、`ghcr.io/<owner>/finance-web`
- 部署脚本：`scripts/deploy.sh`
- 运维脚本：`scripts/*.sh`
- 说明文档：[`docs/05_Operations/`](docs/05_Operations)

## 说明

- 历史文档已尽量保留并迁入归档目录，便于追溯中文业务背景。
- 如果发现文档与代码冲突，请优先以代码、配置和当前主文档为准。
