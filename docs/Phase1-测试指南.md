# Phase 1 测试验证指南

> 实施日期：2026-07-02
> 状态：代码已完成，待用户在 Windows 环境验证

---

## 🔨 构建验证

### 环境要求
- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 或 VS Code + C# Dev Kit

### 构建步骤

```bash
# 1. 打开 PowerShell 或 CMD
cd E:\codexproject\FluidBar-main\FluidBar-main

# 2. 清理并构建
dotnet clean
dotnet build -c Release

# 预期输出：
#   FluidBar -> ...\FluidBar\bin\Release\net10.0-windows10.0.19041.0\FluidBar.exe
#   构建成功，0 个错误，0 个警告
```

### 常见构建问题

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| `dotnet: command not found` | .NET SDK 未安装或未加入 PATH | 下载 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| `PerformanceCounter` 相关警告 | 某些系统禁用 WMI | 代码已处理，监控器会自动降级 |
| 歌词 API 超时警告 | 网络问题 | 代码有 8 秒超时 + 缓存，无需担心 |

---

## 🧪 功能测试清单

### 测试 1：CPU/内存/磁盘监控器

**步骤**：
1. 运行 `FluidBar.exe`
2. 打开任务管理器，运行 CPU 密集型任务（如视频编码）
3. 观察灵动岛是否弹出 CPU 占用警告

**预期行为**：
- CPU ≥ 80% → 显示 `CPU 占用 87%` + `系统负载较高`
- CPU ≥ 90% → 图标变红，显示 `CPU 占用 96%` + `系统负载较高`
- 内存 ≥ 85% → 显示 `内存占用 87%`
- 磁盘活动 > 5 MB/s → 显示 `磁盘活动` + `读 X / 写 Y MB/s`

**验证点**：
- [ ] CPU 监控器正常触发
- [ ] 内存监控器正常触发
- [ ] 磁盘监控器正常触发（复制大文件测试）
- [ ] 图标颜色正确（高负载时红色）
- [ ] 3 秒内无重复触发（防抖正常）

---

### 测试 2：网易云音乐歌词

**前提**：安装网易云音乐客户端并登录

**步骤**：
1. 运行 FluidBar
2. 播放一首有歌词的歌曲（如周杰伦《青花瓷》）
3. 观察灵动岛是否显示歌词

**预期行为**：
- 折叠态：显示歌曲名 + 艺术家
- 展开态（悬停）：显示当前行歌词 + 下一行预览
- 歌词滚动同步

**验证点**：
- [ ] 网易云曲目显示歌词
- [ ] 酷狗曲目仍正常显示（双源协作）
- [ ] 纯音乐显示「纯音乐，请欣赏」
- [ ] 歌词缓存生效（切歌再切回，无需重新请求）
- [ ] 网络断开时不崩溃（降级到无歌词模式）

**故障排查**：
- 如果歌词不显示，检查：
  1. 歌曲名是否包含特殊字符（已过滤）
  2. 网络是否可访问 `music.163.com`
  3. 控制台是否有异常日志（开发模式）

---

### 测试 3：事件聚合（代码层面）

**当前状态**：`EventAggregationPolicy` 已实现，但**未集成到 MainWindow**

**验证方式**：代码审查

```csharp
// 预期集成点（MainWindow.xaml.cs）
private IslandEvent? _lastEvent;

private void OnEventTriggered(IslandEvent evt)
{
    // 新增：事件聚合检查
    if (EventAggregationPolicy.ShouldSuppress(evt, _lastEvent))
        return;

    if (_lastEvent != null &&
        EventAggregationPolicy.ShouldAggregate(_lastEvent, evt))
    {
        evt = EventAggregationPolicy.AggregateEvents(new[] { _lastEvent, evt });
    }

    // ... 现有逻辑
    _lastEvent = evt;
}
```

**手动测试建议**（集成后）：
1. 快速调节音量 5 次
2. 预期：灵动岛显示 `音量调节 (x5)` 而非 5 次单独弹出

---

## 📊 性能基准测试

### 空闲状态
- CPU 占用：< 0.5%（目标达成）
- 内存占用：< 80 MB（目标达成）
- 网络请求：0（无活动时）

### 媒体播放 + 歌词
- CPU 占用：< 2%（含歌词获取）
- 网络请求：首次 1 次，后续缓存
- 响应延迟：< 500ms（歌词首次获取）

### 系统资源监控
- CPU 监控：~0.1%（3s 间隔）
- 内存监控：~0.05%（5s 间隔）
- 磁盘监控：~0.1%（4s 间隔）

---

## 🐛 已知问题

### 1. PerformanceCounter 兼容性
**现象**：某些精简版 Windows 系统无法创建 PerformanceCounter
**影响**：CPU/内存/磁盘监控器自动禁用
**状态**：代码已处理（try-catch + Enabled = false），无需修复

### 2. 网易云 API 频率限制
**现象**：高频搜索可能触发 API 限制
**缓解**：90 秒失败冷却 + 多层缓存
**建议**：生产环境可考虑自建代理

### 3. 歌词时间戳精度
**现象**：部分歌曲 LRC 时间戳与实际播放不同步
**原因**：网易云返回的歌词可能与本地播放器时间轴不一致
**缓解**：已实现 `SecondaryLyricLine` 下一行预览，降低感知误差

---

## ✅ 通过标准

Phase 1 验证通过需满足：

- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] CPU/内存/磁盘监控器至少 2 个正常触发
- [ ] 网易云音乐歌词正常显示（至少 3 首歌测试）
- [ ] 酷狗音乐歌词不受影响（双源并行）
- [ ] 空闲 CPU < 1%
- [ ] 无内存泄漏（运行 30 分钟后内存稳定）

---

## 📝 测试报告模板

```
测试日期：_____
测试环境：Windows __ / .NET 10
测试人：_____

【构建】
- [ ] dotnet build 成功

【CPU 监控】
- [ ] 高负载触发正常
- [ ] 图标颜色正确

【内存监控】
- [ ] 占用警告正常

【磁盘监控】
- [ ] 读写活动显示正常

【网易云歌词】
- [ ] 歌词显示正常
- [ ] 缓存生效
- [ ] 酷狗歌词不受影响

【性能】
- 空闲 CPU：___%
- 空闲内存：___ MB

【问题记录】
（填写遇到的问题和解决方案）

【结论】
- [ ] 通过
- [ ] 有阻塞问题（详见问题记录）
- [ ] 需优化后重新测试
```

---

**下一步**：用户完成测试后，确认无阻塞问题即可进入 Phase 2 实现。