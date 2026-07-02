# FluidBar 功能实现日志 - Phase 2

> 实施日期：2026-07-02
> 目标：事件聚合集成 + QQ音乐歌词 + 天气监控 + 快捷键框架

---

## ✅ 已完成的功能

### 1. 事件聚合策略集成

**文件**：`MainWindow.xaml.cs` (修改)

**实现**：
```csharp
private void ProcessEvent(IslandEvent evt)
{
    // 事件聚合与防打扰策略（Phase 2 新增）
    // 1. 检查是否应该抑制（重复事件、静默期）
    if (EventAggregationPolicy.ShouldSuppress(evt, _lastEvent))
        return;

    // 2. 尝试聚合同类事件
    if (_lastEvent != null && EventAggregationPolicy.ShouldAggregate(_lastEvent, evt))
    {
        evt = EventAggregationPolicy.AggregateEvents(new[] { _lastEvent, evt });
    }

    // ... 现有逻辑
}
```

**效果**：
- 音量快速调节 5 次 → 显示 `音量调节 (x5)` 而非 5 次闪烁
- 午夜 00:00-06:00 非关键事件自动降级
- 3 秒内重复事件自动抑制

---

### 2. QQ音乐歌词提供者

**文件**：`Plugins/Media/QQMusicLyricsProvider.cs` (350 行)

**实现要点**：
- QQ音乐公开 API：
  - 搜索：`c.y.qq.com/soso/fcgi-bin/client_search_cp`
  - 歌词：`c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg`
- 支持 Base64 解码（QQ音乐返回的歌词可能是 Base64）
- 专辑封面 URL 构造：`https://y.qq.com/music/photo_new/T002R300x300M000{albumMid}.jpg`
- 与 Kugou + 网易云三源并行

**三源策略**（MediaPlugin.cs 更新）：
```csharp
// Kugou > 网易云 > QQ音乐
var kugouResult = _kugouLyrics.EnrichSnapshot(snapshot, position);
if (!string.IsNullOrWhiteSpace(kugouResult.LyricLine)) return kugouResult;

var neteaseResult = _neteaseLyrics.EnrichSnapshot(snapshot, position);
if (!string.IsNullOrWhiteSpace(neteaseResult.LyricLine)) return neteaseResult;

var qqResult = _qqMusicLyrics.EnrichSnapshot(snapshot, position);
if (!string.IsNullOrWhiteSpace(qqResult.LyricLine)) return qqResult;
```

---

### 3. 天气监控器

**文件**：`Monitors/WeatherMonitor.cs` (280 行)

**支持的 API**：
1. **OpenWeatherMap**（默认）
   - 免费 Key：https://openweathermap.org/api
   - 端点：`api.openweathermap.org/data/2.5/weather`

2. **和风天气 (QWeather)**
   - 需注册：https://dev.qweather.com/
   - 端点：`devapi.qweather.com/v7/weather/now`

**配置方式**：
```json
// %AppData%\FluidBar\weather.json
{
  "Provider": "openweathermap",
  "ApiKey": "your_api_key_here",
  "City": "Beijing"
}
```

**特性**：
- 默认禁用（`Enabled = false`），需配置后自动启用
- 30 分钟更新一次
- 温度变化 > 3°C 触发事件
- 天气图标：晴/云/雨/雪/雾/雷

**显示示例**：
- `北京 · 晴` + `28°C 体感 31°C`

---

### 4. 快捷键框架

**文件**：`HotkeyManager.cs` (150 行)

**实现**：
- 使用 Win32 `RegisterHotKey` / `UnregisterHotKey`
- 支持修饰键组合：Ctrl / Alt / Shift / Win
- 自动处理 `WM_HOTKEY` 消息

**预定义快捷键**（HotkeyPolicy.cs）：
```csharp
["ToggleVisibility"] = (Ctrl+Alt+H, "临时隐藏/显示灵动岛")
["ShowMedia"]        = (Ctrl+Alt+M, "立即切换到媒体显示")
["ShowClipboard"]    = (Ctrl+Alt+C, "打开剪贴板历史")
["ShowNotifications"]= (Ctrl+Alt+N, "显示最新通知")
["OpenSettings"]     = (Ctrl+Alt+S, "打开设置面板")
```

**集成**（App.xaml.cs）：
```csharp
_hotkeyManager = new HotkeyManager(_mainWindow);
// 实际注册需 MainWindow 暴露相应方法
_hotkeyManager.RegisterHotkey(
    ModifierKeys.Control | ModifierKeys.Alt,
    Key.M,
    () => _mainWindow?.ForceShowMedia());
```

**注意**：当前为框架代码，实际动作需 MainWindow 暴露 API 后启用。

---

## 📝 代码修改清单

### MainWindow.xaml.cs
- `ProcessEvent` 方法新增事件聚合调用

### MediaPlugin.cs
- 新增 `_qqMusicLyrics` 实例
- `EnrichInBackground` 更新为三源策略
- 同曲目补全逻辑更新为三源

### App.xaml.cs
- 新增 `_hotkeyManager` 字段
- `SetupHotkeys()` 方法（框架）
- 注册 `WeatherMonitor`

---

## 🧪 编译验证

```bash
dotnet build -c Release

# 预期：0 错误，0 警告
# 新增文件：
#   - Plugins/Media/QQMusicLyricsProvider.cs
#   - Monitors/WeatherMonitor.cs
#   - HotkeyManager.cs
```

---

## ⚠️ 使用说明

### 天气监控配置

1. 获取 API Key：
   - OpenWeatherMap: https://openweathermap.org/api (免费)
   - 和风天气: https://dev.qweather.com/ (免费额度)

2. 创建配置文件：
   ```bash
   # 创建目录
   mkdir "%AppData%\FluidBar"

   # 编辑 weather.json
   notepad "%AppData%\FluidBar\weather.json"
   ```

3. 填入配置：
   ```json
   {
     "Provider": "openweathermap",
     "ApiKey": "your_key_here",
     "City": "Shanghai"
   }
   ```

4. 重启 FluidBar，天气监控自动启用

### 快捷键启用

当前快捷键框架已就绪，但动作方法需 MainWindow 实现：

```csharp
// MainWindow.xaml.cs 需添加
public void ForceShowMedia() { /* 实现 */ }
public void ShowClipboardHistory() { /* 实现 */ }
```

---

## 📊 性能影响

| 模块 | 资源占用 | 备注 |
|------|----------|------|
| 事件聚合 | 0（纯计算） | 无后台任务 |
| QQ音乐歌词 | 网络请求（首次） | 90s 缓存 |
| 天气监控 | 网络请求（30min） | 默认禁用 |
| 快捷键 | 0 | 仅消息钩子 |

---

## 🎯 Phase 2 完成度

| 功能 | 状态 | 说明 |
|------|------|------|
| 事件聚合集成 | ✅ 完成 | 立即生效 |
| QQ音乐歌词 | ✅ 完成 | 三源并行 |
| 天气监控器 | ✅ 完成 | 需配置启用 |
| 快捷键框架 | ✅ 完成 | 需 MainWindow API |

---

## 🚀 下一步（Phase 3）

根据完善建议，Phase 3 建议：

1. **悬停卡片内容增强**
   - 媒体：完整歌词面板、进度条拖拽
   - 通知：快速操作按钮
   - 系统状态：历史趋势图

2. **主题包系统**
   - 预设：iOS 经典 / Material You / Neon / Minimal
   - 动态壁纸适配

3. **崩溃恢复机制**
   - `AppDomain.CurrentDomain.UnhandledException` 捕获
   - 自动重启 + 状态恢复

4. **性能深度优化**
   - LRU 缓存（专辑封面）
   - 智能轮询降频（无活动时 2s → 5s）

---

**实施人**：Claude (Anthropic)
**审核状态**：代码已完成，待用户测试验证
**相关文档**：`docs/功能完善建议.md`, `docs/实现日志-Phase1.md`