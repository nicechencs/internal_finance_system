# 重新部署生产环境（清除数据卷）
# 用法: .\Redeploy.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "重新部署生产环境" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "警告: 此操作将清除所有数据卷！" -ForegroundColor Red
Write-Host ""

$confirmation = Read-Host "确认继续？(y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "操作已取消" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "[1/3] 停止并删除容器..." -ForegroundColor Yellow
Push-Location $projectRoot
docker-compose -f docker-compose.dev.yml down -v
Pop-Location

Write-Host "[2/3] 重新构建镜像..." -ForegroundColor Yellow
Push-Location $projectRoot
docker-compose -f docker-compose.dev.yml build --no-cache
Pop-Location

Write-Host "[3/3] 启动容器..." -ForegroundColor Yellow
Push-Location $projectRoot
docker-compose -f docker-compose.dev.yml up -d
Pop-Location

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "重新部署完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "访问地址:" -ForegroundColor Yellow
Write-Host "  前端页面: http://localhost" -ForegroundColor White
Write-Host "  后端 API: http://localhost:5000" -ForegroundColor White
Write-Host ""
