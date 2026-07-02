using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace FluidBar;

/// <summary>
/// 涓婚鍖呯鐞嗗櫒 - 鏀寔棰勮涓婚鍜屽姩鎬佸绾搁€傞厤
/// </summary>
public sealed class ThemeManager
{
    private static readonly string ThemeConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "theme.json");

    /// <summary>
    /// 棰勮涓婚鍒楄〃
    /// </summary>
    public static readonly List<ThemePreset> BuiltInPresets = new()
    {
        new ThemePreset
        {
            Name = "iOS 缁忓吀",
            Id = "ios_classic",
            BackgroundColor = "#F4000000",
            AccentColor = "#0A84FF",
            BackgroundOpacity = 0.75,
            RimColor = "#41FFFFFF",
            FontFamily = "Segoe UI",
            Description = "榛樿鐨?iOS 鐏靛姩宀涢鏍?
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
            Description = "Android 12+ Material You 椋庢牸"
        },
        new ThemePreset
        {
            Name = "Neon 闇撹櫣",
            Id = "neon",
            BackgroundColor = "#E6000000",
            AccentColor = "#FF00FF",
            BackgroundOpacity = 0.80,
            RimColor = "#80FF00FF",
            FontFamily = "Consolas",
            Description = "璧涘崥鏈嬪厠闇撹櫣椋庢牸"
        },
        new ThemePreset
        {
            Name = "Minimal 鏋佺畝",
            Id = "minimal",
            BackgroundColor = "#CCFFFFFF",
            AccentColor = "#000000",
            BackgroundOpacity = 0.90,
            RimColor = "#33000000",
            FontFamily = "Segoe UI",
            Description = "绾櫧鏋佺畝椋庢牸"
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
            Description = "娣辫壊涓撲笟椋庢牸"
        },
        new ThemePreset
        {
            Name = "Sunset 鏅氶湠",
            Id = "sunset",
            BackgroundColor = "#F42D1B69",
            AccentColor = "#F97316",
            BackgroundOpacity = 0.78,
            RimColor = "#66F97316",
            FontFamily = "Segoe UI",
            Description = "鏅氶湠娓愬彉椋庢牸"
        }
    };

    /// <summary>
    /// 褰撳墠涓婚
    /// </summary>
    public ThemePreset CurrentTheme { get; private set; } = BuiltInPresets[0];

    /// <summary>
    /// 鍔犺浇淇濆瓨鐨勪富棰橀厤缃?    /// </summary>
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
                    // 鏌ユ壘鍖归厤鐨勯璁?                    var preset = BuiltInPresets.FirstOrDefault(p => p.Id == saved.PresetId);
                    if (preset != null)
                    {
                        manager.CurrentTheme = preset;
                    }
                    else if (!string.IsNullOrWhiteSpace(saved.CustomBackgroundColor))
                    {
                        // 鑷畾涔変富棰?                        manager.CurrentTheme = new ThemePreset
                        {
                            Name = "鑷畾涔?,
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
    /// 搴旂敤涓婚鍒拌缃?    /// </summary>
    public void ApplyToSettings(FluidBarSettings settings)
    {
        settings.BackgroundColor = CurrentTheme.BackgroundColor;
        settings.AccentColor = CurrentTheme.AccentColor;
        settings.BackgroundOpacity = CurrentTheme.BackgroundOpacity;
        // 娉ㄦ剰锛欳ornerRadius銆丱pacity 绛夊叾浠栧睘鎬т繚鎸佺敤鎴疯嚜瀹氫箟
    }

    /// <summary>
    /// 鍒囨崲鍒版寚瀹氶璁?    /// </summary>
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
    /// 浠庡绾告彁鍙栦富鑹诧紙绠€鍖栫増锛?    /// </summary>
    public static ThemePreset? ExtractFromWallpaper(string wallpaperPath)
    {
        try
        {
            if (!File.Exists(wallpaperPath))
                return null;

            // 绠€鍖栧疄鐜帮細瀹為檯搴斾娇鐢?System.Drawing.Bitmap 鍒嗘瀽鍍忕礌
            // 杩欓噷杩斿洖涓€涓熀浜庢枃浠跺悕鐨勭ず渚嬩富棰?            var fileName = Path.GetFileNameWithoutExtension(wallpaperPath).ToLowerInvariant();

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
    /// 淇濆瓨褰撳墠涓婚閰嶇疆
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
    /// 鑾峰彇涓婚棰勮鑹诧紙鐢ㄤ簬璁剧疆 UI锛?    /// </summary>
    public static System.Windows.Media.Color GetPreviewColor(string hexColor)
    {
        try
        {
            var colorStr = hexColor.TrimStart('#');
            if (colorStr.Length == 8)
            {
                // ARGB 鏍煎紡
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

        return Colors.Black;
    }
}

/// <summary>
/// 涓婚棰勮
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
/// 淇濆瓨鐨勪富棰橀厤缃?/// </summary>
public sealed class SavedThemeConfig
{
    public string? PresetId { get; set; }
    public string? CustomBackgroundColor { get; set; }
    public string? CustomAccentColor { get; set; }
    public double? CustomBackgroundOpacity { get; set; }
    public string? CustomRimColor { get; set; }
    public string? CustomFontFamily { get; set; }
}


