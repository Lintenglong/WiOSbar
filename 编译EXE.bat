@echo off
echo ================================================
echo   FluidBar - 一键编译生成EXE
echo ================================================
echo.

:: 设置代码页为UTF-8
chcp 65001 >nul 2>&1

:: 检查dotnet
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 dotnet，请先安装 .NET 10 SDK
    echo.
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/10.0
    echo.
    pause
    exit /b 1
)

echo [信息] 检测到 .NET SDK
dotnet --version
echo.

:: 进入项目目录（脚本所在目录）
cd /d "%~dp0"

echo [步骤 1/3] 正在编译...
echo.

:: 发布（自动包含依赖）
dotnet publish FluidBar.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=false ^
    -o ".\发布版本"

if %errorlevel% neq 0 (
    echo.
    echo [错误] 编译失败！请检查错误信息
    echo.
    pause
    exit /b 1
)

echo.
echo [步骤 2/3] 正在优化...
echo.

:: 删除不必要的pdb文件（如果有）
del ".\发布版本\*.pdb" 2>nul

echo [步骤 3/3] 完成！
echo.
echo ================================================
echo   编译成功！
echo ================================================
echo.
echo EXE文件位置:
echo   %~dp0发布版本\FluidBar.exe
echo.
echo 提示:
echo   1. 直接双击 FluidBar.exe 即可运行
echo   2. 目标电脑需要安装 .NET 10 Runtime
echo   3. 如需独立版本（无需安装.NET），请使用：
echo      dotnet publish -c Release -r win-x64 --self-contained true
echo.
echo 按任意键打开文件夹...
pause >nul

:: 打开文件夹
explorer ".\发布版本"

exit /b 0
