using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 钃濈墮璁惧鐢甸噺鐩戞帶鍣?- 鐩戞帶鑰虫満銆佹墜鏌勭瓑璁惧鐨勭數閲?/// </summary>
public sealed class BluetoothBatteryMonitor : ISystemMonitor
{
    public string Id => "bluetooth_battery";
    public string Name => "钃濈墮鐢甸噺";
    public string Description => "钃濈墮璁惧鐢甸噺鐩戞帶";
    public string Icon => "馃攱";
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private readonly Dictionary<string, int> _lastBatteryLevels = new();

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) }; // 5 鍒嗛挓妫€鏌ヤ竴娆?        _timer.Tick += (_, _) => CheckBluetoothBattery();
        _timer.Start();

        // 棣栨寤惰繜 10 绉掓鏌?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckBluetoothBattery();
            };
            _timer.Start();
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
            // 浣跨敤 WMI 鏌ヨ钃濈墮璁惧
            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Bluetooth'");

            foreach (var device in searcher.Get())
            {
                var deviceName = device["Name"]?.ToString() ?? "Unknown Device";
                var deviceId = device["DeviceID"]?.ToString() ?? "";

                // 灏濊瘯鑾峰彇鐢甸噺锛堟煇浜涜摑鐗欒澶囨敮鎸侊級
                var batteryLevel = GetBatteryLevel(deviceId);

                if (batteryLevel.HasValue)
                {
                    // 妫€鏌ユ槸鍚﹂渶瑕佽Е鍙戜簨浠?                    if (ShouldTriggerEvent(deviceName, batteryLevel.Value))
                    {
                        var iconKind = batteryLevel.Value <= 20 ? "battery_low" : "battery";

                        EventTriggered?.Invoke(new IslandEvent(
                            Source: Id,
                            Title: $"{deviceName}",
                            Content: $"鐢甸噺 {batteryLevel.Value}%",
                            IconKind: iconKind));
                    }

                    _lastBatteryLevels[deviceName] = batteryLevel.Value;
                }
            }
        }
        catch
        {
            // 闈欓粯澶辫触锛圵MI 鍙兘涓嶅彲鐢級
        }
    }

    private int? GetBatteryLevel(string deviceId)
    {
        try
        {
            // 灏濊瘯閫氳繃 WMI 鑾峰彇鐢垫睜淇℃伅
            // 娉ㄦ剰锛氳繖闇€瑕佽澶囨敮鎸佺數姹犳姤鍛?            var searcher = new ManagementObjectSearcher(
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
        // 浣庣數閲忚鍛婏紙<= 20%锛?        if (batteryLevel <= 20)
        {
            if (!_lastBatteryLevels.ContainsKey(deviceName) ||
                _lastBatteryLevels[deviceName] > 20)
            {
                return true;
            }
        }

        // 鐢甸噺鍙樺寲瓒呰繃 10%
        if (_lastBatteryLevels.TryGetValue(deviceName, out var lastLevel))
        {
            return Math.Abs(batteryLevel - lastLevel) >= 10;
        }

        // 棣栨妫€娴嬪埌璁惧
        return true;
    }

    public void Dispose()
    {
        Stop();
    }
}

