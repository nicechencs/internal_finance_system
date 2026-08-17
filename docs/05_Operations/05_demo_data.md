# 演示数据说明

状态：Active
适用对象：开发 / 测试
事实源级别：Primary
最后核对日期：2026-03-21
代码依据：[`database/seed/seed_demo_data.sql`](../../database/seed/seed_demo_data.sql), [`scripts/Init-DemoData.ps1`](../../scripts/Init-DemoData.ps1), [`scripts/Start-Dev.ps1`](../../scripts/Start-Dev.ps1), [`init-demo-data.sh`](../../init-demo-data.sh)

## 关键提示

- 演示数据仅用于开发和测试环境。
- 生产环境不要导入演示数据。
- 开发环境演示账号是 `admin / DemoOnly_ChangeMe!`（不能用于生产）。
- 种子数据全部为虚构演示记录（`example.com` 邮箱、明显假账号/税号），不含真实公司或个人信息。

## 数据内容

- 账户
- 客户
- 供应商
- 人员

## 使用方式

- 可单独执行演示数据导入脚本
- 使用开发启动脚本时也会检查并导入演示数据

### 可用入口

- Windows：`init-demo-data.bat`
- Linux / macOS：`./init-demo-data.sh`
- Windows 一键开发启动：`start-dev.bat`

## 相关资产

- 种子脚本：[`database/seed/seed_demo_data.sql`](../../database/seed/seed_demo_data.sql)
- PowerShell 导入脚本：[`scripts/Init-DemoData.ps1`](../../scripts/Init-DemoData.ps1)
- Shell 导入脚本：[`init-demo-data.sh`](../../init-demo-data.sh)
