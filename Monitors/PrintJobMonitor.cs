using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 打印任务监控器 - 监控打印队列活动
/// </summary>
public sealed class PrintJobMonitor : ISystemMonitor
{
    public string Id => "print";
    public string Name => "打印";
    public string Description => "打印任务状态监控";
    public string Icon => ""; // Segoe MDL2 Print
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private int _lastJobCount;
    private bool _hasActiveJobs;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (_, _) => CheckPrintJobs();
        _timer.Start();

        // 首次延迟检查
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckPrintJobs();
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

    private void CheckPrintJobs()
    {
        if (!_isRunning)
            return;

        try
        {
            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PrintJob");

            var jobs = searcher.Get().Cast<ManagementObject>().ToList();
            var jobCount = jobs.Count;

            // 检测打印任务变化
            if (jobCount > 0 && !_hasActiveJobs)
            {
                // 新打印任务开始
                _hasActiveJobs = true;
                var firstJob = jobs.FirstOrDefault();
                var document = firstJob?["Document"]?.ToString() ?? "Unknown";
                var printer = firstJob?["Name"]?.ToString() ?? "Printer";

                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: "打印中",
                    Content: $"{document}",
                    IconKind: "print"));
            }
            else if (jobCount == 0 && _hasActiveJobs)
            {
                // 打印完成
                _hasActiveJobs = false;
                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: "打印完成",
                    Content: "所有任务已完成",
                    IconKind: "print"));
            }
            else if (jobCount != _lastJobCount && jobCount > 0)
            {
                // 任务数量变化
                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: "打印队列",
                    Content: $"{jobCount} 个任务",
                    IconKind: "print"));
            }

            _lastJobCount = jobCount;
        }
        catch
        {
            // 静默失败（某些系统可能禁用 WMI）
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
