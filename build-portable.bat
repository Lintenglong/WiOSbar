@echo off
chcp 65001 >nul
echo ========================================
echo   FluidBar 编译脚本 - 便携版（独立运行）
echo ========================================
echo.
echo 说明: 此版本包含 .NET 运行时，无需安装
echo       文件较大（约 100-150MB）
echo.

:: 检查dotnet是否安装
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到 .NET SDK，请先安装 .NET 10 SDK
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

echo [1/5] 清理旧的构建文件...
dotnet clean -c Release
if errorlevel 1 (
    echo [警告] 清理过程中出现问题，继续执行...
)

echo.
echo [2/5] 还原依赖包...
dotnet restore
if errorlevel 1 (
    echo [错误] 依赖还原失败
    pause
    exit /b 1
)

echo.
echo [3/5] 编译Release版本...
dotnet build -c Release
if errorlevel 1 (
    echo [错误] 编译失败
    pause
    exit /b 1
)

echo.
echo [4/5] 发布便携版EXE（包含运行时）...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none
if errorlevel 1 (
    echo [错误] 发布失败
    pause
    exit /b 1
)

echo.
echo [5/5] 完成！
echo.
echo ========================================
echo   编译成功！
echo ========================================
echo.
echo 可执行文件位置:
echo   %~dp0bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\FluidBar.exe
echo.
echo 提示: 此版本可直接运行，无需安装 .NET
echo       文件较大，首次启动可能较慢
echo.
pause
