using System.IO;
using System.Text.Json;

namespace FluidBar.Config;

/// <summary>
/// 配置验证和修复系统
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// 验证并修复设置
    /// </summary>
    public static ValidationResult ValidateAndFix(FluidBarSettings settings)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // 验证位置
            if (!IsValidPosition(settings.Position))
            {
                result.Warnings.Add($"无效的位置 '{settings.Position}'，已重置为 'Top'");
                settings.Position = "Top";
                result.FixedCount++;
            }

            // 验证尺寸
            if (settings.CollapsedWidth < 50 || settings.CollapsedWidth > 500)
            {
                result.Warnings.Add($"折叠宽度 {settings.CollapsedWidth} 超出范围 [50, 500]，已重置为 126");
                settings.CollapsedWidth = 126;
                result.FixedCount++;
            }

            if (settings.CollapsedHeight < 20 || settings.CollapsedHeight > 100)
            {
                result.Warnings.Add($"折叠高度 {settings.CollapsedHeight} 超出范围 [20, 100]，已重置为 38");
                settings.CollapsedHeight = 38;
                result.FixedCount++;
            }

            if (settings.ExpandedMaxWidth < 100 || settings.ExpandedMaxWidth > 1000)
            {
                result.Warnings.Add($"展开最大宽度 {settings.ExpandedMaxWidth} 超出范围 [100, 1000]，已重置为 430");
                settings.ExpandedMaxWidth = 430;
                result.FixedCount++;
            }

            // 验证圆角
            if (settings.CornerRadius < 0 || settings.CornerRadius > 50)
            {
                result.Warnings.Add($"圆角半径 {settings.CornerRadius} 超出范围 [0, 50]，已重置为 24");
                settings.CornerRadius = 24;
                result.FixedCount++;
            }

            // 验证不透明度
            if (settings.Opacity < 0.1 || settings.Opacity > 1.0)
            {
                result.Warnings.Add($"不透明度 {settings.Opacity} 超出范围 [0.1, 1.0]，已重置为 0.96");
                settings.Opacity = 0.96;
                result.FixedCount++;
            }

            if (settings.BackgroundOpacity < 0.0 || settings.BackgroundOpacity > 1.0)
            {
                result.Warnings.Add($"背景不透明度 {settings.BackgroundOpacity} 超出范围 [0.0, 1.0]，已重置为 0.75");
                settings.BackgroundOpacity = 0.75;
                result.FixedCount++;
            }

            // 验证颜色格式
            if (!IsValidColor(settings.BackgroundColor))
            {
                result.Warnings.Add($"无效的背景色 '{settings.BackgroundColor}'，已重置为 '#F4000000'");
                settings.BackgroundColor = "#F4000000";
                result.FixedCount++;
            }

            if (!IsValidColor(settings.AccentColor))
            {
                result.Warnings.Add($"无效的强调色 '{settings.AccentColor}'，已重置为 '#0A84FF'");
                settings.AccentColor = "#0A84FF";
                result.FixedCount++;
            }

            // 验证自隐藏延迟
            if (settings.AutoHideDelayMs < 500 || settings.AutoHideDelayMs > 30000)
            {
                result.Warnings.Add($"自隐藏延迟 {settings.AutoHideDelayMs} 超出范围 [500, 30000]，已重置为 3000");
                settings.AutoHideDelayMs = 3000;
                result.FixedCount++;
            }

            // 验证多岛设置
            if (settings.MaxVisibleIslands < 1 || settings.MaxVisibleIslands > 10)
            {
                result.Warnings.Add($"最大可见岛屿数 {settings.MaxVisibleIslands} 超出范围 [1, 10]，已重置为 4");
                settings.MaxVisibleIslands = 4;
                result.FixedCount++;
            }

            if (settings.MultiIslandGap < 0 || settings.MultiIslandGap > 50)
            {
                result.Warnings.Add($"岛屿间距 {settings.MultiIslandGap} 超出范围 [0, 50]，已重置为 10");
                settings.MultiIslandGap = 10;
                result.FixedCount++;
            }

            // 验证快捷键
            if (string.IsNullOrWhiteSpace(settings.HoldToHideKey))
            {
                result.Warnings.Add("隐藏快捷键为空，已重置为 LeftAlt");
                settings.HoldToHideKey = HoldToHideKeyPolicy.LeftAlt;
                result.FixedCount++;
            }

            result.IsValid = result.FixedCount == 0;
            result.Message = result.FixedCount > 0
                ? $"配置已修复 {result.FixedCount} 处问题"
                : "配置验证通过";
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"验证过程中发生错误: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 验证媒体插件设置
    /// </summary>
    public static ValidationResult ValidateMediaSettings(MediaPluginSettings settings)
    {
        var result = new ValidationResult { IsValid = true };

        if (settings.PollIntervalMs < 100 || settings.PollIntervalMs > 10000)
        {
            result.Warnings.Add($"轮询间隔 {settings.PollIntervalMs} 超出范围 [100, 10000]，已重置为 500");
            settings.PollIntervalMs = 500;
            result.FixedCount++;
        }

        result.IsValid = result.FixedCount == 0;
        return result;
    }

    /// <summary>
    /// 验证剪贴板设置
    /// </summary>
    public static ValidationResult ValidateClipboardSettings(ClipboardPluginSettings settings)
    {
        var result = new ValidationResult { IsValid = true };

        if (settings.MinFullDisplayChars < 5 || settings.MinFullDisplayChars > 100)
        {
            result.Warnings.Add($"最小完整显示字符数 {settings.MinFullDisplayChars} 超出范围 [5, 100]，已重置为 20");
            settings.MinFullDisplayChars = 20;
            result.FixedCount++;
        }

        if (settings.DisplayDurationMs < 500 || settings.DisplayDurationMs > 10000)
        {
            result.Warnings.Add($"显示时长 {settings.DisplayDurationMs} 超出范围 [500, 10000]，已重置为 3000");
            settings.DisplayDurationMs = 3000;
            result.FixedCount++;
        }

        if (settings.ScrollSpeed < 0.1 || settings.ScrollSpeed > 10.0)
        {
            result.Warnings.Add($"滚动速度 {settings.ScrollSpeed} 超出范围 [0.1, 10.0]，已重置为 1.0");
            settings.ScrollSpeed = 1.0;
            result.FixedCount++;
        }

        result.IsValid = result.FixedCount == 0;
        return result;
    }

    private static bool IsValidPosition(string position)
    {
        var validPositions = new[] { "Top", "Bottom", "Left", "Right" };
        return validPositions.Contains(position, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        var hex = color.TrimStart('#');
        return (hex.Length == 6 || hex.Length == 8) &&
               hex.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }
}

/// <summary>
/// 验证结果
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public int FixedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = "";
}
