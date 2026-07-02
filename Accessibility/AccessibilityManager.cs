using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace FluidBar.Accessibility;

/// <summary>
/// 无障碍增强管理器 - 支持屏幕阅读器和高对比度
/// </summary>
public static class AccessibilityManager
{
    /// <summary>
    /// 应用无障碍设置到窗口
    /// </summary>
    public static void ApplyAccessibility(Window window, FluidBarSettings settings)
    {
        if (settings == null)
            return;

        // 应用高对比度模式
        if (ShouldUseHighContrast())
        {
            ApplyHighContrastMode(window);
        }

        // 设置 Automation 属性
        SetupAutomationProperties(window);
    }

    /// <summary>
    /// 检测系统是否启用了高对比度
    /// </summary>
    private static bool ShouldUseHighContrast()
    {
        try
        {
            // 检查系统高对比度设置
            var colorFilter = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Accessibility",
                "ColorFilterType", 0);

            return colorFilter is int type && type > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 应用高对比度模式
    /// </summary>
    private static void ApplyHighContrastMode(Window window)
    {
        // 增加边框对比度
        window.BorderThickness = new Thickness(2);
        window.BorderBrush = System.Windows.Media.Brushes.White;

        // 递归设置子控件
        ApplyHighContrastToChildren(window);
    }

    private static void ApplyHighContrastToChildren(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is TextBlock textBlock)
            {
                textBlock.Foreground = System.Windows.Media.Brushes.White;
            }
            else if (child is Border border)
            {
                border.BorderBrush = System.Windows.Media.Brushes.White;
                border.BorderThickness = new Thickness(1);
            }

            ApplyHighContrastToChildren(child);
        }
    }

    /// <summary>
    /// 设置 Automation 属性（供屏幕阅读器使用）
    /// </summary>
    private static void SetupAutomationProperties(Window window)
    {
        // 设置窗口的 Automation 属性
        AutomationProperties.SetName(window, "FluidBar 灵动岛");
        AutomationProperties.SetHelpText(window, "显示系统状态和通知的浮动窗口");

        // 查找并设置关键控件的 Automation 属性
        SetupControlAutomation(window);
    }

    private static void SetupControlAutomation(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            // 为 Border 设置角色
            if (child is Border border)
            {
                AutomationProperties.SetControlType(border, AutomationControlType.Pane);
            }

            // 为 TextBlock 设置名称
            if (child is TextBlock textBlock && string.IsNullOrEmpty(AutomationProperties.GetName(textBlock)))
            {
                AutomationProperties.SetName(textBlock, textBlock.Text);
            }

            SetupControlAutomation(child);
        }
    }

    /// <summary>
    /// 为灵动岛事件添加无障碍通知
    /// </summary>
    public static void AnnounceEvent(string eventDescription)
    {
        try
        {
            // 使用 AutomationPeer 发送通知
            if (Application.Current?.MainWindow != null)
            {
                var peer = UIElementAutomationPeer.FromElement(Application.Current.MainWindow);
                if (peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
                }
            }
        }
        catch
        {
            // 静默失败
        }
    }

    /// <summary>
    /// 检查是否需要字体缩放
    /// </summary>
    public static double GetFontScale()
    {
        try
        {
            // 读取系统字体缩放设置
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Desktop\WindowMetrics");

            if (key?.GetValue("AppliedDPI") is int dpi)
            {
                // 标准 DPI 是 96，缩放比例 = dpi / 96
                return dpi / 96.0;
            }
        }
        catch { }

        return 1.0; // 默认不缩放
    }

    /// <summary>
    /// 应用字体缩放到控件
    /// </summary>
    public static void ApplyFontScale(FrameworkElement element, double scale)
    {
        if (scale <= 1.0)
            return;

        if (element is TextBlock textBlock)
        {
            textBlock.FontSize *= scale;
        }
        else if (element is Control control)
        {
            if (control.FontSize > 0)
                control.FontSize *= scale;
        }
    }
}
