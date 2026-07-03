using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using FluidBar.Monitors;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace FluidBar;

public partial class App : Application
{
    private NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private EventBus? _bus;
    private PluginManager? _pluginManager;
    private SystemMonitorManager? _monitorManager;
    private ClipboardPlugin? _clipboardPlugin;
    private MediaPlugin? _mediaPlugin;
    private AgentStatusPlugin? _agentStatusPlugin;
    private FluidBarSettings? _settings;
    private HotkeyManager? _hotkeyManager;
    private UsageStatistics? _usageStats;
    private FocusModeManager? _focusModeManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Phase 3：启用崩溃恢复机制
        EnableCrashRecovery();

        _settings = FluidBarSettings.Load();
        _usageStats = UsageStatistics.Load();
        _bus = new EventBus();

        SetupTrayIcon();
        SetupThemeWatcher();

        // 创建主窗口
        _mainWindow = new MainWindow(_bus, _settings);
        _mainWindow.RequestOpenSettings += OpenSettings;
        _mainWindow.Show();

        // 初始化全局快捷键（Phase 2 新增）
        SetupHotkeys();

        // 初始化专注模式检测（Phase 5 新增）
        SetupFocusMode();

        // 应用自启动设置（如果已启用）
        ApplyStartupSetting();

        // 初始化插件系统
        _pluginManager = new PluginManager(_bus, _settings);
        _clipboardPlugin = new ClipboardPlugin();
        _pluginManager.Register(_clipboardPlugin);
        _clipboardPlugin.AttachWindow(_mainWindow);
        _mediaPlugin = new MediaPlugin();
        _pluginManager.Register(_mediaPlugin);
        _agentStatusPlugin = new AgentStatusPlugin();
        _pluginManager.Register(_agentStatusPlugin);
        _pluginManager.StartAll();

        if (_clipboardPlugin.Config is ClipboardPluginConfig cfg)
        {
            _mainWindow.SetClipboardPluginSettings(
                (ClipboardPluginSettings)cfg.CreateSettingsPanel());
        }

        if (_mediaPlugin?.SessionProvider is IMediaSessionProvider mediaProvider)
        {
            _mainWindow.SetMediaSessionProvider(mediaProvider);
        }

        // 初始化系统监控
        _monitorManager = new SystemMonitorManager(_bus, _settings);
        _monitorManager.Register(new VolumeMonitor());
        _monitorManager.Register(new BatteryMonitor());
        _monitorManager.Register(new InputMethodMonitor());
        _monitorManager.Register(new LockKeyMonitor());
        _monitorManager.Register(new NetworkMonitor());
        _monitorManager.Register(new UsbMonitor());
        _monitorManager.Register(new BrightnessMonitor());
        _monitorManager.Register(new BluetoothMonitor());
        _monitorManager.Register(new ClockMonitor());
        _monitorManager.Register(new NotificationMonitor());

        // 新增：系统资源监控（CPU/内存/磁盘）
        _monitorManager.Register(new CpuMonitor());
        _monitorManager.Register(new MemoryMonitor());
        _monitorManager.Register(new DiskMonitor());

        // 新增：天气监控（需配置 API Key 后启用）
        _monitorManager.Register(new WeatherMonitor());

        // 新增：网络速度监控
        _monitorManager.Register(new NetworkSpeedMonitor());

        // 延迟启动：确保 Window_Loaded 先完成（PositionWindow + ApplySettings）
        // 否则事件触发时窗口尚未就位，动画被 PositionWindow 覆盖
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => _monitorManager.StartAll()));

        // 启动时根据配置隐藏托盘图标
        if (_settings.HideTrayIcon && _trayIcon != null)
            _trayIcon.Visible = false;

        // 不再在启动时推送系统主题（避免弹出 "Dark"/"Light" 文字）
        // 系统主题变更由 SetupThemeWatcher 中的 UserPreferenceChanged 事件处理
    }

    #region 全局快捷键

    private void SetupHotkeys()
    {
        if (_mainWindow == null)
            return;

        _hotkeyManager = new HotkeyManager(_mainWindow);

        // 注册默认快捷键
        // Ctrl+Alt+H: 临时隐藏灵动岛（已由 Alt 键实现，此处保留作为示例）
        // Ctrl+Alt+M: 立即切换到媒体显示
        // Ctrl+Alt+C: 打开剪贴板历史
        // Ctrl+Alt+S: 打开设置面板

        // 注意：快捷键动作需要 MainWindow 暴露相应方法
        // 以下为示例代码，实际使用需根据 MainWindow API 调整

        /*
        _hotkeyManager.RegisterHotkey(
            ModifierKeys.Control | ModifierKeys.Alt,
            Key.M,
            () => _mainWindow?.ForceShowMedia());

        _hotkeyManager.RegisterHotkey(
            ModifierKeys.Control | ModifierKeys.Alt,
            Key.C,
            () => _mainWindow?.ShowClipboardHistory());

        _hotkeyManager.RegisterHotkey(
            ModifierKeys.Control | ModifierKeys.Alt,
            Key.S,
            () => OpenSettings());
        */
    }

    #endregion

    #region 专注模式与自启动（Phase 5 新增）

    private void SetupFocusMode()
    {
        if (_mainWindow == null)
            return;

        _focusModeManager = new FocusModeManager(_settings!);
        _focusModeManager.Start(isFocusMode =>
        {
            if (isFocusMode)
            {
                // 进入专注模式，隐藏灵动岛
                _mainWindow?.HideForFocusMode();
            }
            else
            {
                // 退出专注模式，恢复显示
                _mainWindow?.ShowAfterFocusMode();
            }
        });
    }

    private void ApplyStartupSetting()
    {
        // 检查是否需要应用自启动（首次运行或设置变更）
        // 实际的自启动开关应在设置界面中提供
        // 这里仅作为框架，记录当前状态
        var isStartupEnabled = StartupManager.IsEnabled();
        // 可选：记录到日志或使用统计
    }

    #endregion

    #region 崩溃恢复（Phase 3 新增）

    private void EnableCrashRecovery()
    {
        // 捕获非 UI 线程异常
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            LogCrash(ex, "AppDomain.UnhandledException");

            // 尝试保存状态
            try
            {
                _settings?.Save();
            }
            catch { }

            // 如果是严重错误，记录但不阻止应用退出
            if (e.IsTerminating)
            {
                // 记录日志供下次启动分析
            }
        };

        // 捕获 UI 线程异常
        this.DispatcherUnhandledException += (sender, e) =>
        {
            LogCrash(e.Exception, "DispatcherUnhandledException");

            // 尝试恢复：不让应用崩溃
            e.Handled = true;

            // 尝试恢复 UI 状态
            try
            {
                // 如果主窗口存在，尝试重新显示
                if (_mainWindow != null && !_mainWindow.IsVisible)
                {
                    _mainWindow.Show();
                }
            }
            catch { }
        };

        // Task 异常（未观察的）
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LogCrash(e.Exception, "UnobservedTaskException");
            e.SetObserved(); // 阻止进程终止
        };
    }

    private static void LogCrash(Exception? ex, string source)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "logs");

            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n" +
                         $"Exception: {ex?.GetType().Name}\n" +
                         $"Message: {ex?.Message}\n" +
                         $"StackTrace:\n{ex?.StackTrace}\n" +
                         new string('-', 80) + "\n";

            File.AppendAllText(logFile, message);
        }
        catch
        {
            // 日志写入失败，静默忽略
        }
    }

    #endregion

    #region 系统主题检测

    private void SetupThemeWatcher()
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // 系统主题可能已变化，通知灵动岛
                _bus?.Publish(new IslandEvent(
                    "system", "主题变更",
                    GetSystemTheme(), "info"));
            }
        };
    }

    /// <summary>
    /// 获取当前系统主题：Dark 或 Light
    /// </summary>
    public static string GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
                return value == 0 ? "Dark" : "Light";
        }
        catch { }
        return "Dark";
    }

    /// <summary>
    /// 获取系统主题对应的背景色
    /// </summary>
    public static string GetThemeBackgroundColor()
    {
        return GetSystemTheme() == "Dark" ? "#E8202022" : "#E0F0F0F5";
    }

    #endregion

    #region 托盘图标

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "FluidBar",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        var settingsItem = new ToolStripMenuItem("设置");
        settingsItem.Click += (_, _) => OpenSettings();
        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => OpenSettings();
    }

    private static Icon CreateTrayIcon()
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var path = new GraphicsPath();
        int cr = 7;
        var rect = new Rectangle(3, 9, size - 6, 14);
        path.AddArc(rect.X, rect.Y, cr * 2, cr * 2, 180, 90);
        path.AddArc(rect.Right - cr * 2, rect.Y, cr * 2, cr * 2, 270, 90);
        path.AddArc(rect.Right - cr * 2, rect.Bottom - cr * 2, cr * 2, cr * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - cr * 2, cr * 2, cr * 2, 90, 90);
        path.CloseFigure();

        using var glow = new SolidBrush(Color.FromArgb(60, 10, 132, 255));
        g.FillEllipse(glow, 4, 4, 24, 24);

        using var brush = new SolidBrush(Color.FromArgb(245, 0, 0, 0));
        g.FillPath(brush, path);

        using var rimPen = new Pen(Color.FromArgb(65, 255, 255, 255), 1f);
        g.DrawPath(rimPen, path);

        using var accent = new SolidBrush(Color.FromArgb(235, 10, 132, 255));
        g.FillEllipse(accent, 11, 13, 4, 4);
        using var soft = new SolidBrush(Color.FromArgb(210, 48, 209, 88));
        g.FillEllipse(soft, 17, 13, 4, 4);

        return Icon.FromHandle(bmp.GetHicon());
    }

    #endregion

    private void OpenSettings()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(
                _settings!, _pluginManager!, _monitorManager!, OnSettingsChanged);
            _settingsWindow.TrayIconVisibilityChanged += OnTrayIconVisibilityChanged;
            _settingsWindow.IsVisibleChanged += (_, _) =>
            {
                if (_settingsWindow.IsVisible)
                    _mainWindow?.OnSettingsPanelOpened();
                else
                    _mainWindow?.OnSettingsPanelClosed();
            };
        }

        // 重置透明度（淡出动画可能将其设为 0）
        _settingsWindow.BeginAnimation(UIElement.OpacityProperty, null);
        _settingsWindow.Opacity = 1;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnSettingsChanged()
    {
        _mainWindow?.ApplySettings();
    }

    private void OnTrayIconVisibilityChanged(bool hide)
    {
        if (_trayIcon != null)
            _trayIcon.Visible = !hide;
    }

    private void ExitApp()
    {
        _monitorManager?.Dispose();
        _pluginManager?.Dispose();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _hotkeyManager?.Dispose();
        _settingsWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _monitorManager?.Dispose();
        _pluginManager?.Dispose();
        base.OnExit(e);
    }
}
