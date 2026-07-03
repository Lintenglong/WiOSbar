using System.Diagnostics;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// CPU 使用率监控
/// </summary>
public sealed class CpuMonitor : ISystemMonitor
{
    public string Id => "cpu";
    public string Name => "CPU";
    public string Description => "处理器使用率监控";
    public string Icon => ""; // Segoe MDL2 Processor
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private float _lastPercent = -1;
    private PerformanceCounter? _cpuCounter;
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
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _cpuCounter.NextValue();
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

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => QueueCheckCpu();
        _timer.Start();

        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                QueueCheckCpu();
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

    private void QueueCheckCpu()
    {
        if (!_isRunning || System.Threading.Interlocked.Exchange(ref _isChecking, 1) == 1)
            return;

        _ = Task.Run(() =>
        {
            try { CheckCpu(); }
            finally { System.Threading.Interlocked.Exchange(ref _isChecking, 0); }
        });
    }
    private void CheckCpu()
    {
        if (!_isRunning || _cpuCounter == null)
            return;

        try
        {
            var percent = _cpuCounter.NextValue();
            if (percent < 0 || percent > 100)
                return;

            var previous = _lastPercent;
            _lastPercent = percent;
            if (previous < 0)
                return;

            var crossedHigh = previous < 85 && percent >= 85;
            var crossedCritical = previous < 95 && percent >= 95;
            if (!crossedHigh && !crossedCritical)
                return;

            var iconKind = percent >= 95 ? "cpu_high" : "cpu";
            var title = $"CPU {percent:F0}%";
            var content = percent >= 95 ? "系统负载很高" : "CPU 占用偏高";
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
        _cpuCounter?.Dispose();
    }
}
