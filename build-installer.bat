@echo off
setlocal

echo ============================================
echo  Building LabSystem and Installer
echo ============================================

REM Step 1: Run publish script
echo.
echo Running publish.bat to prepare application files...
call publish.bat
if errorlevel 1 (
    echo FAILED: publish.bat encountered an error.
    exit /b 1
)

REM Step 2: Compile Inno Setup Script
echo.
echo Compiling Inno Setup script (installer.iss)...

set "ISCC_PATH=C:\Users\hp\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC_PATH%" (
    echo FAILED: Inno Setup compiler not found at %ISCC_PATH%.
    echo Please install Inno Setup to build the installer.
    exit /b 1
)

"%ISCC_PATH%" installer.iss
if errorlevel 1 (
    echo FAILED: Inno Setup compilation failed.
    exit /b 1
)

echo.
echo ============================================
echo  Installer build complete!
echo  Check the "Output" directory for the setup .exe file.
echo ============================================

endlocal
