using System.Diagnostics;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// CPU 浣跨敤鐜囩洃鎺?/// </summary>
public sealed class CpuMonitor : ISystemMonitor
{
    public string Id => "cpu";
    public string Name => "CPU";
    public string Description => "澶勭悊鍣ㄤ娇鐢ㄧ巼鐩戞帶";
    public string Icon => "睽?; // Segoe MDL2 Processor
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

        // 寤惰繜鍒濆鍖?PerformanceCounter锛堥娆″垱寤洪渶瑕?~1 绉掞級
        if (!_counterInitialized)
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // 棣栨璋冪敤杩斿洖 0锛岄渶瑕侀鐑?                _counterInitialized = true;
            }
            catch
            {
                // PerformanceCounter 涓嶅彲鐢紙濡傛煇浜涚簿绠€绯荤粺锛夛紝闈欓粯闄嶇骇
                Enabled = false;
                return;
            }
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => CheckCpu();
        _timer.Start();

        // 棣栨妫€鏌ュ欢杩?1 绉掞紙绛夊緟 PerformanceCounter 棰勭儹锛?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckCpu();
            };
            _timer.Start();
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

            // 蹇界暐寮傚父鍊?            if (percent < 0 || percent > 100)
                return;

            // 浠呭湪鍙樺寲瓒呰繃 5% 鎴栬秴杩囬槇鍊兼椂瑙﹀彂
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
                    title = $"CPU 鍗犵敤 {percent:F0}%";
                    content = "绯荤粺璐熻浇杈冮珮";
                }
                else if (percent >= 70)
                {
                    iconKind = "cpu";
                    title = $"CPU 鍗犵敤 {percent:F0}%";
                    content = "绯荤粺绻佸繖";
                }
                else
                {
                    iconKind = "cpu";
                    title = $"CPU {percent:F0}%";
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
        _cpuCounter?.Dispose();
    }
}

