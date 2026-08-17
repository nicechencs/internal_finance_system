# 生产部署

状态：Active
适用对象：运维 / 开发
事实源级别：Primary
最后核对日期：2026-08-17
代码依据：[`docker-compose.yml`](../../docker-compose.yml), [`scripts/deploy.sh`](../../scripts/deploy.sh), [`.github/workflows/release.yml`](../../.github/workflows/release.yml)

## 关键风险

- 当前回滚主要回滚容器镜像，不自动回滚数据库结构。
- 首次部署完成后必须关闭 `BOOTSTRAP_ADMIN_ENABLED`。
- 不要把开发演示密码 `DemoOnly_ChangeMe!` 用于生产；应用在非 Development 环境会拒绝该占位符。

## 当前生产拓扑

- `web` 容器对外暴露 `WEB_PORT`（可再经反向代理终止 TLS）
- `api` 容器通过 Compose 内部网络提供服务
- 默认连接外部 PostgreSQL

## 事实源

- 运行编排：根目录 `docker-compose.yml`
- 自动发布：`.github/workflows/release.yml`
- 手动部署：`scripts/deploy.sh`
- 日常运维：根目录 `scripts/*.sh`

## 分支与发布

- 默认分支：`dev`（日常开发）
- 发布分支：`release`
- 版本：`v*` 标签（例如 `v1.0.0`）
- 推送 `v*` 标签，或在 GitHub Actions 中手动运行 `Release` 并填写版本
- 工作流构建并推送 `ghcr.io/<owner>/finance-api:<version>` 与 `ghcr.io/<owner>/finance-web:<version>`（同时打 `latest`），并创建或更新对应 GitHub Release
- 服务器用 Compose 拉取上述镜像，或用 `scripts/deploy.sh` 在目标机本地构建

如需把容器挂到已有的反向代理网络，请使用未被提交的 `docker-compose.override.yml`，例如：

```yaml
services:
  api:
    networks:
      - finance_network
      - proxy_network
  web:
    networks:
      - finance_network
      - proxy_network

networks:
  proxy_network:
    external: true
    name: proxy_network
```

## 核心流程

1. 准备外部 PostgreSQL，并复制 `.env.production.example` 为 `.env.production`
2. 填写数据库、管理员、CORS 和 `GITHUB_REPO_OWNER`
3. 使用 Docker Compose 构建/拉取镜像并启动
4. 健康检查通过后关闭引导管理员

## 替代部署方案

- 手动部署：`scripts/deploy.sh`
- 测试环境部署：按需使用 `docker-compose.testing.yml`；GitHub Actions 工作流 `deploy-testing.yml` 仅手动触发，避免公开仓库在推送时自动 SSH 到测试机

## 相关文档

- [配置参考](02_configuration_reference.md)
- [上线检查清单](03_checklist.md)
- [运维脚本与日常维护](04_scripts_and_maintenance.md)
