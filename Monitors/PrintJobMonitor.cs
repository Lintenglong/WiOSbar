using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 鎵撳嵃浠诲姟鐩戞帶鍣?- 鐩戞帶鎵撳嵃闃熷垪娲诲姩
/// </summary>
public sealed class PrintJobMonitor : ISystemMonitor
{
    public string Id => "print";
    public string Name => "鎵撳嵃";
    public string Description => "鎵撳嵃浠诲姟鐘舵€佺洃鎺?;
    public string Icon => "瞑?; // Segoe MDL2 Print
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

        // 棣栨寤惰繜妫€鏌?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckPrintJobs();
            };
            _timer.Start();
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

            // 妫€娴嬫墦鍗颁换鍔″彉鍖?            if (jobCount > 0 && !_hasActiveJobs)
            {
                // 鏂版墦鍗颁换鍔″紑濮?                _hasActiveJobs = true;
                var firstJob = jobs.FirstOrDefault();
                var document = firstJob?["Document"]?.ToString() ?? "Unknown";
                var printer = firstJob?["Name"]?.ToString() ?? "Printer";

                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: "鎵撳嵃涓?,
                    Content: $"{document}",
                    IconKind: "print"));
            }
            else if (jobCount == 0 && _hasActiveJobs)
            {
                // 鎵撳嵃瀹屾垚
                _hasActiveJobs = false;
                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: "鎵撳嵃瀹屾垚",
                    Content: "鎵€鏈変换鍔″凡瀹屾垚",
                    IconKind: "print"));
            }
            else if (jobCount != _lastJobCount && jobCount > 0)
            {
                // 浠诲姟鏁伴噺鍙樺寲
                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: "鎵撳嵃闃熷垪",
                    Content: $"{jobCount} 涓换鍔?,
                    IconKind: "print"));
            }

            _lastJobCount = jobCount;
        }
        catch
        {
            // 闈欓粯澶辫触锛堟煇浜涚郴缁熷彲鑳界鐢?WMI锛?        }
    }

    public void Dispose()
    {
        Stop();
    }
}

