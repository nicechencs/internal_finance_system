# 导入演示数据到数据库
# 用法: .\Init-DemoData.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$demoDataScript = Join-Path $projectRoot "database\seed\seed_demo_data.sql"
$containerName = "finance_db"
$databaseName = "finance_dev"
$defaultAdminUsername = "admin"
$defaultAdminPassword = "DemoOnly_ChangeMe!"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "导入演示数据" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查脚本文件是否存在
if (-not (Test-Path $demoDataScript)) {
    Write-Host "错误: 未找到演示数据脚本文件" -ForegroundColor Red
    Write-Host "路径: $demoDataScript" -ForegroundColor Gray
    exit 1
}

# 检查容器是否运行
$containerRunning = docker ps --filter "name=$containerName" --format "{{.Names}}" 2>$null
if (-not $containerRunning) {
    Write-Host "错误: 数据库容器未运行" -ForegroundColor Red
    Write-Host "请先运行 start-dev.bat，或手动执行 docker-compose -f docker-compose.dev.yml up -d postgres" -ForegroundColor Yellow
    exit 1
}

Write-Host "正在导入演示数据..." -ForegroundColor Yellow
try {
    Get-Content -Raw -Encoding UTF8 $demoDataScript | docker exec -i $containerName psql -U postgres -d $databaseName
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "演示数据导入成功！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "默认账号: $defaultAdminUsername / $defaultAdminPassword" -ForegroundColor Yellow
} catch {
    Write-Host ""
    Write-Host "演示数据导入失败: $_" -ForegroundColor Red
    exit 1
}
