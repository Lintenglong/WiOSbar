# Contributing to FluidBar

感谢你对 FluidBar 的贡献兴趣！本指南将帮助你参与项目开发。

---

## 📋 行为准则

- 尊重所有贡献者
- 保持友好和专业的沟通
- 接受建设性反馈
- 专注于项目最佳利益

---

## 🚀 开始贡献

### 1. Fork 仓库

点击 GitHub 页面右上角的 **Fork** 按钮，创建你的副本。

### 2. 克隆仓库

```bash
git clone https://github.com/YOUR_USERNAME/FluidBar.git
cd FluidBar
git remote add upstream https://github.com/Doulor/FluidBar.git
```

### 3. 创建功能分支

```bash
git checkout -b feature/your-feature-name
```

**分支命名规范**：
- `feature/` - 新功能
- `fix/` - Bug 修复
- `docs/` - 文档更新
- `refactor/` - 代码重构
- `test/` - 测试相关

### 4. 开发与测试

```bash
# 构建项目
dotnet build

# 运行测试
dotnet test
```

### 5. 提交更改

```bash
git add .
git commit -m "feat: add amazing feature

- Detailed description of changes
- Why this change is needed
- Any breaking changes"
```

**提交信息格式**：
- `feat:` - 新功能
- `fix:` - Bug 修复
- `docs:` - 文档
- `style:` - 代码格式
- `refactor:` - 重构
- `test:` - 测试
- `chore:` - 构建/工具

### 6. 推送并创建 PR

```bash
git push origin feature/your-feature-name
```

在 GitHub 上创建 Pull Request。

---

## 📝 代码规范

### C# 代码风格

- 使用 4 空格缩进
- 使用 `PascalCase` 命名公共成员
- 使用 `camelCase` 命名私有字段
- 每行最多 120 个字符
- 使用有意义的变量名

### 注释要求

```csharp
/// <summary>
/// 简要描述功能
/// </summary>
/// <param name="input">参数说明</param>
/// <returns>返回值说明</returns>
public string ProcessData(string input)
{
    // 复杂逻辑需要行内注释
    return result;
}
```

### 提交前检查

- [ ] 代码编译通过
- [ ] 没有引入新警告
- [ ] 更新了相关文档
- [ ] 添加了必要的测试

---

## 🐛 报告 Bug

### 在提交 Issue 前

1. 搜索现有 Issues，避免重复
2. 尝试在最新版本复现问题
3. 收集详细信息（日志、截图、复现步骤）

### Issue 模板

```markdown
**Bug 描述**
简要描述问题

**复现步骤**
1. 打开应用
2. 点击...
3. 看到错误

**预期行为**
应该发生什么

**环境信息**
- Windows 版本: [e.g. Windows 11 23H2]
- .NET 版本: [e.g. 10.0.100]
- FluidBar 版本: [e.g. v1.0.0]

**附加信息**
日志、截图等
```

---

## 💡 功能请求

欢迎提出新功能建议！

### 提交前考虑

- 这个功能是否符合项目目标？
- 是否有更简单的替代方案？
- 是否会影响性能或稳定性？

### 功能请求模板

```markdown
**功能描述**
我想添加...

**使用场景**
这个功能可以解决...

**建议实现**
我认为可以这样实现...
```

---

## 📚 开发资源

### 项目结构

```
FluidBar/
├── Monitors/          # 系统监控器
├── Plugins/           # 插件系统
├── Utils/             # 工具类
└── docs/              # 文档
```

### 核心概念

- **EventBus** - 事件总线，解耦数据源与 UI
- **IslandEvent** - 事件数据模型
- **ISystemMonitor** - 监控器接口
- **IIslandPlugin** - 插件接口

### 调试技巧

```csharp
// 使用 Logger 记录日志
Logger.Info("Something happened", "SourceName");
Logger.Error("Error occurred", "SourceName", ex);

// 查看日志
// %AppData%\FluidBar\logs\
```

---

## ❓ 常见问题

### Q: 如何添加新的监控器？

参考 `Monitors/CpuMonitor.cs` 模板，实现 `ISystemMonitor` 接口并在 `App.xaml.cs` 中注册。

### Q: 如何添加新的歌词来源？

参考 `Plugins/Media/NeteaseLyricsProvider.cs`，实现 `ILyricsProvider` 接口。

### Q: 提交 PR 需要签名 CLA 吗？

目前不需要，但未来可能需要。

---

## 📞 获取帮助

- **GitHub Discussions** - 提问和讨论
- **GitHub Issues** - 报告 Bug 和功能请求
- **Email** - doulor@example.com

---

感谢你的贡献！🎉
