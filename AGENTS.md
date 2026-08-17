# AGENTS.md

> 通用协作约定见 [`CLAUDE.md`](CLAUDE.md)（事实源顺序、迁移规则、命名规范等）。本文件补充本地启动与测试注意事项。

## 服务概览

| 服务 | 目录 | 开发启动命令 | 端口 |
| --- | --- | --- | --- |
| PostgreSQL | Docker 或本机服务 | `docker-compose -f docker-compose.dev.yml up -d postgres` | 5432 |
| 后端 API (.NET 8) | `backend/FinanceApp.Api` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 5187（Swagger 在 `/swagger`，健康检查 `/health`） |
| 前端 (Vue3 + Vite) | `frontend` | `npm run dev` | 5173（部分环境为 3000） |

## 数据库

- 开发连接串见 `backend/FinanceApp.Api/appsettings.Development.json`，默认库名 `finance_dev`。
- 使用 Docker 时，`docker-compose.dev.yml` 会创建 `finance_dev`。
- 后端启动时会自动执行 EF Core 迁移并写入种子数据。
- 开发演示账号：`admin / DemoOnly_ChangeMe!`（仅 Development；生产引导管理员不能使用该占位符）。

## 运行与测试要点

- 启动顺序：先 PostgreSQL → 再后端（日志出现「数据库初始化完成」和 `Now listening on: http://localhost:5187`）→ 再前端。前端通过 Vite 代理把 `/api` 转发到后端。
- 认证是 **Cookie 会话**（非 JWT），登录接口 `POST /api/auth/login`。
- 后端测试：`dotnet test backend/FinanceApp.sln`，使用 EF Core InMemory，不需要 PostgreSQL。
- 前端命令：`npx vitest run`、`npm run type-check`、`npm run build`。仓库没有 ESLint 配置，后端检查即 `dotnet build`。
