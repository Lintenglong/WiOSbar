using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 纾佺洏鍋ュ悍鐩戞帶鍣?- 鐩戞帶纾佺洏SMART鐘舵€佸拰鍋ュ悍搴?/// </summary>
public sealed class DiskHealthMonitor : ISystemMonitor
{
    public string Id => "disk_health";
    public string Name => "纾佺洏鍋ュ悍";
    public string Description => "纾佺洏SMART鍋ュ悍鐘舵€佺洃鎺?;
    public string Icon => "馃捑";
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private readonly Dictionary<string, string> _lastHealthStatus = new();

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) }; // 姣忓皬鏃舵鏌ヤ竴娆?        _timer.Tick += (_, _) => CheckDiskHealth();
        _timer.Start();

        // 棣栨寤惰繜 30 绉掓鏌?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckDiskHealth();
            };
            _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void CheckDiskHealth()
    {
        if (!_isRunning)
            return;

        try
        {
            // 鏌ヨ鐗╃悊纾佺洏
            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_DiskDrive");

            foreach (var disk in searcher.Get())
            {
                var model = disk["Model"]?.ToString() ?? "Unknown Disk";
                var deviceId = disk["DeviceID"]?.ToString() ?? "";
                var status = disk["Status"]?.ToString() ?? "Unknown";

                // 妫€鏌ュ仴搴风姸鎬佸彉鍖?                if (ShouldTriggerEvent(deviceId, status, model))
                {
                    var iconKind = status.Equals("OK", StringComparison.OrdinalIgnoreCase)
                        ? "disk_healthy"
                        : "disk_warning";

                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: model.Length > 30 ? model.Substring(0, 30) + "..." : model,
                        Content: $"鐘舵€? {status}",
                        IconKind: iconKind));
                }

                _lastHealthStatus[deviceId] = status;
            }
        }
        catch
        {
            // 鏌愪簺绯荤粺鍙兘涓嶆敮鎸侊紝闈欓粯澶辫触
        }
    }

    private bool ShouldTriggerEvent(string deviceId, string currentStatus, string model)
    {
        // 棣栨妫€娴嬪埌纾佺洏
        if (!_lastHealthStatus.ContainsKey(deviceId))
        {
            // 浠呭湪鐘舵€佸紓甯告椂瑙﹀彂
            return !currentStatus.Equals("OK", StringComparison.OrdinalIgnoreCase);
        }

        // 鐘舵€佸彉鍖?        var lastStatus = _lastHealthStatus[deviceId];
        if (lastStatus != currentStatus)
        {
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}

