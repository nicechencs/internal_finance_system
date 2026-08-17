# 脚本说明

脚本的详细口径以 [`docs/04_Development/02_scripts.md`](../docs/04_Development/02_scripts.md) 和 [`docs/05_Operations/04_scripts_and_maintenance.md`](../docs/05_Operations/04_scripts_and_maintenance.md) 为准；本页只保留快速索引，避免和主文档重复维护。

## 根目录入口

- `start-dev.bat`：Windows 一键启动开发环境
- `stop-dev.bat`：停止开发环境
- `init-demo-data.bat`：导入演示数据
- `start.bat` / `start.sh`：启动生产容器
- `redeploy.bat`：重新部署生产环境
- `clean.bat`：清理临时文件

## PowerShell 脚本

- `Start-Dev.ps1`
- `Stop-Dev.ps1`
- `Init-DemoData.ps1`
- `Start-Production.ps1`
- `Redeploy.ps1`
- `Clean.ps1`

## Shell 运维脚本

- `deploy.sh`
- `backup-database.sh`
- `restore-database.sh`
- `health-check.sh`
- `restart.sh`
- `view-logs.sh`
- `deploy-testing.sh`
- `backup-database-testing.sh`
- `restore-database-testing.sh`
- `health-check-testing.sh`
- `restart-testing.sh`
- `view-logs-testing.sh`

## 说明

- 当前没有 `start-dev.sh`；非 Windows 本地开发请参考开发入门文档手动启动。
- 演示数据种子脚本位于 `database/seed/seed_demo_data.sql`。
