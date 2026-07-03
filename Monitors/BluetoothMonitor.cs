using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 蓝牙设备监控 - 设备连接/断开
/// </summary>
public sealed class BluetoothMonitor : ISystemMonitor
{
    public string Id => "bluetooth";
    public string Name => "蓝牙设备";
    public string Description => "蓝牙设备连接/断开时提示";
    public string Icon => "\uE702"; // Segoe MDL2 Bluetooth
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private HashSet<string> _lastDevices = new();
    private bool _isRunning;
    private int _isChecking;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _ = RefreshInitialDevicesAsync();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timer.Tick += (_, _) => QueueCheckDevices();
        _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private async Task RefreshInitialDevicesAsync()
    {
        var devices = await Task.Run(GetConnectedDevices).ConfigureAwait(false);
        if (_isRunning)
            _lastDevices = devices;
    }

    private void QueueCheckDevices()
    {
        if (!_isRunning || System.Threading.Interlocked.Exchange(ref _isChecking, 1) == 1)
            return;

        _ = Task.Run(() =>
        {
            try { CheckDevices(); }
            finally { System.Threading.Interlocked.Exchange(ref _isChecking, 0); }
        });
    }
    private void CheckDevices()
    {
        try
        {
            var current = GetConnectedDevices();
            var added = current.Except(_lastDevices).ToList();
            var removed = _lastDevices.Except(current).ToList();

            foreach (var device in added)
            {
                EventTriggered?.Invoke(new IslandEvent(
                    Id, "蓝牙已连接", device, "bluetooth"));
            }

            foreach (var device in removed)
            {
                EventTriggered?.Invoke(new IslandEvent(
                    Id, "蓝牙已断开", device, "bluetooth"));
            }

            _lastDevices = current;
        }
        catch { }
    }

    private static HashSet<string> GetConnectedDevices()
    {
        var devices = new HashSet<string>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Service = 'BthEnum' " +
                "OR Service = 'BthLEEnum' OR Service = 'HidBth'");

            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && !name.Contains("Enumerator"))
                    devices.Add(name);
            }
        }
        catch { }
        return devices;
    }

    public void Dispose() => Stop();
}
