# 🌊 WiOSbar

> Windows 上的灵动岛体验 — 将 macOS 动态岛风格的通知和信息显示带到 Windows 桌面

[![Build Status](https://github.com/Lintenglong/WiOSbar/actions/workflows/build.yml/badge.svg)](https://github.com/Lintenglong/WiOSbar/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

WiOSbar 是一个轻量级的 Windows 桌面应用，使用 WPF 构建，在屏幕顶部以优雅的药丸形状（Pill）浮窗实时展示系统状态、媒体播放、剪贴板内容和应用通知。它的设计灵感来源于 iOS 的 Dynamic Island，通过玻璃拟态（Glassmorphism）、弹簧动画和渐变光效，为 Windows 用户带来精致的交互体验。

本项目基于 [Doulor/FluidBar](https://github.com/Doulor/FluidBar) 修改和维护。

---

## ✨ 功能特性

### 🎵 媒体播放
- **4 源歌词支持**：酷狗音乐、网易云音乐、QQ音乐、Spotify
- 浏览器媒体检测（YouTube、Bilibili、Netflix 等 10+ 站点）
- 实时音频波形动画
- 播放控制（播放/暂停/上一曲/下一曲）
- 专辑封面显示

### 📊 系统监控（21 个）
**基础监控**：音量、亮度、电池、时钟、输入法、锁定键、网络、USB、蓝牙、通知

**增强监控**：
- CPU/内存/磁盘使用率
- 网络速度监控
- 天气信息（需配置 API Key）
- 打印任务状态
- VPN 连接状态
- Agent 状态（Claude Code/Codex）
- 蓝牙设备电量
- 系统温度
- 磁盘健康（SMART）

### 🎨 智能体验
- **事件聚合**：iOS 式智能防打扰
- **专注模式**：全屏/游戏/视频自动隐藏
- **6 种主题**：iOS 经典、Material You、Neon 霓虹、Minimal 极简、Dark Pro、Sunset 晚霞
- **4 种语言**：简体中文、English、日本語、한국어
- **无障碍支持**：高对比度模式、屏幕阅读器

### 🛠️ 工具功能
- 使用统计与洞察报告
- 剪贴板历史持久化（支持文本/图片/文件/URL）
- 数据导出（JSON/CSV）
- 设置备份恢复
- 开机自启动
- 全局快捷键
- 首次运行向导

### 🛡️ 稳定性
- 三级崩溃防护
- LRU 缓存系统
- 性能实时监控
- 结构化日志
- 配置自动验证

---

## 🚀 快速开始

### 下载预编译版本

从 [Releases](https://github.com/Lintenglong/WiOSbar/releases) 页面下载最新版本：

| 版本 | 文件大小 | 适用场景 |
|------|----------|----------|
| **WiOSbar-Standard** | ~5-10 MB | 已安装 .NET 10 的用户 |
| **WiOSbar-Portable** | ~100-150 MB | 无需安装，直接运行 |

### 从源码构建

#### 前置要求
- Windows 10 (19041+) 或 Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

#### 编译步骤

```powershell
# 克隆仓库
git clone https://github.com/Lintenglong/WiOSbar.git
cd WiOSbar

# 标准版本（需安装 .NET 10）
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# 便携版本（独立运行）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

编译后的 EXE 文件位于：
```
bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\WiOSbar.exe
```

---

## 🎮 使用指南

### 基本操作
- **显示/隐藏**：按住 `Alt` 键临时隐藏灵动岛
- **打开设置**：按住 `Ctrl+Alt` 点击灵动岛
- **快捷键**：
  - `Ctrl+Alt+H` - 隐藏/显示
  - `Ctrl+Alt+M` - 切换到媒体
  - `Ctrl+Alt+C` - 剪贴板历史
  - `Ctrl+Alt+S` - 打开设置

### 配置天气（可选）
创建配置文件：`%AppData%\WiOSbar\weather.json`
```json
{
  "Provider": "openweathermap",
  "ApiKey": "your_api_key_here",
  "City": "Shanghai"
}
```

获取 API Key：
- [OpenWeatherMap](https://openweathermap.org/api)（免费）
- [和风天气](https://dev.qweather.com/)（免费额度）

### 启用自启动
在设置中开启，或运行：
```powershell
# 注册表方式
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "WiOSbar" /t REG_SZ /d "\"C:\Path\To\WiOSbar.exe\"" /f
```

---

## 🏗️ 架构概览

```
WiOSbar/
├── App.xaml/cs              # 应用入口 + 崩溃恢复
├── MainWindow.xaml/cs       # 灵动岛主窗口 + 事件处理
├── SettingsWindow.xaml/cs   # 设置界面（4 标签页）
├── Settings.cs              # 配置模型 + JSON 持久化
├── EventSystem.cs           # EventBus + IslandEvent 核心
├── EventAggregationPolicy.cs # 事件聚合策略
│
├── Monitors/                # 21 个系统监控器
│   ├── CpuMonitor.cs
│   ├── MemoryMonitor.cs
│   ├── NetworkSpeedMonitor.cs
│   ├── WeatherMonitor.cs
│   └── ...
│
├── Plugins/                 # 4 个内置插件
│   ├── Clipboard/           # 剪贴板历史
│   ├── Media/               # 媒体播放 + 4 源歌词
│   ├── AgentStatus/         # Agent 状态监控
│   └── Notifications/       # Windows 通知
│
├── Utils/
│   └── LRUCache.cs          # LRU 缓存实现
│
├── Localization/            # 多语言支持
│   ├── Strings.resx         # 中文
│   └── Strings.en-US.resx   # 英文
│
└── .github/workflows/       # GitHub Actions 自动编译
    └── build.yml
```

### 核心设计模式
- **Event Bus / Pub-Sub** — 数据源与 UI 完全解耦
- **Strategy Pattern** — 动画策略、显示策略、媒体选择策略
- **Record Types** — 不可变数据模型
- **Spring Physics** — 自定义弹簧物理动画

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 开发指南
1. Fork 仓库
2. 创建功能分支：`git checkout -b feature/amazing-feature`
3. 提交更改：`git commit -m 'Add amazing feature'`
4. 推送分支：`git push origin feature/amazing-feature`
5. 创建 Pull Request

### 插件开发
参考 [Plugins/Template/](Plugins/Template/) 目录下的模板。

---

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

---

## 🙏 致谢

- 上游项目：[Doulor/FluidBar](https://github.com/Doulor/FluidBar)
- 灵感来源：[iOS Dynamic Island](https://developer.apple.com/design/human-interface-guidelines/dynamic-island)
- 歌词 API：酷狗、网易云、QQ音乐、Spotify、lyrics.ovh
- 天气 API：OpenWeatherMap、和风天气

---

## 📞 支持

- **项目主页**：https://github.com/Lintenglong/WiOSbar
- **问题反馈**：https://github.com/Lintenglong/WiOSbar/issues
- **崩溃日志**：`%AppData%\WiOSbar\logs\`

---

<p align="center">
  <sub>Built with WPF & .NET 10 · Designed for Windows</sub>
</p>

<p align="center">
  <a href="https://github.com/Lintenglong/WiOSbar/stargazers">⭐ Star us on GitHub</a>
</p>
