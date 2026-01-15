# KerbNoteLite - Cleanup Old Versions Script
# Removes old DLL versions from GameData/KerbNoteLite folder
# Usage: Run this script before installing new version

param(
    [switch]$Auto,
    [string]$KSPPath = ""
)

Write-Host "=== KerbNoteLite Version Cleanup ===" -ForegroundColor Cyan
Write-Host ""

# Function to find KSP installation
function Find-KSPPath {
    $commonPaths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program",
        "C:\Program Files\Steam\steamapps\common\Kerbal Space Program",
        "D:\SteamLibrary\steamapps\common\Kerbal Space Program",
        "E:\SteamLibrary\steamapps\common\Kerbal Space Program",
        "$env:ProgramFiles\Kerbal Space Program",
        "$env:ProgramFiles(x86)\Kerbal Space Program"
    )
    
    foreach ($path in $commonPaths) {
        if (Test-Path "$path\GameData") {
            return $path
        }
    }
    return $null
}

# Get KSP path
if ([string]::IsNullOrEmpty($KSPPath)) {
    $KSPPath = Find-KSPPath
    if ($null -eq $KSPPath) {
        Write-Host "Could not find KSP installation automatically." -ForegroundColor Yellow
        Write-Host "Please enter your KSP installation path:"
        Write-Host "Example: C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
        Write-Host ""
        $KSPPath = Read-Host "KSP Path"
    } else {
        Write-Host "Found KSP at: $KSPPath" -ForegroundColor Green
    }
}

# Validate path
if (-not (Test-Path "$KSPPath\GameData")) {
    Write-Host "ERROR: Invalid KSP path. GameData folder not found." -ForegroundColor Red
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

$modPath = Join-Path $KSPPath "GameData\KerbNoteLite"

# Check if mod is installed
if (-not (Test-Path $modPath)) {
    Write-Host "KerbNoteLite is not installed in this KSP installation." -ForegroundColor Yellow
    Write-Host "Path checked: $modPath"
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

# Find all DLL files matching the pattern
$dllPattern = "KerbNote_V*.dll"
$dllFiles = Get-ChildItem -Path $modPath -Filter $dllPattern -ErrorAction SilentlyContinue

if ($dllFiles.Count -eq 0) {
    Write-Host "No old KerbNote DLL files found." -ForegroundColor Green
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

# Sort by version number (extract from filename)
$dllFiles = $dllFiles | Sort-Object Name

Write-Host "Found $($dllFiles.Count) KerbNote DLL file(s):" -ForegroundColor Yellow
Write-Host ""

foreach ($dll in $dllFiles) {
    $size = [math]::Round($dll.Length / 1KB, 2)
    Write-Host "  - $($dll.Name) ($size KB)" -ForegroundColor White
}

Write-Host ""

# Determine which files to delete
if ($dllFiles.Count -eq 1) {
    Write-Host "Only one version found. Nothing to clean up." -ForegroundColor Green
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

# Keep only the newest version
$filesToDelete = $dllFiles | Select-Object -SkipLast 1
$latestVersion = $dllFiles | Select-Object -Last 1

Write-Host "Latest version: $($latestVersion.Name)" -ForegroundColor Green
Write-Host "Old versions to remove: $($filesToDelete.Count)" -ForegroundColor Yellow
Write-Host ""

# Confirm deletion
if (-not $Auto) {
    Write-Host "Do you want to delete old versions? (Y/N)" -ForegroundColor Cyan
    $confirm = Read-Host
    if ($confirm -ne "Y" -and $confirm -ne "y") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 0
    }
}

# Delete old versions
Write-Host ""
Write-Host "Deleting old versions..." -ForegroundColor Cyan

$deletedCount = 0
foreach ($dll in $filesToDelete) {
    try {
        Remove-Item $dll.FullName -Force
        Write-Host "  ✓ Deleted: $($dll.Name)" -ForegroundColor Green
        $deletedCount++
    } catch {
        Write-Host "  ✗ Failed to delete: $($dll.Name)" -ForegroundColor Red
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Cleanup Complete ===" -ForegroundColor Cyan
Write-Host "Deleted: $deletedCount file(s)" -ForegroundColor Green
Write-Host "Kept: $($latestVersion.Name)" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
