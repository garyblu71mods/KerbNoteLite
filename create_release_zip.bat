@echo off
REM KerbNoteLite - Create Release ZIP
REM Run this AFTER adding textures to GameData/KerbNoteLite/Textures/

echo ============================================
echo   KerbNoteLite v1.3.1 - Create Release ZIP
echo ============================================
echo.

REM Check if textures exist
if not exist "GameData\KerbNoteLite\Textures\Background_window.png" (
    echo ERROR: Textures not found!
    echo.
    echo Please add texture files to GameData\KerbNoteLite\Textures\
    echo See TEXTURE_CHECKLIST.md for required files.
    echo.
    pause
    exit /b 1
)

echo Textures found! Creating release ZIP...
echo.

REM Remove old ZIP if exists
if exist "KerbNoteLite-v1.3.1.zip" (
    del "KerbNoteLite-v1.3.1.zip"
    echo Old ZIP removed.
)

REM Create ZIP using PowerShell
powershell -Command "Compress-Archive -Path 'GameData\*' -DestinationPath 'KerbNoteLite-v1.3.1.zip' -Force"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo   SUCCESS! Release ZIP created:
    echo   KerbNoteLite-v1.3.1.zip
    echo ============================================
    echo.
    echo Next steps:
    echo 1. Test the ZIP by extracting to a test KSP folder
    echo 2. Upload to GitHub release
    echo 3. Update CKAN if needed
    echo.
) else (
    echo.
    echo ERROR: Failed to create ZIP file!
    echo.
)

pause
