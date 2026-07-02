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

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        // 延迟初始化 PerformanceCounter（首次创建需要 ~1 秒）
        if (!_counterInitialized)
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // 首次调用返回 0，需要预热
                _counterInitialized = true;
            }
            catch
            {
                // PerformanceCounter 不可用（如某些精简系统），静默降级
                Enabled = false;
                return;
            }
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => CheckCpu();
        _timer.Start();

        // 首次检查延迟 1 秒（等待 PerformanceCounter 预热）
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckCpu();
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

    private void CheckCpu()
    {
        if (!_isRunning || _cpuCounter == null)
            return;

        try
        {
            var percent = _cpuCounter.NextValue();

            // 忽略异常值
            if (percent < 0 || percent > 100)
                return;

            // 仅在变化超过 5% 或超过阈值时触发
            var shouldTrigger = Math.Abs(percent - _lastPercent) > 5 ||
                               (percent > 80 && _lastPercent <= 80) ||
                               (percent > 90 && _lastPercent <= 90);

            if (shouldTrigger || _lastPercent < 0)
            {
                _lastPercent = percent;

                string iconKind, title, content;
                var iconColor = "cpu";

                if (percent >= 90)
                {
                    iconKind = "cpu_high";
                    title = $"CPU 占用 {percent:F0}%";
                    content = "系统负载较高";
                }
                else if (percent >= 70)
                {
                    iconKind = "cpu";
                    title = $"CPU 占用 {percent:F0}%";
                    content = "系统繁忙";
                }
                else
                {
                    iconKind = "cpu";
                    title = $"CPU {percent:F0}%";
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
        _cpuCounter?.Dispose();
    }
}
