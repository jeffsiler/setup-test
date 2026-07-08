using System.Windows;
using TrimbleBatteryMonitor.Core.Configuration;

namespace TrimbleBatteryMonitor;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        AppLogger.Info("Starting Trimble Battery Monitor");
        AppLogger.Info($"Process path: {Environment.ProcessPath ?? "unknown"}");
        AppLogger.Info($"Base directory: {AppContext.BaseDirectory}");

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            AppLogger.Error("Unhandled domain exception", args.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLogger.Error("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.DispatcherUnhandledException += (_, args) =>
            {
                AppLogger.Error("Dispatcher unhandled exception", args.Exception);
                MessageBox.Show(
                    $"Trimble Battery Monitor encountered an error:{Environment.NewLine}{Environment.NewLine}{args.Exception.Message}{Environment.NewLine}{Environment.NewLine}Details were written to:{Environment.NewLine}{AppLogger.LogFilePath}",
                    "Trimble Battery Monitor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            app.Run();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Fatal startup exception", ex);
            MessageBox.Show(
                $"Trimble Battery Monitor failed to start:{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Details were written to:{Environment.NewLine}{AppLogger.LogFilePath}",
                "Trimble Battery Monitor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
