using System.IO;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 首次运行向导 - 引导用户完成初始设置
/// </summary>
public static class FirstRunWizard
{
    private static readonly string WizardStatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "firstrun.json");

    /// <summary>
    /// 检查是否是首次运行
    /// </summary>
    public static bool IsFirstRun()
    {
        return !File.Exists(WizardStatePath);
    }

    /// <summary>
    /// 标记向导已完成
    /// </summary>
    public static void MarkCompleted()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WizardStatePath)!);

            var state = new FirstRunState
            {
                CompletedAt = DateTime.UtcNow,
                Version = "1.0",
                SkippedSteps = new List<string>()
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(WizardStatePath, json);
        }
        catch { }
    }

    /// <summary>
    /// 跳过向导
    /// </summary>
    public static void Skip()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WizardStatePath)!);

            var state = new FirstRunState
            {
                CompletedAt = DateTime.UtcNow,
                Version = "1.0",
                SkippedSteps = new List<string> { "all" }
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(WizardStatePath, json);
        }
        catch { }
    }

    /// <summary>
    /// 获取推荐的初始设置
    /// </summary>
    public static RecommendedSettings GetRecommendedSettings()
    {
        return new RecommendedSettings
        {
            EnableStartup = true,
            EnableFocusMode = true,
            EnableNotifications = true,
            DefaultTheme = "ios_classic",
            EnableStatistics = true,
            MaxClipboardHistory = 50
        };
    }

    /// <summary>
    /// 应用推荐设置
    /// </summary>
    public static void ApplyRecommendedSettings(FluidBarSettings settings)
    {
        var recommended = GetRecommendedSettings();

        // 启用自启动
        if (recommended.EnableStartup)
        {
            StartupManager.Enable();
        }

        // 设置默认主题
        settings.AlwaysOnTop = true;
        settings.AlwaysVisible = false;
        settings.AutoHideDelayMs = 3000;

        // 应用主题
        var themeManager = ThemeManager.Load();
        themeManager.SwitchToPreset(recommended.DefaultTheme, settings);
    }
}

/// <summary>
/// 首次运行状态
/// </summary>
public sealed class FirstRunState
{
    public DateTime CompletedAt { get; set; }
    public string Version { get; set; } = "";
    public List<string> SkippedSteps { get; set; } = new();
}

/// <summary>
/// 推荐设置
/// </summary>
public sealed class RecommendedSettings
{
    public bool EnableStartup { get; set; } = true;
    public bool EnableFocusMode { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public string DefaultTheme { get; set; } = "ios_classic";
    public bool EnableStatistics { get; set; } = true;
    public int MaxClipboardHistory { get; set; } = 50;
}
