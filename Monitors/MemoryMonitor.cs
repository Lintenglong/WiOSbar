using System.Diagnostics;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 内存占用监控
/// </summary>
public sealed class MemoryMonitor : ISystemMonitor
{
    public string Id => "memory";
    public string Name => "内存";
    public string Description => "内存使用率监控";
    public string Icon => ""; // Segoe MDL2 Memory
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private float _lastPercent = -1;
    private PerformanceCounter? _memoryCounter;
    private bool _counterInitialized;
    private int _isChecking;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _ = InitializeCounterAsync();
    }

    private async Task InitializeCounterAsync()
    {
        if (!_counterInitialized)
        {
            try
            {
                await Task.Run(() =>
                {
                    _memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                    _memoryCounter.NextValue();
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

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (_, _) => QueueCheckMemory();
        _timer.Start();

        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                QueueCheckMemory();
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

    private void QueueCheckMemory()
    {
        if (!_isRunning || System.Threading.Interlocked.Exchange(ref _isChecking, 1) == 1)
            return;

        _ = Task.Run(() =>
        {
            try { CheckMemory(); }
            finally { System.Threading.Interlocked.Exchange(ref _isChecking, 0); }
        });
    }
    private void CheckMemory()
    {
        if (!_isRunning || _memoryCounter == null)
            return;

        try
        {
            var percent = _memoryCounter.NextValue();

            if (percent < 0 || percent > 100)
                return;

            // 内存变化阈值：3%，警告阈值：85%/95%
            var shouldTrigger = Math.Abs(percent - _lastPercent) > 3 ||
                               (percent > 85 && _lastPercent <= 85) ||
                               (percent > 95 && _lastPercent <= 95);

            if (shouldTrigger || _lastPercent < 0)
            {
                _lastPercent = percent;

                string iconKind, title, content;

                if (percent >= 95)
                {
                    iconKind = "memory_high";
                    title = $"内存占用 {percent:F0}%";
                    content = "内存不足，建议关闭应用";
                }
                else if (percent >= 85)
                {
                    iconKind = "memory";
                    title = $"内存占用 {percent:F0}%";
                    content = "内存占用较高";
                }
                else
                {
                    iconKind = "memory";
                    title = $"内存 {percent:F0}%";
                    content = "运行正常";
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
        _memoryCounter?.Dispose();
    }
}
