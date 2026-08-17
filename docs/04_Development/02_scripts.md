# 脚本索引

状态：Active
适用对象：开发 / 运维
事实源级别：Primary
最后核对日期：2026-03-21
代码依据：[`scripts`](../../scripts)

## 开发相关

- `scripts/Start-Dev.ps1`
- `scripts/Stop-Dev.ps1`
- `scripts/Init-DemoData.ps1`
- 根目录 `start-dev.bat`
- 根目录 `stop-dev.bat`
- 根目录 `init-demo-data.bat`
- 根目录 `clean.bat`
- 根目录 `init-demo-data.sh`

## 生产相关

- `scripts/deploy.sh`
- `scripts/backup-database.sh`
- `scripts/restore-database.sh`
- `scripts/health-check.sh`
- `scripts/restart.sh`
- `scripts/view-logs.sh`

## 测试环境相关

- `scripts/deploy-testing.sh`
- `scripts/backup-database-testing.sh`
- `scripts/restore-database-testing.sh`
- `scripts/health-check-testing.sh`
- `scripts/restart-testing.sh`
- `scripts/view-logs-testing.sh`

## 初始化与校验

- `scripts/init-server.sh`
- `scripts/simulate-deployment.sh`
- `scripts/simulate-testing-deployment.sh`

## 使用建议

- 开发同学优先看本页和 [开发入门](01_onboarding.md)
- 运维同学优先看 [运维脚本与日常维护](../05_Operations/04_scripts_and_maintenance.md)
- 当前没有 `start-dev.sh`，非 Windows 本地开发需要按入门文档手动启动
