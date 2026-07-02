using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 缃戠粶閫熷害鐩戞帶鍣?- 瀹炴椂鏄剧ず涓婁紶/涓嬭浇閫熺巼
/// </summary>
public sealed class NetworkSpeedMonitor : ISystemMonitor
{
    public string Id => "network_speed";
    public string Name => "缃戠粶閫熷害";
    public string Description => "瀹炴椂缃戠粶涓婁紶/涓嬭浇閫熺巼";
    public string Icon => "顪?; // Segoe MDL2 Network
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTime _lastSampleTime = DateTime.UtcNow;
    private bool _firstSample = true;

    // 缃戝崱缂撳瓨锛堥伩鍏嶆瘡娆￠兘鏋氫妇锛?    private NetworkInterface? _activeInterface;
    private DateTime _interfaceCacheTime = DateTime.MinValue;
    private static readonly TimeSpan InterfaceCacheTtl = TimeSpan.FromSeconds(30);

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => SampleNetworkSpeed();
        _timer.Start();

        // 棣栨寤惰繜 1 绉掗噰鏍?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                SampleNetworkSpeed();
            };
            _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void SampleNetworkSpeed()
    {
        if (!_isRunning)
            return;

        try
        {
            var ni = GetActiveNetworkInterface();
            if (ni == null)
                return;

            var stats = ni.GetIPv4Statistics();
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastSampleTime).TotalSeconds;

            if (elapsed < 0.5)
                return; // 閬垮厤閲囨牱杩囧瘑

            var bytesReceived = stats.BytesReceived;
            var bytesSent = stats.BytesSent;

            if (!_firstSample && elapsed > 0)
            {
                var downloadBps = (bytesReceived - _lastBytesReceived) / elapsed;
                var uploadBps = (bytesSent - _lastBytesSent) / elapsed;

                // 杞崲涓?KB/s 鎴?MB/s
                var downloadKbps = downloadBps / 1024.0;
                var uploadKbps = uploadBps / 1024.0;

                // 浠呭湪鏈夋樉钁楁椿鍔ㄦ椂瑙﹀彂锛? 10 KB/s锛?                if (downloadKbps > 10 || uploadKbps > 10)
                {
                    var downloadStr = FormatSpeed(downloadKbps);
                    var uploadStr = FormatSpeed(uploadKbps);

                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: "缃戠粶",
                        Content: $"鈫?{downloadStr}  鈫?{uploadStr}",
                        IconKind: "network"));
                }
            }

            _lastBytesReceived = bytesReceived;
            _lastBytesSent = bytesSent;
            _lastSampleTime = now;
            _firstSample = false;
        }
        catch
        {
            // 闈欓粯澶辫触
        }
    }

    private static string FormatSpeed(double kbps)
    {
        if (kbps >= 1024)
        {
            return $"{kbps / 1024:F1} MB/s";
        }
        return $"{kbps:F0} KB/s";
    }

    private NetworkInterface? GetActiveNetworkInterface()
    {
        // 浣跨敤缂撳瓨
        if (_activeInterface != null &&
            (DateTime.UtcNow - _interfaceCacheTime) < InterfaceCacheTtl)
        {
            return _activeInterface;
        }

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 浼樺厛閫夋嫨宸茶繛鎺ョ殑浠ュお缃戞垨 WiFi
            var active = interfaces.FirstOrDefault(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                 ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                ni.GetIPv4Statistics().BytesReceived > 0);

            if (active == null)
            {
                // 闄嶇骇锛氫换鎰忓凡杩炴帴鐨勬帴鍙?                active = interfaces.FirstOrDefault(ni =>
                    ni.OperationalStatus == OperationalStatus.Up);
            }

            _activeInterface = active;
            _interfaceCacheTime = DateTime.UtcNow;

            return active;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

