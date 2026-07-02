@echo off
chcp 65001 >nul
echo ================================================
echo   WiOSbar - 推送脚本
echo ================================================
echo.

:: 检查git是否安装
where git >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 git，请先安装 Git for Windows
    echo 下载地址: https://git-scm.com/download/win
    pause
    exit /b 1
)

:: 进入项目目录
cd /d "%~dp0"

:: 检查是否已初始化git
if not exist ".git" (
    echo [信息] 初始化 Git 仓库...
    git init
)

:: 获取GitHub用户名
set /p GITHUB_USER="请输入你的 GitHub 用户名: "

if "%GITHUB_USER%"=="" (
    echo [错误] 用户名不能为空
    pause
    exit /b 1
)

:: 检查是否已添加远程仓库
git remote -v | findstr "origin" >nul 2>&1
if %errorlevel% neq 0 (
    echo [信息] 添加远程仓库...
    git remote add origin https://github.com/%GITHUB_USER%/WiOSbar.git
) else (
    echo [信息] 更新远程仓库地址...
    git remote set-url origin https://github.com/%GITHUB_USER%/WiOSbar.git
)

echo.
echo [步骤 1/4] 添加文件...
git add .

echo.
echo [步骤 2/4] 提交更改...
git commit -m "Initial commit - WiOSbar v1.0"

echo.
echo [步骤 3/4] 推送代码...
echo.
echo 注意: 如果是首次推送，需要输入 GitHub 用户名和密码/Token
echo.
echo 如果提示输入密码，建议使用 Personal Access Token 而不是密码
echo 生成 Token: https://github.com/settings/tokens
echo.
pause

git branch -M main
git push -u origin main

if %errorlevel% neq 0 (
    echo.
    echo [错误] 推送失败！
    echo.
    echo 可能的原因:
    echo   1. GitHub 仓库不存在，请先创建: https://github.com/new
    echo   2. 用户名或密码错误
    echo   3. 没有推送权限
    echo.
    echo 解决方法:
    echo   1. 确认仓库名是 WiOSbar
    echo   2. 使用 Personal Access Token 作为密码
    echo   3. 确保仓库是 Public 或你有写入权限
    echo.
    pause
    exit /b 1
)

echo.
echo [步骤 4/4] 完成！
echo.
echo ================================================
echo   推送成功！
echo ================================================
echo.
echo 下一步:
echo   1. 访问 https://github.com/%GITHUB_USER%/WiOSbar
echo   2. 点击 Actions 标签查看编译状态
echo   3. 编译完成后在 Artifacts 下载 EXE
echo.
pause

exit /b 0
