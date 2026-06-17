@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  Quality Diagnostics Center - Production Build
echo ============================================
echo.

set "OUTPUT_DIR=Publish-v2"
set "ZIP_NAME=LabSystem-Production-v2.zip"

echo [1/4] Restoring NuGet packages...
dotnet restore LabSystem.sln
if errorlevel 1 (
    echo FAILED: Package restore failed.
    exit /b 1
)
echo OK
echo.

echo [2/4] Building solution (Release)...
dotnet build LabSystem.sln -c Release --no-restore
if errorlevel 1 (
    echo FAILED: Build failed.
    exit /b 1
)
echo OK
echo.

echo [3/4] Publishing WPF application to %OUTPUT_DIR%/...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
)
dotnet publish LabSystem.UI/LabSystem.UI.csproj -c Release -o "%OUTPUT_DIR%" --no-build
if errorlevel 1 (
    echo FAILED: Publish failed.
    exit /b 1
)

echo Copying appsettings.json to output...
if exist "appsettings.json" (
    copy /y "appsettings.json" "%OUTPUT_DIR%\appsettings.json" >nul
    echo OK
) else (
    echo WARNING: appsettings.json not found at root - skipping.
)
echo OK
echo.

echo [4/4] Creating zip archive %ZIP_NAME%...
if exist "%ZIP_NAME%" del /f "%ZIP_NAME%"
powershell -NoProfile -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%ZIP_NAME%' -Force"
if errorlevel 1 (
    echo FAILED: Zip creation failed.
    exit /b 1
)
echo OK
echo.

echo ============================================
echo  Production build complete!
echo.
echo  Output folder: %OUTPUT_DIR%/
echo  Zip archive:   %ZIP_NAME%
echo.
echo  To verify on target laptop:
echo    1. Copy %ZIP_NAME% to the laptop
echo    2. Extract to a folder
echo    3. Run verify-app.ps1 first
echo    4. Run LabSystem.UI.exe
echo ============================================

endlocal
