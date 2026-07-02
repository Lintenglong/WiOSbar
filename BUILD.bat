@echo off
echo ================================================
echo   FluidBar Build Script
echo ================================================
echo.

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] dotnet not found. Please install .NET 10 SDK
    echo Download: https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

cd /d "%~dp0"

echo [1/3] Building Release version...
echo.

dotnet publish FluidBar.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ".\Release"

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo [2/3] Cleaning up...
del ".\Release\*.pdb" 2>nul

echo [3/3] Done!
echo.
echo ================================================
echo   Build successful!
echo ================================================
echo.
echo EXE location:
echo   %~dp0Release\FluidBar.exe
echo.
echo Note: Target PC needs .NET 10 Runtime installed
echo.
pause

explorer ".\Release"

exit /b 0
