using System.Windows;
using TrimbleBatteryMonitor.Services;

namespace TrimbleBatteryMonitor;

public partial class App : System.Windows.Application
{
    private TrayApplicationHost? _trayHost;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _trayHost = new TrayApplicationHost();
        _trayHost.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayHost?.Dispose();
        base.OnExit(e);
    }
}
