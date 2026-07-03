using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 磁盘活动监控
/// </summary>
public sealed class DiskMonitor : ISystemMonitor
{
    public string Id => "disk";
    public string Name => "磁盘";
    public string Description => "磁盘读写活动监控";
    public string Icon => ""; // Segoe MDL2 HardDrive
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private float _lastReadBytes;
    private float _lastWriteBytes;
    private PerformanceCounter? _diskReadCounter;
    private PerformanceCounter? _diskWriteCounter;
    private bool _counterInitialized;
    private string _systemDrive = "C";
    private int _isChecking;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        try
        {
            _systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C";
        }
        catch
        {
            _systemDrive = "C";
        }

        _ = InitializeCountersAsync();
    }

    private async Task InitializeCountersAsync()
    {
        if (!_counterInitialized)
        {
            try
            {
                await Task.Run(() =>
                {
                    var instanceName = $"{_systemDrive}:";
                    _diskReadCounter = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", instanceName);
                    _diskWriteCounter = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", instanceName);
                    _diskReadCounter.NextValue();
                    _diskWriteCounter.NextValue();
                    _counterInitialized = true;
                }).ConfigureAwait(false);
            }
            catch
            {
                Enabled = false;
                _isRunning = false;
                return;
            }
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            StartTimer();
        else
            dispatcher.BeginInvoke(new Action(StartTimer));
    }

    private void StartTimer()
    {
        if (!_isRunning || _timer != null)
            return;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _timer.Tick += (_, _) => QueueCheckDisk();
        _timer.Start();

        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                QueueCheckDisk();
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

    private void QueueCheckDisk()
    {
        if (!_isRunning || System.Threading.Interlocked.Exchange(ref _isChecking, 1) == 1)
            return;

        _ = Task.Run(() =>
        {
            try { CheckDisk(); }
            finally { System.Threading.Interlocked.Exchange(ref _isChecking, 0); }
        });
    }
    private void CheckDisk()
    {
        if (!_isRunning || _diskReadCounter == null || _diskWriteCounter == null)
            return;

        try
        {
            var readBytes = _diskReadCounter.NextValue();
            var writeBytes = _diskWriteCounter.NextValue();

            // 转换为 MB/s
            var readMB = readBytes / (1024 * 1024);
            var writeMB = writeBytes / (1024 * 1024);
            var totalMB = readMB + writeMB;

            // 仅在磁盘活动显著（> 5MB/s）或变化大时触发
            var prevTotal = (_lastReadBytes + _lastWriteBytes) / (1024 * 1024);
            var shouldTrigger = totalMB > 5 && Math.Abs(totalMB - prevTotal) > 3;

            if (shouldTrigger)
            {
                _lastReadBytes = readBytes;
                _lastWriteBytes = writeBytes;

                string iconKind, title, content;

                if (totalMB > 50)
                {
                    iconKind = "disk_active";
                    title = "磁盘繁忙";
                    content = $"读 {readMB:F1} / 写 {writeMB:F1} MB/s";
                }
                else if (totalMB > 20)
                {
                    iconKind = "disk";
                    title = "磁盘活动";
                    content = $"读 {readMB:F1} / 写 {writeMB:F1} MB/s";
                }
                else
                {
                    iconKind = "disk";
                    title = "磁盘";
                    content = $"{totalMB:F0} MB/s";
                }

                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: title,
                    Content: content,
                    IconKind: iconKind));
            }
        }
        catch
        {
            // 静默失败
        }
    }

    public void Dispose()
    {
        Stop();
        _diskReadCounter?.Dispose();
        _diskWriteCounter?.Dispose();
    }
}
