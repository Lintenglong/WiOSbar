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

        // Phase 3锛氬惎鐢ㄥ穿婧冩仮澶嶆満鍒?        EnableCrashRecovery();

        _settings = FluidBarSettings.Load();
        _usageStats = UsageStatistics.Load();
        _bus = new EventBus();

        SetupTrayIcon();
        SetupThemeWatcher();

        // 鍒涘缓涓荤獥鍙?        _mainWindow = new MainWindow(_bus, _settings);
        _mainWindow.RequestOpenSettings += OpenSettings;
        _mainWindow.Show();

        // 鍒濆鍖栧叏灞€蹇嵎閿紙Phase 2 鏂板锛?        SetupHotkeys();

        // 鍒濆鍖栦笓娉ㄦā寮忔娴嬶紙Phase 5 鏂板锛?        SetupFocusMode();

        // 搴旂敤鑷惎鍔ㄨ缃紙濡傛灉宸插惎鐢級
        ApplyStartupSetting();

        // 鍒濆鍖栨彃浠剁郴缁?        _pluginManager = new PluginManager(_bus, _settings);
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

        // 鍒濆鍖栫郴缁熺洃鎺?        _monitorManager = new SystemMonitorManager(_bus, _settings);
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

        // 鏂板锛氱郴缁熻祫婧愮洃鎺э紙CPU/鍐呭瓨/纾佺洏锛?        _monitorManager.Register(new CpuMonitor());
        _monitorManager.Register(new MemoryMonitor());
        _monitorManager.Register(new DiskMonitor());

        // 鏂板锛氬ぉ姘旂洃鎺э紙闇€閰嶇疆 API Key 鍚庡惎鐢級
        _monitorManager.Register(new WeatherMonitor());

        // 鏂板锛氱綉缁滈€熷害鐩戞帶
        _monitorManager.Register(new NetworkSpeedMonitor());

        // 寤惰繜鍚姩锛氱‘淇?Window_Loaded 鍏堝畬鎴愶紙PositionWindow + ApplySettings锛?        // 鍚﹀垯浜嬩欢瑙﹀彂鏃剁獥鍙ｅ皻鏈氨浣嶏紝鍔ㄧ敾琚?PositionWindow 瑕嗙洊
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => _monitorManager.StartAll()));

        // 鍚姩鏃舵牴鎹厤缃殣钘忔墭鐩樺浘鏍?        if (_settings.HideTrayIcon && _trayIcon != null)
            _trayIcon.Visible = false;

        // 涓嶅啀鍦ㄥ惎鍔ㄦ椂鎺ㄩ€佺郴缁熶富棰橈紙閬垮厤寮瑰嚭 "Dark"/"Light" 鏂囧瓧锛?        // 绯荤粺涓婚鍙樻洿鐢?SetupThemeWatcher 涓殑 UserPreferenceChanged 浜嬩欢澶勭悊
    }

    #region 鍏ㄥ眬蹇嵎閿?
    private void SetupHotkeys()
    {
        if (_mainWindow == null)
            return;

        _hotkeyManager = new HotkeyManager(_mainWindow);

        // 娉ㄥ唽榛樿蹇嵎閿?        // Ctrl+Alt+H: 涓存椂闅愯棌鐏靛姩宀涳紙宸茬敱 Alt 閿疄鐜帮紝姝ゅ淇濈暀浣滀负绀轰緥锛?        // Ctrl+Alt+M: 绔嬪嵆鍒囨崲鍒板獟浣撴樉绀?        // Ctrl+Alt+C: 鎵撳紑鍓创鏉垮巻鍙?        // Ctrl+Alt+S: 鎵撳紑璁剧疆闈㈡澘

        // 娉ㄦ剰锛氬揩鎹烽敭鍔ㄤ綔闇€瑕?MainWindow 鏆撮湶鐩稿簲鏂规硶
        // 浠ヤ笅涓虹ず渚嬩唬鐮侊紝瀹為檯浣跨敤闇€鏍规嵁 MainWindow API 璋冩暣

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

    #region 涓撴敞妯″紡涓庤嚜鍚姩锛圥hase 5 鏂板锛?
    private void SetupFocusMode()
    {
        if (_mainWindow == null)
            return;

        _focusModeManager = new FocusModeManager(_settings!);
        _focusModeManager.Start(isFocusMode =>
        {
            if (isFocusMode)
            {
                // 杩涘叆涓撴敞妯″紡锛岄殣钘忕伒鍔ㄥ矝
                _mainWindow?.HideForFocusMode();
            }
            else
            {
                // 閫€鍑轰笓娉ㄦā寮忥紝鎭㈠鏄剧ず
                _mainWindow?.ShowAfterFocusMode();
            }
        });
    }

    private void ApplyStartupSetting()
    {
        // 妫€鏌ユ槸鍚﹂渶瑕佸簲鐢ㄨ嚜鍚姩锛堥娆¤繍琛屾垨璁剧疆鍙樻洿锛?        // 瀹為檯鐨勮嚜鍚姩寮€鍏冲簲鍦ㄨ缃晫闈腑鎻愪緵
        // 杩欓噷浠呬綔涓烘鏋讹紝璁板綍褰撳墠鐘舵€?        var isStartupEnabled = StartupManager.IsEnabled();
        // 鍙€夛細璁板綍鍒版棩蹇楁垨浣跨敤缁熻
    }

    #endregion

    #region 宕╂簝鎭㈠锛圥hase 3 鏂板锛?
    private void EnableCrashRecovery()
    {
        // 鎹曡幏闈?UI 绾跨▼寮傚父
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            LogCrash(ex, "AppDomain.UnhandledException");

            // 灏濊瘯淇濆瓨鐘舵€?            try
            {
                _settings?.Save();
            }
            catch { }

            // 濡傛灉鏄弗閲嶉敊璇紝璁板綍浣嗕笉闃绘搴旂敤閫€鍑?            if (e.IsTerminating)
            {
                // 璁板綍鏃ュ織渚涗笅娆″惎鍔ㄥ垎鏋?            }
        };

        // 鎹曡幏 UI 绾跨▼寮傚父
        this.DispatcherUnhandledException += (sender, e) =>
        {
            LogCrash(e.Exception, "DispatcherUnhandledException");

            // 灏濊瘯鎭㈠锛氫笉璁╁簲鐢ㄥ穿婧?            e.Handled = true;

            // 灏濊瘯鎭㈠ UI 鐘舵€?            try
            {
                // 濡傛灉涓荤獥鍙ｅ瓨鍦紝灏濊瘯閲嶆柊鏄剧ず
                if (_mainWindow != null && !_mainWindow.IsVisible)
                {
                    _mainWindow.Show();
                }
            }
            catch { }
        };

        // Task 寮傚父锛堟湭瑙傚療鐨勶級
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LogCrash(e.Exception, "UnobservedTaskException");
            e.SetObserved(); // 闃绘杩涚▼缁堟
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
            // 鏃ュ織鍐欏叆澶辫触锛岄潤榛樺拷鐣?        }
    }

    #endregion

    #region 绯荤粺涓婚妫€娴?
    private void SetupThemeWatcher()
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // 绯荤粺涓婚鍙兘宸插彉鍖栵紝閫氱煡鐏靛姩宀?                _bus?.Publish(new IslandEvent(
                    "system", "涓婚鍙樻洿",
                    GetSystemTheme(), "info"));
            }
        };
    }

    /// <summary>
    /// 鑾峰彇褰撳墠绯荤粺涓婚锛欴ark 鎴?Light
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
    /// 鑾峰彇绯荤粺涓婚瀵瑰簲鐨勮儗鏅壊
    /// </summary>
    public static string GetThemeBackgroundColor()
    {
        return GetSystemTheme() == "Dark" ? "#E8202022" : "#E0F0F0F5";
    }

    #endregion

    #region 鎵樼洏鍥炬爣

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "FluidBar",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        var settingsItem = new ToolStripMenuItem("璁剧疆");
        settingsItem.Click += (_, _) => OpenSettings();
        var exitItem = new ToolStripMenuItem("閫€鍑?);
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

        // 閲嶇疆閫忔槑搴︼紙娣″嚭鍔ㄧ敾鍙兘灏嗗叾璁句负 0锛?        _settingsWindow.BeginAnimation(UIElement.OpacityProperty, null);
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


