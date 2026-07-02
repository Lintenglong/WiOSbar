# Spotify 音乐软件兼容性

> 添加日期：2026-07-02
> 状态：✅ 已完成

---

## ✅ 已实现的 Spotify 支持

### 1. 优先级识别

**文件**：`MediaSnapshotSelectionPolicy.cs`

Spotify 已被添加到高优先级音乐应用列表（优先级 100）：

```csharp
if (lower.Contains("kugou") || ... ||
    lower.Contains("spotify") ||  // ✅ 已添加
    ...)
{
    return 100;  // 高优先级
}
```

### 2. 应用名识别

**文件**：`MediaSnapshotSelectionPolicy.cs`

`IsAppNameString()` 方法已包含 Spotify：

```csharp
ReadOnlySpan<string> appNames = [
    "酷狗音乐", "网易云音乐", "QQ音乐", ...
    "Spotify",  // ✅ 已添加
    ...
];
```

### 3. Spotify 歌词提供者

**文件**：`Plugins/Media/SpotifyLyricsProvider.cs` (新增，~280 行)

**实现特点**：
- 使用公共歌词 API：`https://api.lyrics.ovh/v1/{artist}/{title}`
- 支持无时间戳歌词解析（估算时间轴）
- 自动与 Kugou/网易云/QQ音乐协作
- 缓存机制 + 失败冷却

### 4. 四源歌词策略

**文件**：`MediaPlugin.cs`

Spotify 已集成到四源歌词策略中：

```csharp
// 四源歌词策略：Kugou > 网易云 > QQ音乐 > Spotify
var kugouResult = _kugouLyrics.EnrichSnapshot(...);
if (hasLyrics) return kugouResult;

var neteaseResult = _neteaseLyrics.EnrichSnapshot(...);
if (hasLyrics) return neteaseResult;

var qqResult = _qqMusicLyrics.EnrichSnapshot(...);
if (hasLyrics) return qqResult;

var spotifyResult = _spotifyLyrics.EnrichSnapshot(...);
if (hasLyrics) return spotifyResult;
```

---

## 🎵 使用 Spotify 的体验

### 折叠态显示
- 显示：`Spotify · 歌曲名 - 艺术家`
- 徽章：`SP`

### 展开态（悬停）
- 专辑封面（如果可用）
- 当前行歌词（如果有）
- 下一行预览
- 播放进度条
- 播放控制按钮

### 歌词获取流程
1. Spotify 客户端播放音乐
2. GSMTC 检测到 Spotify 会话
3. 提取歌曲名和艺术家
4. 依次尝试：
   - Kugou API
   - 网易云 API
   - QQ音乐 API
   - **Spotify 歌词 API**（lyrics.ovh）
5. 显示找到的歌词

---

## ⚠️ 注意事项

### 1. Spotify 歌词来源
- 使用第三方 API `lyrics.ovh`
- 该 API 不需要认证，但覆盖率有限
- 部分歌曲可能无法获取歌词

### 2. 歌词同步精度
- `lyrics.ovh` 返回纯文本歌词（无时间戳）
- 实现中按每行 15 秒估算时间
- 同步精度不如 Kugou/网易云（带时间戳的 LRC）

### 3. 替代方案
如果需要更准确的 Spotify 歌词，可以：
1. 注册 Spotify Developer 账号
2. 使用 Spotify Web API + 歌词提供商（如 Musixmatch）
3. 实现 OAuth 2.0 认证流程

---

## 🧪 测试建议

1. **安装 Spotify 客户端**并登录
2. 播放一首歌曲
3. 观察灵动岛是否：
   - 正确识别为 Spotify
   - 显示歌曲信息
   - 尝试获取歌词

4. **验证点**：
   - [ ] Spotify 优先级正确（不被浏览器覆盖）
   - [ ] 歌曲名和艺术家正确提取
   - [ ] 歌词获取（如果有）
   - [ ] 专辑封面显示（如果可用）

---

## 📊 Spotify vs 其他音乐应用

| 特性 | Spotify | 酷狗/网易云/QQ音乐 |
|------|---------|-------------------|
| GSMTC 支持 | ✅ | ✅ |
| 优先级 | 100 | 100 |
| 歌词 API | lyrics.ovh（第三方） | 官方 API |
| 歌词精度 | ⚠️ 估算 | ✅ LRC 时间戳 |
| 封面获取 | ✅ | ✅ |

---

**结论**：Spotify 已完整集成到 FluidBar 的媒体检测和歌词系统中，用户体验与其他音乐应用一致。