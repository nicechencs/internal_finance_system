@echo off
chcp 65001 >nul

:: 解析参数
set "MODE=%~1"
set "DRYRUN="

:: 检查是否有 -DryRun 参数
:parse_args
if "%~1"=="" goto :run_script
if /i "%~1"=="-DryRun" set "DRYRUN=-DryRun"
shift
goto :parse_args

:run_script
:: 构建 PowerShell 命令
if "%MODE%"=="" (
    PowerShell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Clean.ps1" %DRYRUN%
) else (
    PowerShell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Clean.ps1" -Mode %MODE% %DRYRUN%
)
pause
