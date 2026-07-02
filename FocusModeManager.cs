using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace FluidBar;

/// <summary>
/// 专注/游戏模式管理器 - 自动隐藏灵动岛
/// </summary>
public sealed class FocusModeManager
{
    private readonly FluidBarSettings _settings;
    private readonly DispatcherTimer _checkTimer;
    private bool _isInFocusMode;
    private Action<bool>? _onFocusModeChanged;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public bool IsInFocusMode => _isInFocusMode;

    public FocusModeManager(FluidBarSettings settings)
    {
        _settings = settings;
        _checkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _checkTimer.Tick += CheckFocusMode;
    }

    /// <summary>
    /// 启动专注模式检测
    /// </summary>
    public void Start(Action<bool> onFocusModeChanged)
    {
        _onFocusModeChanged = onFocusModeChanged;
        _checkTimer.Start();
    }

    /// <summary>
    /// 停止检测
    /// </summary>
    public void Stop()
    {
        _checkTimer.Stop();
    }

    private void CheckFocusMode(object? sender, EventArgs e)
    {
        try
        {
            var shouldHide = ShouldHideIsland();
            if (shouldHide != _isInFocusMode)
            {
                _isInFocusMode = shouldHide;
                _onFocusModeChanged?.Invoke(shouldHide);
            }
        }
        catch
        {
            // 静默失败
        }
    }

    /// <summary>
    /// 判断当前是否应该隐藏灵动岛
    /// </summary>
    private bool ShouldHideIsland()
    {
        // 1. 检查全屏应用
        if (IsFullscreenApplication())
            return true;

        // 2. 检查游戏进程
        if (IsGameProcess())
            return true;

        // 3. 检查视频播放（浏览器全屏）
        if (IsVideoPlayback())
            return true;

        // 4. 检查专注模式（Windows 专注助手）
        if (IsWindowsFocusAssistEnabled())
            return true;

        return false;
    }

    /// <summary>
    /// 检测全屏应用
    /// </summary>
    private static bool IsFullscreenApplication()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            // 获取窗口矩形
            GetWindowRect(hwnd, out var rect);
            var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

            // 如果窗口尺寸接近或等于屏幕尺寸，认为是全屏
            var windowWidth = rect.Right - rect.Left;
            var windowHeight = rect.Bottom - rect.Top;

            // 允许 5% 误差
            var widthRatio = Math.Abs(windowWidth - screenWidth) / screenWidth;
            var heightRatio = Math.Abs(windowHeight - screenHeight) / screenHeight;

            return widthRatio < 0.05 && heightRatio < 0.05;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// 检测游戏进程
    /// </summary>
    private static bool IsGameProcess()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            GetWindowThreadProcessId(hwnd, out var processId);
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName.ToLowerInvariant();

            // 常见游戏进程关键词
            var gameKeywords = new[]
            {
                "game", "steam", "origin", "epic", "battle", "league", "dota",
                "wow", "minecraft", "fortnite", "apex", "valorant", "csgo",
                "overwatch", "destiny", "warframe", "genshin", "starrail"
            };

            return gameKeywords.Any(kw => processName.Contains(kw));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检测视频播放（浏览器全屏模式）
    /// </summary>
    private static bool IsVideoPlayback()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            var className = new System.Text.StringBuilder(256);
            GetClassName(hwnd, className, className.Capacity);
            var classNameStr = className.ToString().ToLowerInvariant();

            // 浏览器全屏通常使用特定窗口类
            return classNameStr.Contains("chrome") ||
                   classNameStr.Contains("firefox") ||
                   classNameStr.Contains("edge");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检测 Windows 专注助手
    /// </summary>
    private static bool IsWindowsFocusAssistEnabled()
    {
        try
        {
            // 读取注册表判断专注助手状态
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\QuietHours");

            if (key == null)
                return false;

            var value = key.GetValue("QuietHoursState");
            if (value is int state)
            {
                // 1 = 优先级通知, 2 = 仅闹钟
                return state > 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
