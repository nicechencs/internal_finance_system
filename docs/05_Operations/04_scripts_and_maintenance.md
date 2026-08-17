# 运维脚本与日常维护

状态：Active
适用对象：运维 / 开发
事实源级别：Primary
最后核对日期：2026-08-16
代码依据：[`scripts`](../../scripts), [`.github/workflows/release-production.yml`](../../.github/workflows/release-production.yml)

## 生产脚本

- `scripts/deploy.sh`
- `scripts/backup-database.sh`
- `scripts/restore-database.sh`
- `scripts/health-check.sh`
- `scripts/restart.sh`
- `scripts/view-logs.sh`

## 测试环境脚本

- `scripts/deploy-testing.sh`
- `scripts/backup-database-testing.sh`
- `scripts/restore-database-testing.sh`
- `scripts/health-check-testing.sh`
- `scripts/restart-testing.sh`
- `scripts/view-logs-testing.sh`

## 自动发布

- 推送到 `production` 或手动运行 GitHub Actions `Release Production`
- 本地资产校验：`scripts/simulate-deployment.sh`（会检查发布工作流文件是否存在）

## 日常维护动作

- 查看日志
- 重启服务
- 数据库备份
- 数据库恢复
- 健康检查
- 本地部署资产模拟校验

## 注意

- 旧版 `scripts/README.md` 已缩减为脚本入口说明。
- 具体参数和行为以脚本源码为准。
