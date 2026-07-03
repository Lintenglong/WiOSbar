using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 蓝牙设备电量监控器 - 监控耳机、手柄等设备的电量
/// </summary>
public sealed class BluetoothBatteryMonitor : ISystemMonitor
{
    public string Id => "bluetooth_battery";
    public string Name => "蓝牙电量";
    public string Description => "蓝牙设备电量监控";
    public string Icon => "🔋";
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private readonly Dictionary<string, int> _lastBatteryLevels = new();

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) }; // 5 分钟检查一次
        _timer.Tick += (_, _) => CheckBluetoothBattery();
        _timer.Start();

        // 首次延迟 10 秒检查
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckBluetoothBattery();
            };
            t.Start();
        });
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void CheckBluetoothBattery()
    {
        if (!_isRunning)
            return;

        try
        {
            // 使用 WMI 查询蓝牙设备
            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Bluetooth'");

            foreach (var device in searcher.Get())
            {
                var deviceName = device["Name"]?.ToString() ?? "Unknown Device";
                var deviceId = device["DeviceID"]?.ToString() ?? "";

                // 尝试获取电量（某些蓝牙设备支持）
                var batteryLevel = GetBatteryLevel(deviceId);

                if (batteryLevel.HasValue)
                {
                    // 检查是否需要触发事件
                    if (ShouldTriggerEvent(deviceName, batteryLevel.Value))
                    {
                        var iconKind = batteryLevel.Value <= 20 ? "battery_low" : "battery";

                        EventTriggered?.Invoke(new IslandEvent(
                            Source: Id,
                            Title: $"{deviceName}",
                            Content: $"电量 {batteryLevel.Value}%",
                            IconKind: iconKind));
                    }

                    _lastBatteryLevels[deviceName] = batteryLevel.Value;
                }
            }
        }
        catch
        {
            // 静默失败（WMI 可能不可用）
        }
    }

    private int? GetBatteryLevel(string deviceId)
    {
        try
        {
            // 尝试通过 WMI 获取电池信息
            // 注意：这需要设备支持电池报告
            var searcher = new ManagementObjectSearcher(
                $"SELECT * FROM Win32_Battery WHERE DeviceID LIKE '%{deviceId.Split('&').LastOrDefault()}%'");

            foreach (var battery in searcher.Get())
            {
                var estimatedCharge = battery["EstimatedChargeRemaining"];
                if (estimatedCharge != null)
                {
                    return Convert.ToInt32(estimatedCharge);
                }
            }
        }
        catch { }

        return null;
    }

    private bool ShouldTriggerEvent(string deviceName, int batteryLevel)
    {
        // 低电量警告（<= 20%）
        if (batteryLevel <= 20)
        {
            if (!_lastBatteryLevels.ContainsKey(deviceName) ||
                _lastBatteryLevels[deviceName] > 20)
            {
                return true;
            }
        }

        // 电量变化超过 10%
        if (_lastBatteryLevels.TryGetValue(deviceName, out var lastLevel))
        {
            return Math.Abs(batteryLevel - lastLevel) >= 10;
        }

        // 首次检测到设备
        return true;
    }

    public void Dispose()
    {
        Stop();
    }
}
