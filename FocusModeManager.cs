using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace FluidBar;

/// <summary>
/// 涓撴敞/娓告垙妯″紡绠＄悊鍣?- 鑷姩闅愯棌鐏靛姩宀?/// </summary>
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
    /// 鍚姩涓撴敞妯″紡妫€娴?    /// </summary>
    public void Start(Action<bool> onFocusModeChanged)
    {
        _onFocusModeChanged = onFocusModeChanged;
        _checkTimer.Start();
    }

    /// <summary>
    /// 鍋滄妫€娴?    /// </summary>
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
            // 闈欓粯澶辫触
        }
    }

    /// <summary>
    /// 鍒ゆ柇褰撳墠鏄惁搴旇闅愯棌鐏靛姩宀?    /// </summary>
    private bool ShouldHideIsland()
    {
        // 1. 妫€鏌ュ叏灞忓簲鐢?        if (IsFullscreenApplication())
            return true;

        // 2. 妫€鏌ユ父鎴忚繘绋?        if (IsGameProcess())
            return true;

        // 3. 妫€鏌ヨ棰戞挱鏀撅紙娴忚鍣ㄥ叏灞忥級
        if (IsVideoPlayback())
            return true;

        // 4. 妫€鏌ヤ笓娉ㄦā寮忥紙Windows 涓撴敞鍔╂墜锛?        if (IsWindowsFocusAssistEnabled())
            return true;

        return false;
    }

    /// <summary>
    /// 妫€娴嬪叏灞忓簲鐢?    /// </summary>
    private static bool IsFullscreenApplication()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            // 鑾峰彇绐楀彛鐭╁舰
            GetWindowRect(hwnd, out var rect);
            var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

            // 濡傛灉绐楀彛灏哄鎺ヨ繎鎴栫瓑浜庡睆骞曞昂瀵革紝璁や负鏄叏灞?            var windowWidth = rect.Right - rect.Left;
            var windowHeight = rect.Bottom - rect.Top;

            // 鍏佽 5% 璇樊
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
    /// 妫€娴嬫父鎴忚繘绋?    /// </summary>
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

            // 甯歌娓告垙杩涚▼鍏抽敭璇?            var gameKeywords = new[]
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
    /// 妫€娴嬭棰戞挱鏀撅紙娴忚鍣ㄥ叏灞忔ā寮忥級
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

            // 娴忚鍣ㄥ叏灞忛€氬父浣跨敤鐗瑰畾绐楀彛绫?            return classNameStr.Contains("chrome") ||
                   classNameStr.Contains("firefox") ||
                   classNameStr.Contains("edge");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 妫€娴?Windows 涓撴敞鍔╂墜
    /// </summary>
    private static bool IsWindowsFocusAssistEnabled()
    {
        try
        {
            // 璇诲彇娉ㄥ唽琛ㄥ垽鏂笓娉ㄥ姪鎵嬬姸鎬?            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\QuietHours");

            if (key == null)
                return false;

            var value = key.GetValue("QuietHoursState");
            if (value is int state)
            {
                // 1 = 浼樺厛绾ч€氱煡, 2 = 浠呴椆閽?                return state > 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}


