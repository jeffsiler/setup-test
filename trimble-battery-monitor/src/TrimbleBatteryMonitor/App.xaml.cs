using System.Windows;
using TrimbleBatteryMonitor.Core.Configuration;
using TrimbleBatteryMonitor.Services;

namespace TrimbleBatteryMonitor;

public partial class App : System.Windows.Application
{
    private TrayApplicationHost? _trayHost;

    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _trayHost = new TrayApplicationHost();
            _trayHost.Start();
            AppLogger.Info("Tray application host started.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            AppLogger.Info("Exiting duplicate instance.");
            Shutdown();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to start tray application host", ex);
            MessageBox.Show(
                $"Trimble Battery Monitor failed to start:{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Details were written to:{Environment.NewLine}{AppLogger.LogFilePath}",
                "Trimble Battery Monitor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayHost?.Dispose();
        AppLogger.Info("Trimble Battery Monitor exiting.");
        base.OnExit(e);
    }
}
