namespace FluidBar.Monitors;

/// <summary>
/// 系统监控管理器 - 管理所有内置系统监控
/// </summary>
public sealed class SystemMonitorManager : IDisposable
{
    private readonly List<ISystemMonitor> _monitors = new();
    private readonly EventBus _bus;
    private readonly FluidBarSettings? _settings;
    private readonly bool _persistSettings;
    private readonly System.Windows.Threading.Dispatcher? _dispatcher;

    public IReadOnlyList<ISystemMonitor> Monitors => _monitors;

    public SystemMonitorManager(EventBus bus, FluidBarSettings? settings = null, bool persistSettings = true)
    {
        _bus = bus;
        _settings = settings;
        _persistSettings = persistSettings;
        _dispatcher = System.Windows.Application.Current?.Dispatcher;
    }

    public void Register(ISystemMonitor monitor)
    {
        monitor.Enabled = _settings?.IsMonitorEnabled(monitor.Id, monitor.Enabled)
            ?? monitor.Enabled;
        monitor.EventTriggered += PublishOnUiThread;
        _monitors.Add(monitor);
    }

    public void StartAll()
    {
        _ = StartEnabledMonitorsAsync();
    }

    private async Task StartEnabledMonitorsAsync()
    {
        foreach (var monitor in _monitors)
        {
            if (!monitor.Enabled)
                continue;

            monitor.Start();
            await Task.Delay(120);
        }
    }

    public void SetEnabled(ISystemMonitor monitor, bool enabled)
    {
        monitor.Enabled = enabled;
        _settings?.SetMonitorEnabled(monitor.Id, enabled);
        if (_persistSettings)
            _settings?.Save();

        if (enabled) monitor.Start();
        else monitor.Stop();
    }

    private void PublishOnUiThread(IslandEvent evt)
    {
        var dispatcher = _dispatcher ?? System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            _bus.Publish(evt);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => _bus.Publish(evt)));
    }
    public void Dispose()
    {
        foreach (var m in _monitors)
            m.Dispose();
    }
}

/// <summary>
/// 系统监控接口
/// </summary>
public interface ISystemMonitor : IDisposable
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Icon { get; }
    bool Enabled { get; set; }
    event Action<IslandEvent>? EventTriggered;
    void Start();
    void Stop();
}
