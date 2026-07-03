using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace FluidBar;

/// <summary>
/// 主题包管理器 - 支持预设主题和动态壁纸适配
/// </summary>
public sealed class ThemeManager
{
    private static readonly string ThemeConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "theme.json");

    /// <summary>
    /// 预设主题列表
    /// </summary>
    public static readonly List<ThemePreset> BuiltInPresets = new()
    {
        new ThemePreset
        {
            Name = "iOS 经典",
            Id = "ios_classic",
            BackgroundColor = "#F4000000",
            AccentColor = "#0A84FF",
            BackgroundOpacity = 0.75,
            RimColor = "#41FFFFFF",
            FontFamily = "Segoe UI",
            Description = "默认的 iOS 灵动岛风格"
        },
        new ThemePreset
        {
            Name = "Material You",
            Id = "material_you",
            BackgroundColor = "#E8F3E8F3",
            AccentColor = "#6750A4",
            BackgroundOpacity = 0.85,
            RimColor = "#33FFFFFF",
            FontFamily = "Segoe UI",
            Description = "Android 12+ Material You 风格"
        },
        new ThemePreset
        {
            Name = "Neon 霓虹",
            Id = "neon",
            BackgroundColor = "#E6000000",
            AccentColor = "#FF00FF",
            BackgroundOpacity = 0.80,
            RimColor = "#80FF00FF",
            FontFamily = "Consolas",
            Description = "赛博朋克霓虹风格"
        },
        new ThemePreset
        {
            Name = "Minimal 极简",
            Id = "minimal",
            BackgroundColor = "#CCFFFFFF",
            AccentColor = "#000000",
            BackgroundOpacity = 0.90,
            RimColor = "#33000000",
            FontFamily = "Segoe UI",
            Description = "纯白极简风格"
        },
        new ThemePreset
        {
            Name = "Dark Pro",
            Id = "dark_pro",
            BackgroundColor = "#F40A0A0A",
            AccentColor = "#3B82F6",
            BackgroundOpacity = 0.70,
            RimColor = "#4DFFFFFF",
            FontFamily = "Segoe UI",
            Description = "深色专业风格"
        },
        new ThemePreset
        {
            Name = "Sunset 晚霞",
            Id = "sunset",
            BackgroundColor = "#F42D1B69",
            AccentColor = "#F97316",
            BackgroundOpacity = 0.78,
            RimColor = "#66F97316",
            FontFamily = "Segoe UI",
            Description = "晚霞渐变风格"
        }
    };

    /// <summary>
    /// 当前主题
    /// </summary>
    public ThemePreset CurrentTheme { get; private set; } = BuiltInPresets[0];

    /// <summary>
    /// 加载保存的主题配置
    /// </summary>
    public static ThemeManager Load()
    {
        var manager = new ThemeManager();

        try
        {
            if (File.Exists(ThemeConfigPath))
            {
                var json = File.ReadAllText(ThemeConfigPath);
                var saved = JsonSerializer.Deserialize<SavedThemeConfig>(json);

                if (saved != null)
                {
                    // 查找匹配的预设
                    var preset = BuiltInPresets.FirstOrDefault(p => p.Id == saved.PresetId);
                    if (preset != null)
                    {
                        manager.CurrentTheme = preset;
                    }
                    else if (!string.IsNullOrWhiteSpace(saved.CustomBackgroundColor))
                    {
                        // 自定义主题
                        manager.CurrentTheme = new ThemePreset
                        {
                            Name = "自定义",
                            Id = "custom",
                            BackgroundColor = saved.CustomBackgroundColor,
                            AccentColor = saved.CustomAccentColor ?? "#0A84FF",
                            BackgroundOpacity = saved.CustomBackgroundOpacity ?? 0.75,
                            RimColor = saved.CustomRimColor ?? "#41FFFFFF",
                            FontFamily = saved.CustomFontFamily ?? "Segoe UI"
                        };
                    }
                }
            }
        }
        catch { }

        return manager;
    }

    /// <summary>
    /// 应用主题到设置
    /// </summary>
    public void ApplyToSettings(FluidBarSettings settings)
    {
        settings.BackgroundColor = CurrentTheme.BackgroundColor;
        settings.AccentColor = CurrentTheme.AccentColor;
        settings.BackgroundOpacity = CurrentTheme.BackgroundOpacity;
        // 注意：CornerRadius、Opacity 等其他属性保持用户自定义
    }

    /// <summary>
    /// 切换到指定预设
    /// </summary>
    public void SwitchToPreset(string presetId, FluidBarSettings settings)
    {
        var preset = BuiltInPresets.FirstOrDefault(p => p.Id == presetId);
        if (preset != null)
        {
            CurrentTheme = preset;
            ApplyToSettings(settings);
            Save();
        }
    }

    /// <summary>
    /// 从壁纸提取主色（简化版）
    /// </summary>
    public static ThemePreset? ExtractFromWallpaper(string wallpaperPath)
    {
        try
        {
            if (!File.Exists(wallpaperPath))
                return null;

            // 简化实现：实际应使用 System.Drawing.Bitmap 分析像素
            // 这里返回一个基于文件名的示例主题
            var fileName = Path.GetFileNameWithoutExtension(wallpaperPath).ToLowerInvariant();

            if (fileName.Contains("dark") || fileName.Contains("night"))
            {
                return BuiltInPresets.FirstOrDefault(p => p.Id == "dark_pro");
            }

            if (fileName.Contains("sunset") || fileName.Contains("orange"))
            {
                return BuiltInPresets.FirstOrDefault(p => p.Id == "sunset");
            }

            if (fileName.Contains("neon") || fileName.Contains("cyber"))
            {
                return BuiltInPresets.FirstOrDefault(p => p.Id == "neon");
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 保存当前主题配置
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThemeConfigPath)!);

            var config = new SavedThemeConfig
            {
                PresetId = CurrentTheme.Id == "custom" ? null : CurrentTheme.Id,
                CustomBackgroundColor = CurrentTheme.Id == "custom" ? CurrentTheme.BackgroundColor : null,
                CustomAccentColor = CurrentTheme.Id == "custom" ? CurrentTheme.AccentColor : null,
                CustomBackgroundOpacity = CurrentTheme.Id == "custom" ? CurrentTheme.BackgroundOpacity : null,
                CustomRimColor = CurrentTheme.Id == "custom" ? CurrentTheme.RimColor : null,
                CustomFontFamily = CurrentTheme.Id == "custom" ? CurrentTheme.FontFamily : null
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ThemeConfigPath, json);
        }
        catch { }
    }

    /// <summary>
    /// 获取主题预览色（用于设置 UI）
    /// </summary>
    public static System.Windows.Media.Color GetPreviewColor(string hexColor)
    {
        try
        {
            var colorStr = hexColor.TrimStart('#');
            if (colorStr.Length == 8)
            {
                // ARGB 格式
                var a = byte.Parse(colorStr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                var r = byte.Parse(colorStr.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(colorStr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(colorStr.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                return System.Windows.Media.Color.FromArgb(a, r, g, b);
            }
            else if (colorStr.Length == 6)
            {
                var r = byte.Parse(colorStr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(colorStr.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(colorStr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return System.Windows.Media.Color.FromRgb(r, g, b);
            }
        }
        catch { }

        return System.Windows.Media.Colors.Black;
    }
}

/// <summary>
/// 主题预设
/// </summary>
public sealed class ThemePreset
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string BackgroundColor { get; set; } = "#F4000000";
    public string AccentColor { get; set; } = "#0A84FF";
    public double BackgroundOpacity { get; set; } = 0.75;
    public string RimColor { get; set; } = "#41FFFFFF";
    public string FontFamily { get; set; } = "Segoe UI";
    public string Description { get; set; } = "";
}

/// <summary>
/// 保存的主题配置
/// </summary>
public sealed class SavedThemeConfig
{
    public string? PresetId { get; set; }
    public string? CustomBackgroundColor { get; set; }
    public string? CustomAccentColor { get; set; }
    public double? CustomBackgroundOpacity { get; set; }
    public string? CustomRimColor { get; set; }
    public string? CustomFontFamily { get; set; }
}
