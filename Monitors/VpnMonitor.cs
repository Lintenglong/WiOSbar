using System.Management;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// VPN 鐘舵€佺洃鎺у櫒
/// </summary>
public sealed class VpnMonitor : ISystemMonitor
{
    public string Id => "vpn";
    public string Name => "VPN";
    public string Description => "VPN 杩炴帴鐘舵€佺洃鎺?;
    public string Icon => "顪?; // Segoe MDL2 Lock
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private bool _lastVpnState;
    private bool _firstCheck = true;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += (_, _) => CheckVpnStatus();
        _timer.Start();

        // 棣栨寤惰繜妫€鏌?        = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
_timer.Tick += (_, _) => {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckVpnStatus();
            };
            _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void CheckVpnStatus()
    {
        if (!_isRunning)
            return;

        try
        {
            var hasVpn = DetectVpnConnection();
            var stateChanged = hasVpn != _lastVpnState;

            if (stateChanged || _firstCheck)
            {
                if (hasVpn)
                {
                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: "VPN 宸茶繛鎺?,
                        Content: "瀹夊叏杩炴帴宸插缓绔?,
                        IconKind: "vpn"));
                }
                else if (!_firstCheck)
                {
                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: "VPN 宸叉柇寮€",
                        Content: "杩炴帴宸叉柇寮€",
                        IconKind: "vpn"));
                }

                _lastVpnState = hasVpn;
                _firstCheck = false;
            }
        }
        catch
        {
            // 闈欓粯澶辫触
        }
    }

    private static bool DetectVpnConnection()
    {
        try
        {
            // 鏂规硶 1锛氭鏌ョ綉缁滄帴鍙ｇ被鍨?            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // VPN 閫氬父浣跨敤 PPP 鎴?Tunnel 鎺ュ彛
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ppp ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    // 鎺掗櫎鍥炵幆鍜屼互澶綉
                    if (!ni.Description.Contains("Loopback", StringComparison.OrdinalIgnoreCase) &&
                        !ni.Description.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) &&
                        !ni.Description.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 鏂规硶 2锛氭鏌ヨ矾鐢辫〃涓殑 VPN 缃戝叧锛堢畝鍖栵級
            // 瀹為檯鐢熶骇鐜鍙兘闇€瑕佹洿澶嶆潅鐨勬娴嬮€昏緫

            return false;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}


