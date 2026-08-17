# 启动财务系统开发环境
# 用法: .\Start-Dev.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$backendPath = Join-Path $projectRoot "backend\FinanceApp.Api"
$frontendPath = Join-Path $projectRoot "frontend"
$demoDataScript = Join-Path $projectRoot "database\seed\seed_demo_data.sql"
$containerName = "finance_db"
$databaseName = "finance_dev"
$defaultAdminUsername = "admin"
$defaultAdminPassword = "DemoOnly_ChangeMe!"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "启动财务系统开发环境" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 0. 清理旧日志文件
Write-Host "[0/5] 清理旧日志文件..." -ForegroundColor Yellow
$logsDir = Join-Path $projectRoot "logs"
if (Test-Path $logsDir) {
    $logFiles = Get-ChildItem -Path $logsDir -Filter "*.log" -ErrorAction SilentlyContinue
    if ($logFiles.Count -gt 0) {
        $logFiles | Remove-Item -Force -ErrorAction SilentlyContinue
        Write-Host "  已清理 $($logFiles.Count) 个日志文件" -ForegroundColor Green
    } else {
        Write-Host "  无旧日志文件" -ForegroundColor Gray
    }
} else {
    Write-Host "  日志目录不存在，跳过" -ForegroundColor Gray
}
$frontendLogFiles = @("dev-server.log", "dev-server.err.log")
foreach ($logFile in $frontendLogFiles) {
    $logPath = Join-Path $frontendPath $logFile
    if (Test-Path $logPath) {
        Remove-Item -Path $logPath -Force -ErrorAction SilentlyContinue
    }
}
Write-Host ""

# 1. 启动数据库容器
Write-Host "[1/4] 启动数据库容器..." -ForegroundColor Yellow
Push-Location $projectRoot
docker-compose -f docker-compose.dev.yml up -d postgres
Pop-Location
Write-Host "  PostgreSQL 容器已启动" -ForegroundColor Green
Write-Host ""

# 等待数据库就绪
Write-Host "[2/5] 等待数据库就绪..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
Write-Host "  数据库已就绪" -ForegroundColor Green
Write-Host ""

# 2. 确保开发默认管理员存在
Write-Host "[3/5] 检查开发默认管理员..." -ForegroundColor Yellow
try {
    $adminCount = docker exec -i $containerName psql -U postgres -d $databaseName -t -c "SELECT COUNT(*) FROM users WHERE username = '$defaultAdminUsername' AND NOT is_deleted;" 2>$null

    if (-not $adminCount -or $adminCount.Trim() -eq "0") {
        Write-Host "  创建开发默认管理员..." -ForegroundColor Gray
        Push-Location $backendPath
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        dotnet run -- auth-cli create-user --username $defaultAdminUsername --password $defaultAdminPassword --full-name "系统管理员" --role Admin | Out-Host
        Pop-Location
        Write-Host "  开发默认管理员已创建" -ForegroundColor Green
    } else {
        Write-Host "  开发默认管理员已存在，跳过创建" -ForegroundColor Gray
    }
} catch {
    Write-Host "  开发默认管理员检查失败：$($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# 3. 尝试导入演示数据（如果尚未导入）
Write-Host "[4/5] 检查演示数据..." -ForegroundColor Yellow
if (Test-Path $demoDataScript) {
    try {
        docker exec -i $containerName psql -U postgres -d $databaseName -c "SELECT COUNT(*) FROM users;" 2>$null | Out-Null
        $userCount = docker exec -i $containerName psql -U postgres -d $databaseName -t -c "SELECT COUNT(*) FROM users;" 2>$null

        if ($userCount -and $userCount.Trim() -eq "1") {
            Write-Host "  导入演示数据..." -ForegroundColor Gray
            Get-Content -Raw -Encoding UTF8 $demoDataScript | docker exec -i $containerName psql -U postgres -d $databaseName
            Write-Host "  演示数据导入成功" -ForegroundColor Green
        } else {
            Write-Host "  演示数据已存在，跳过导入" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  演示数据导入失败（可能已存在）" -ForegroundColor Yellow
    }
} else {
    Write-Host "  未找到演示数据脚本" -ForegroundColor Gray
}
Write-Host ""

# 3. 启动后端 API（新窗口）
Write-Host "[5/5] 启动后端和前端..." -ForegroundColor Yellow

# 启动后端
Start-Process cmd.exe -ArgumentList "/k", "title 后端 API && cd /d `"$backendPath`" && dotnet watch run"
Write-Host "  后端 API 已启动（新窗口）" -ForegroundColor Green
Write-Host "  地址: http://localhost:5187" -ForegroundColor Cyan

# 启动前端
Start-Process cmd.exe -ArgumentList "/k", "title 前端 Dev Server && cd /d `"$frontendPath`" && npm run dev"
Write-Host "  前端 Dev Server 已启动（新窗口）" -ForegroundColor Green
Write-Host "  地址: http://localhost:5173" -ForegroundColor Cyan

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "启动完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "访问地址:" -ForegroundColor Yellow
Write-Host "  前端页面: http://localhost:5173" -ForegroundColor White
Write-Host "  后端 API: http://localhost:5187" -ForegroundColor White
Write-Host "  Swagger:  http://localhost:5187/swagger" -ForegroundColor White
Write-Host ""
Write-Host "开发默认账号: $defaultAdminUsername / $defaultAdminPassword" -ForegroundColor Yellow
Write-Host "仅在账号不存在时自动创建" -ForegroundColor DarkYellow
Write-Host ""
