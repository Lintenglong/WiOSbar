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

            var previous = _lastPercent;
            _lastPercent = percent;
            if (previous < 0)
                return;

            var crossedHigh = previous < 88 && percent >= 88;
            var crossedCritical = previous < 96 && percent >= 96;
            if (!crossedHigh && !crossedCritical)
                return;

            var iconKind = percent >= 96 ? "memory_high" : "memory";
            var title = $"Memory {percent:F0}%";
            var content = percent >= 96 ? "内存压力很高" : "内存占用偏高";
            EventTriggered?.Invoke(new IslandEvent(
                Source: Id,
                Title: title,
                Content: content,
                IconKind: iconKind));
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        Stop();
        _memoryCounter?.Dispose();
    }
}
