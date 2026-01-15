@echo off
REM KerbNoteLite - Cleanup Old Versions
REM Easy launcher for PowerShell script

echo ============================================
echo   KerbNoteLite - Version Cleanup Tool
echo ============================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: PowerShell not found!
    echo Please install PowerShell or run cleanup_old_versions.ps1 manually.
    pause
    exit /b 1
)

REM Run PowerShell script with execution policy bypass
powershell -ExecutionPolicy Bypass -File "%~dp0cleanup_old_versions.ps1"

REM Check exit code
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Cleanup completed successfully!
) else (
    echo.
    echo Cleanup failed or was cancelled.
)

pause
