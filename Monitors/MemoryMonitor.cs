using System.Diagnostics;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 鍐呭瓨鍗犵敤鐩戞帶
/// </summary>
public sealed class MemoryMonitor : ISystemMonitor
{
    public string Id => "memory";
    public string Name => "鍐呭瓨";
    public string Description => "鍐呭瓨浣跨敤鐜囩洃鎺?;
    public string Icon => "瞍€"; // Segoe MDL2 Memory
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private float _lastPercent = -1;
    private PerformanceCounter? _memoryCounter;
    private bool _counterInitialized;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        if (!_counterInitialized)
        {
            try
            {
                _memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                _memoryCounter.NextValue();
                _counterInitialized = true;
            }
            catch
            {
                Enabled = false;
                return;
            }
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (_, _) => CheckMemory();
        _timer.Start();

        // 棣栨寤惰繜妫€鏌?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckMemory();
            };
            _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
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

            // 鍐呭瓨鍙樺寲闃堝€硷細3%锛岃鍛婇槇鍊硷細85%/95%
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
                    title = $"鍐呭瓨鍗犵敤 {percent:F0}%";
                    content = "鍐呭瓨涓嶈冻锛屽缓璁叧闂簲鐢?;
                }
                else if (percent >= 85)
                {
                    iconKind = "memory";
                    title = $"鍐呭瓨鍗犵敤 {percent:F0}%";
                    content = "鍐呭瓨鍗犵敤杈冮珮";
                }
                else
                {
                    iconKind = "memory";
                    title = $"鍐呭瓨 {percent:F0}%";
                    content = "杩愯姝ｅ父";
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
            // 闈欓粯澶辫触
        }
    }

    public void Dispose()
    {
        Stop();
        _memoryCounter?.Dispose();
    }
}

