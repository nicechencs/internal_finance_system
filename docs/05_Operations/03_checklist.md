# 上线检查清单

状态：Active
适用对象：运维 / 开发
事实源级别：Primary
最后核对日期：2026-08-17

## 部署前

- 发布内容已合入 `release`，并准备好 `v*` 版本标签
- Docker / Docker Compose 可用
- 环境变量已配置（`.env.production`）
- `BOOTSTRAP_ADMIN_PASSWORD` 不是演示占位符 `DemoOnly_ChangeMe!`
- 外部数据库可连接
- 磁盘空间充足
- 若使用自动发布：已推送 `v*` 标签（或手动运行 GitHub Actions `Release`），并确认 GHCR 上已有 `finance-api` / `finance-web` 对应版本
- **数据迁移前置检查**：若包含 `MakeSettlementTransactionIdRequired` 迁移，先执行以下查询确认无孤立数据：
  ```sql
  SELECT COUNT(*) FROM receivable_details WHERE transaction_id IS NULL AND deleted_at IS NULL;
  SELECT COUNT(*) FROM payable_details WHERE transaction_id IS NULL AND deleted_at IS NULL;
  ```
  结果必须为 `0`，否则需先清理（补建交易回填 / 软删除 / 归档历史记录）

## 部署中

- 仓库代码已就位
- 前后端镜像构建或拉取成功
- 容器启动成功

## 部署后

- `docker compose ps` 状态正常
- 健康检查通过
- `http://localhost:${WEB_PORT}/api/auth/me` 对匿名请求返回 `401`
- 管理员可以登录
- 如要求备份，本次备份文件已生成

## 收尾

- 关闭 `BOOTSTRAP_ADMIN_ENABLED`
- 记录当前镜像 tag（GitHub Release `v*` 与 GHCR `finance-api` / `finance-web`）
- 确认回滚路径可用
