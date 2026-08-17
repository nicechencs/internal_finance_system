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

## 当前注意事项

- 当前对外访问入口是 `WEB_PORT`，不是直接暴露 API 端口
- 生产镜像默认命名为 `ghcr.io/<GITHUB_REPO_OWNER>/finance-*`
- 反向代理网络不要写进公共 Compose；用 `docker-compose.override.yml` 按环境附加
