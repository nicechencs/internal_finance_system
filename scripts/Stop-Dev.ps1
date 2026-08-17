# 停止财务系统开发环境
# 用法: .\Stop-Dev.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "SilentlyContinue"

# 辅助函数：通过 taskkill /T 终止进程树（杀掉进程及其所有子进程）
function Stop-ProcessTree {
    param([int]$ProcessId)
    & taskkill /F /T /PID $ProcessId 2>$null | Out-Null
}

# 辅助函数：通过 WMI 获取进程命令行（兼容 PowerShell 5.1）
function Get-ProcessCommandLine {
    param([int]$ProcessId)
    $wmiProc = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
    if ($wmiProc) { return $wmiProc.CommandLine }
    return $null
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "停止财务系统开发环境" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 停止后端 API
Write-Host "[1/3] 停止后端 API..." -ForegroundColor Yellow

# 关闭标题为"后端 API"的 CMD 窗口（使用 taskkill /T 终止整个进程树）
$backendWindows = Get-Process | Where-Object { $_.MainWindowTitle -eq "后端 API" }
foreach ($proc in $backendWindows) {
    Stop-ProcessTree -ProcessId $proc.Id
    Write-Host "  已关闭后端 API 窗口及子进程 (PID: $($proc.Id))" -ForegroundColor Gray
}

# 使用 WMI 查找残留的 dotnet 进程（兼容 PowerShell 5.1）
$dotnetProcesses = Get-Process -Name dotnet -ErrorAction SilentlyContinue
$killedAny = $false
foreach ($proc in $dotnetProcesses) {
    $cmdLine = Get-ProcessCommandLine -ProcessId $proc.Id
    if ($cmdLine -and (
        ($cmdLine -like "*FinanceApp.Api*") -or
        (($cmdLine -like "*watch*") -and ($cmdLine -like "*finance_app*"))
    )) {
        Write-Host "  终止残留 dotnet 进程 (PID: $($proc.Id))" -ForegroundColor Gray
        Stop-ProcessTree -ProcessId $proc.Id
        $killedAny = $true
    }
}

# 兜底：检查端口 5187 上是否还有进程占用
$portCheck = & netstat -ano 2>$null | Select-String ":5187\s" | ForEach-Object {
    if ($_ -match '\s(\d+)\s*$') { [int]$Matches[1] }
} | Where-Object { $_ -ne 0 } | Select-Object -Unique
foreach ($pid in $portCheck) {
    $procName = (Get-Process -Id $pid -ErrorAction SilentlyContinue).ProcessName
    Write-Host "  终止占用端口 5187 的进程: $procName (PID: $pid)" -ForegroundColor Gray
    Stop-ProcessTree -ProcessId $pid
    $killedAny = $true
}

# 清理 dotnet watch 产生的 MSBuild 节点和 Roslyn 编译服务器（它们会锁住 DLL）
$dotnetProcesses2 = Get-Process -Name dotnet -ErrorAction SilentlyContinue
foreach ($proc in $dotnetProcesses2) {
    $cmdLine = Get-ProcessCommandLine -ProcessId $proc.Id
    if ($cmdLine -and (
        ($cmdLine -like "*MSBuild.dll*/nodemode:1*") -or
        ($cmdLine -like "*VBCSCompiler.dll*") -or
        ($cmdLine -like "*vstest.console.dll*")
    )) {
        Write-Host "  终止 MSBuild/编译器常驻进程 (PID: $($proc.Id))" -ForegroundColor Gray
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        $killedAny = $true
    }
}

if ($killedAny -or $backendWindows) {
    Write-Host "  后端 API 已停止" -ForegroundColor Green
} else {
    Write-Host "  未发现运行中的后端进程" -ForegroundColor Gray
}

Write-Host ""

# 2. 停止前端 Dev Server
Write-Host "[2/3] 停止前端 Dev Server..." -ForegroundColor Yellow

# 关闭标题为"前端 Dev Server"的 CMD 窗口（使用 taskkill /T 终止整个进程树）
$frontendWindows = Get-Process | Where-Object { $_.MainWindowTitle -eq "前端 Dev Server" }
foreach ($proc in $frontendWindows) {
    Stop-ProcessTree -ProcessId $proc.Id
    Write-Host "  已关闭前端 Dev Server 窗口及子进程 (PID: $($proc.Id))" -ForegroundColor Gray
}

# 使用 WMI 查找残留的 node 进程（兼容 PowerShell 5.1）
$nodeProcesses = Get-Process -Name node -ErrorAction SilentlyContinue
$killedAnyNode = $false
foreach ($proc in $nodeProcesses) {
    $cmdLine = Get-ProcessCommandLine -ProcessId $proc.Id
    if ($cmdLine -and (
        ($cmdLine -like "*finance_app*frontend*") -or
        (($cmdLine -like "*vite*") -and ($cmdLine -like "*finance_app*"))
    )) {
        Write-Host "  终止残留 node 进程 (PID: $($proc.Id))" -ForegroundColor Gray
        Stop-ProcessTree -ProcessId $proc.Id
        $killedAnyNode = $true
    }
}

# 兜底：检查端口 5173 上是否还有进程占用
$portCheckFe = & netstat -ano 2>$null | Select-String ":5173\s" | ForEach-Object {
    if ($_ -match '\s(\d+)\s*$') { [int]$Matches[1] }
} | Where-Object { $_ -ne 0 } | Select-Object -Unique
foreach ($pid in $portCheckFe) {
    $procName = (Get-Process -Id $pid -ErrorAction SilentlyContinue).ProcessName
    Write-Host "  终止占用端口 5173 的进程: $procName (PID: $pid)" -ForegroundColor Gray
    Stop-ProcessTree -ProcessId $pid
    $killedAnyNode = $true
}

if ($killedAnyNode -or $frontendWindows) {
    Write-Host "  前端 Dev Server 已停止" -ForegroundColor Green
} else {
    Write-Host "  未发现运行中的前端进程" -ForegroundColor Gray
}

Write-Host ""

# 3. 停止数据库容器
# Write-Host "[3/3] 停止数据库容器..." -ForegroundColor Yellow
# Push-Location $PSScriptRoot\..
# docker-compose -f docker-compose.dev.yml stop postgres
# Pop-Location

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "停止完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
