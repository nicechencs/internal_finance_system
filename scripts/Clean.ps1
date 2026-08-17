# ============================================================
# 项目临时文件清理脚本 (PowerShell)
# 用法：
#   .\clean.ps1              - 交互模式，逐项确认
#   .\clean.ps1 -Mode all    - 清理全部（不含 node_modules）
#   .\clean.ps1 -Mode full   - 清理全部（含 node_modules）
#   .\clean.ps1 -Mode backend
#   .\clean.ps1 -Mode frontend
#   .\clean.ps1 -Mode logs
#   .\clean.ps1 -DryRun      - 仅预览，不实际删除
# ============================================================

param(
    [ValidateSet("interactive", "all", "full", "backend", "frontend", "logs")]
    [string]$Mode = "interactive",
    [switch]$DryRun
)

$ErrorActionPreference = "SilentlyContinue"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$TotalFreed = 0

function Get-DirSize($path) {
    if (Test-Path $path) {
        $size = (Get-ChildItem -Path $path -Recurse -Force -ErrorAction SilentlyContinue |
                 Measure-Object -Property Length -Sum).Sum
        return [math]::Round($size / 1MB, 1)
    }
    return 0
}

function Format-Size($mb) {
    if ($mb -ge 1024) { return "{0:N1} GB" -f ($mb / 1024) }
    return "{0:N1} MB" -f $mb
}

function Remove-Target($path, $label) {
    if (-not (Test-Path $path)) {
        Write-Host "  [跳过] $label 不存在" -ForegroundColor DarkGray
        return 0
    }
    $size = Get-DirSize $path
    if ($DryRun) {
        Write-Host "  [预览] 将删除 $label ($(Format-Size $size))" -ForegroundColor Yellow
    } else {
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [删除] $label ($(Format-Size $size))" -ForegroundColor Green
    }
    return $size
}

function Remove-FilePattern($dir, $pattern, $label) {
    if (-not (Test-Path $dir)) { return 0 }
    $files = Get-ChildItem -Path $dir -Filter $pattern -Recurse -Force -ErrorAction SilentlyContinue
    if ($files.Count -eq 0) {
        Write-Host "  [跳过] $label 无匹配文件" -ForegroundColor DarkGray
        return 0
    }
    $size = [math]::Round(($files | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
    if ($DryRun) {
        Write-Host "  [预览] 将删除 $($files.Count) 个 $label ($(Format-Size $size))" -ForegroundColor Yellow
    } else {
        $files | Remove-Item -Force -ErrorAction SilentlyContinue
        Write-Host "  [删除] $($files.Count) 个 $label ($(Format-Size $size))" -ForegroundColor Green
    }
    return $size
}

function Confirm-Action($message) {
    $reply = Read-Host "[Y/n] $message"
    return ($reply -eq "" -or $reply -eq "Y" -or $reply -eq "y")
}

# ---- 清理动作 ----

function Clean-Backend {
    Write-Host "`n[后端] 清理 bin/obj 编译输出..." -ForegroundColor Cyan
    $freed = 0
    Get-ChildItem -Path "$Root\backend" -Directory | ForEach-Object {
        $freed += Remove-Target "$($_.FullName)\bin" "$($_.Name)\bin"
        $freed += Remove-Target "$($_.FullName)\obj" "$($_.Name)\obj"
    }
    return $freed
}

function Clean-FrontendDist {
    Write-Host "`n[前端] 清理构建产物..." -ForegroundColor Cyan
    $freed = 0
    $freed += Remove-Target "$Root\frontend\dist" "dist"
    $freed += Remove-Target "$Root\frontend\dist-ssr" "dist-ssr"
    return $freed
}

function Clean-ViteCache {
    Write-Host "`n[前端] 清理 Vite 缓存和构建信息..." -ForegroundColor Cyan
    $freed = 0
    $freed += Remove-Target "$Root\frontend\node_modules\.vite" ".vite 缓存"
    $freed += Remove-Target "$Root\frontend\node_modules\.tmp" ".tmp 构建信息"
    $freed += Remove-FilePattern "$Root\frontend" ".eslintcache" ".eslintcache"
    $freed += Remove-FilePattern "$Root\frontend" "*.tsbuildinfo" "tsbuildinfo"
    return $freed
}

function Clean-Logs {
    Write-Host "`n[日志] 清理日志文件..." -ForegroundColor Cyan
    $freed = 0
    $freed += Remove-FilePattern "$Root\logs" "*.log" "日志文件"
    return $freed
}

function Clean-NodeModules {
    Write-Host "`n[前端] 清理 node_modules..." -ForegroundColor Cyan
    $freed = Remove-Target "$Root\frontend\node_modules" "node_modules"
    if (-not $DryRun -and $freed -gt 0) {
        Write-Host "  提示: 运行 'cd frontend && npm install' 重新安装依赖" -ForegroundColor Yellow
    }
    return $freed
}

function Clean-TestArtifacts {
    Write-Host "`n[测试] 清理测试产物..." -ForegroundColor Cyan
    $freed = 0
    $freed += Remove-Target "$Root\frontend\coverage" "前端 coverage"
    $freed += Remove-Target "$Root\frontend\cypress\videos" "Cypress videos"
    $freed += Remove-Target "$Root\frontend\cypress\screenshots" "Cypress screenshots"
    $freed += Remove-Target "$Root\frontend\__screenshots__" "Vitest screenshots"
    $freed += Remove-Target "$Root\backend\TestResults" "后端 TestResults"
    return $freed
}

# ---- 主流程 ----

Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host "  财务系统 - 临时文件清理工具" -ForegroundColor White
if ($DryRun) {
    Write-Host "  [预览模式 - 不会实际删除]" -ForegroundColor Yellow
}
Write-Host "========================================" -ForegroundColor White

switch ($Mode) {
    "interactive" {
        if (Confirm-Action "清理后端编译输出 (bin/obj)?") { $TotalFreed += Clean-Backend }
        if (Confirm-Action "清理前端构建产物 (dist)?") { $TotalFreed += Clean-FrontendDist }
        if (Confirm-Action "清理 Vite 缓存和 TS 构建信息?") { $TotalFreed += Clean-ViteCache }
        if (Confirm-Action "清理测试产物 (coverage/screenshots)?") { $TotalFreed += Clean-TestArtifacts }
        if (Confirm-Action "清理日志文件?") { $TotalFreed += Clean-Logs }
        if (Confirm-Action "清理 node_modules? (需重新 npm install)") { $TotalFreed += Clean-NodeModules }
    }
    "all" {
        $TotalFreed += Clean-Backend
        $TotalFreed += Clean-FrontendDist
        $TotalFreed += Clean-ViteCache
        $TotalFreed += Clean-TestArtifacts
        $TotalFreed += Clean-Logs
    }
    "full" {
        $TotalFreed += Clean-Backend
        $TotalFreed += Clean-FrontendDist
        $TotalFreed += Clean-ViteCache
        $TotalFreed += Clean-TestArtifacts
        $TotalFreed += Clean-Logs
        $TotalFreed += Clean-NodeModules
    }
    "backend" { $TotalFreed += Clean-Backend }
    "frontend" {
        $TotalFreed += Clean-FrontendDist
        $TotalFreed += Clean-ViteCache
    }
    "logs" { $TotalFreed += Clean-Logs }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor White
$verb = if ($DryRun) { "可释放" } else { "已释放" }
Write-Host "  $verb 空间: $(Format-Size $TotalFreed)" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor White
Write-Host ""
