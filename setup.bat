@echo off
echo ========================================
echo Claude Code Helper - Setup
echo ========================================
echo.
echo This script will:
echo 1. Build the application
echo 2. Create a shortcut in your startup folder (optional)
echo 3. Run the application
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Building the application...
echo.
dotnet build -c Release

if %errorlevel% neq 0 (
    echo.
    echo Build failed! Please check the errors above.
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.

set EXE_PATH=%~dp0bin\Release\net8.0-windows\ClaudeCodeHelper.exe

if not exist "%EXE_PATH%" (
    echo ERROR: Executable not found at expected location:
    echo %EXE_PATH%
    pause
    exit /b 1
)

echo Executable created at:
echo %EXE_PATH%
echo.

echo Would you like to run the application now? (Y/N)
choice /C YN /N
if errorlevel 2 goto :skip_run
if errorlevel 1 goto :run_app

:run_app
echo.
echo Starting Claude Code Helper...
echo The application will run in the system tray.
echo.
start "" "%EXE_PATH%"
echo.
echo Application started! Look for it in the system tray.
echo.
goto :end

:skip_run
echo.
echo You can manually run the application later:
echo %EXE_PATH%
echo.

:end
echo ========================================
echo Setup complete!
echo ========================================
echo.
echo The application will automatically:
echo - Add itself to Windows startup on first run
echo - Send 'hi' to Claude Code every 5 hours
echo - Display session time and token info in the tray tooltip
echo.
pause
