using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 磁盘健康监控器 - 监控磁盘SMART状态和健康度
/// </summary>
public sealed class DiskHealthMonitor : ISystemMonitor
{
    public string Id => "disk_health";
    public string Name => "磁盘健康";
    public string Description => "磁盘SMART健康状态监控";
    public string Icon => "💾";
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private readonly Dictionary<string, string> _lastHealthStatus = new();

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) }; // 每小时检查一次
        _timer.Tick += (_, _) => CheckDiskHealth();
        _timer.Start();

        // 首次延迟 30 秒检查
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckDiskHealth();
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

    private void CheckDiskHealth()
    {
        if (!_isRunning)
            return;

        try
        {
            // 查询物理磁盘
            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_DiskDrive");

            foreach (var disk in searcher.Get())
            {
                var model = disk["Model"]?.ToString() ?? "Unknown Disk";
                var deviceId = disk["DeviceID"]?.ToString() ?? "";
                var status = disk["Status"]?.ToString() ?? "Unknown";

                // 检查健康状态变化
                if (ShouldTriggerEvent(deviceId, status, model))
                {
                    var iconKind = status.Equals("OK", StringComparison.OrdinalIgnoreCase)
                        ? "disk_healthy"
                        : "disk_warning";

                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: model.Length > 30 ? model.Substring(0, 30) + "..." : model,
                        Content: $"状态: {status}",
                        IconKind: iconKind));
                }

                _lastHealthStatus[deviceId] = status;
            }
        }
        catch
        {
            // 某些系统可能不支持，静默失败
        }
    }

    private bool ShouldTriggerEvent(string deviceId, string currentStatus, string model)
    {
        // 首次检测到磁盘
        if (!_lastHealthStatus.ContainsKey(deviceId))
        {
            // 仅在状态异常时触发
            return !currentStatus.Equals("OK", StringComparison.OrdinalIgnoreCase);
        }

        // 状态变化
        var lastStatus = _lastHealthStatus[deviceId];
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
