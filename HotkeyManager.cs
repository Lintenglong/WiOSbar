using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace FluidBar;

/// <summary>
/// 全局快捷键管理器
/// 支持注册系统级热键（如 Ctrl+Alt+M）
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly HwndSource? _hwndSource;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _nextHotkeyId = 9000; // 起始 ID，避免与系统热键冲突

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // 修饰键标志
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    public HotkeyManager(System.Windows.Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            // 窗口尚未创建，延迟初始化
            window.SourceInitialized += (_, _) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                InitializeHwndSource(handle);
            };
        }
        else
        {
            InitializeHwndSource(hwnd);
        }
    }

    private void InitializeHwndSource(IntPtr hwnd)
    {
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// 注册快捷键
    /// </summary>
    /// <param name="modifiers">修饰键（如 ModifierKeys.Control | ModifierKeys.Alt）</param>
    /// <param name="key">主键</param>
    /// <param name="action">触发时执行的动作</param>
    /// <returns>是否注册成功</returns>
    public bool RegisterHotkey(ModifierKeys modifiers, Key key, Action action)
    {
        if (_hwndSource == null)
            return false;

        var id = _nextHotkeyId++;
        var fsModifiers = ModifiersToFlags(modifiers);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        var success = RegisterHotKey(_hwndSource.Handle, id, fsModifiers, vk);
        if (success)
        {
            _hotkeyActions[id] = action;
        }

        return success;
    }

    /// <summary>
    /// 取消注册快捷键
    /// </summary>
    public void UnregisterHotkey(int id)
    {
        if (_hwndSource != null && _hotkeyActions.ContainsKey(id))
        {
            UnregisterHotKey(_hwndSource.Handle, id);
            _hotkeyActions.Remove(id);
        }
    }

    private static uint ModifiersToFlags(ModifierKeys modifiers)
    {
        uint flags = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt))
            flags |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control))
            flags |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift))
            flags |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows))
            flags |= MOD_WIN;

        return flags;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hwndSource != null)
        {
            foreach (var id in _hotkeyActions.Keys.ToList())
            {
                UnregisterHotKey(_hwndSource.Handle, id);
            }
            _hotkeyActions.Clear();
        }
    }
}

/// <summary>
/// 预定义快捷键策略
/// </summary>
public static class HotkeyPolicy
{
    /// <summary>
    /// 默认快捷键配置
    /// </summary>
    public static readonly Dictionary<string, (ModifierKeys Modifiers, Key Key, string Description)> DefaultHotkeys = new()
    {
        ["ToggleVisibility"] = (ModifierKeys.Control | ModifierKeys.Alt, Key.H, "临时隐藏/显示灵动岛"),
        ["ShowMedia"] = (ModifierKeys.Control | ModifierKeys.Alt, Key.M, "立即切换到媒体显示"),
        ["ShowClipboard"] = (ModifierKeys.Control | ModifierKeys.Alt, Key.C, "打开剪贴板历史"),
        ["ShowNotifications"] = (ModifierKeys.Control | ModifierKeys.Alt, Key.N, "显示最新通知"),
        ["OpenSettings"] = (ModifierKeys.Control | ModifierKeys.Alt, Key.S, "打开设置面板"),
    };
}
