# 配置参考

状态：Active
适用对象：运维 / 开发
事实源级别：Primary
最后核对日期：2026-08-17
代码依据：[`.env.production.example`](../../.env.production.example), [`docker-compose.yml`](../../docker-compose.yml)

## 必需环境变量

- `DB_HOST`
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `AUTH_COOKIE_SECURE_POLICY`
- `BOOTSTRAP_ADMIN_ENABLED`
- `CORS_ALLOWED_ORIGIN`

## 可选环境变量

- `GITHUB_REPO_OWNER`，默认 `your-github-username`（请改为实际的 GitHub 用户名或组织名）
- `WEB_PORT`，默认 `8080`

## 首次部署重点

- `BOOTSTRAP_ADMIN_ENABLED=true`
- `BOOTSTRAP_ADMIN_PASSWORD` 必须填写强密码，且不能是仓库文档中的演示占位符
- 首次部署后应改回 `false`

## 必需前置条件

- 主机已安装 Docker 与 Docker Compose
- 外部 PostgreSQL 可连接
- 已准备 `.env.production`（或等价环境变量）

## 站点名称

管理员可在登录后的「系统设置 → 站点设置」中修改站点名称和英文副标题。该配置持久化在 `system_configs` 表：

- `system_name`：站点名称，默认 `财务管理系统`，最长 50 个字符
- `system_name_en`：英文副标题，默认 `Finance Management System`，可留空，最长 80 个字符

未配置、值为空白或读取失败时，界面回退到上述默认名称，升级后不会出现空白标题。

相关接口：

- 公开读取：`GET /api/public/brand`（无需登录，只返回 `siteName` / `siteNameEn`）
- 管理员更新：`PUT /api/configs/site-brand`（仅 `Admin`）

不要把站点名称写进环境变量或前端构建参数；也不要修改代码标识（`FinanceApp.*`、`finance-api`、`finance-web`）。

## 当前注意事项

- 当前对外访问入口是 `WEB_PORT`，不是直接暴露 API 端口
- 生产镜像默认命名为 `ghcr.io/<GITHUB_REPO_OWNER>/finance-*`
- 反向代理网络不要写进公共 Compose；用 `docker-compose.override.yml` 按环境附加
