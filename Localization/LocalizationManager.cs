using System.Globalization;
using System.Resources;

namespace FluidBar.Localization;

/// <summary>
/// 本地化管理器 - 支持多语言切换
/// </summary>
public static class LocalizationManager
{
    private static CultureInfo? _currentCulture;
    private static ResourceManager? _resourceManager;

    /// <summary>
    /// 支持的语言列表
    /// </summary>
    public static readonly Dictionary<string, string> SupportedLanguages = new()
    {
        ["zh-CN"] = "简体中文",
        ["en-US"] = "English",
        ["ja-JP"] = "日本語",
        ["ko-KR"] = "한국어"
    };

    /// <summary>
    /// 当前语言
    /// </summary>
    public static string CurrentLanguage
    {
        get => _currentCulture?.Name ?? "zh-CN";
        set
        {
            if (SupportedLanguages.ContainsKey(value))
            {
                _currentCulture = new CultureInfo(value);
                CultureInfo.CurrentUICulture = _currentCulture;
            }
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public static string GetString(string key)
    {
        try
        {
            _resourceManager ??= new ResourceManager("FluidBar.Localization.Strings",
                typeof(LocalizationManager).Assembly);

            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key; // 如果找不到翻译，返回键名
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// 自动检测系统语言
    /// </summary>
    public static void DetectSystemLanguage()
    {
        var systemCulture = CultureInfo.CurrentUICulture.Name;

        // 匹配支持的语言
        if (SupportedLanguages.ContainsKey(systemCulture))
        {
            CurrentLanguage = systemCulture;
        }
        else
        {
            // 尝试匹配语言前缀（如 zh-Hans -> zh-CN）
            var langPrefix = systemCulture.Split('-')[0];
            var match = SupportedLanguages.Keys.FirstOrDefault(k => k.StartsWith(langPrefix));
            if (match != null)
            {
                CurrentLanguage = match;
            }
            else
            {
                CurrentLanguage = "zh-CN"; // 默认中文
            }
        }
    }

    /// <summary>
    /// 格式化字符串（支持参数）
    /// </summary>
    public static string Format(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// 获取当前语言的显示名称
    /// </summary>
    public static string GetCurrentLanguageDisplayName()
    {
        return SupportedLanguages.TryGetValue(CurrentLanguage, out var name) ? name : CurrentLanguage;
    }
}

/// <summary>
/// 便捷扩展方法
/// </summary>
public static class LocalizationExtensions
{
    public static string Localize(this string key)
    {
        return LocalizationManager.GetString(key);
    }
}
