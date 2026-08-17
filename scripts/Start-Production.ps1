# 启动生产环境（Docker Compose）
# 用法: .\Start-Production.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "启动生产环境" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "正在构建并启动容器..." -ForegroundColor Yellow
Push-Location $projectRoot
docker-compose up -d --build
Pop-Location

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "生产环境启动成功！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "访问地址:" -ForegroundColor Yellow
Write-Host "  前端页面: http://localhost" -ForegroundColor White
Write-Host "  后端 API: http://localhost:5000" -ForegroundColor White
Write-Host ""
