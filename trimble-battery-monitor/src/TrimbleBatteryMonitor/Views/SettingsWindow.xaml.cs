using System.Windows;
using TrimbleBatteryMonitor.Core.Configuration;
using TrimbleBatteryMonitor.Services;

namespace TrimbleBatteryMonitor.Views;

public partial class SettingsWindow : Window
{
    private readonly BatteryMonitoringService _monitoringService;

    public SettingsWindow(BatteryMonitoringService monitoringService)
    {
        InitializeComponent();
        _monitoringService = monitoringService;

        var settings = _monitoringService.Settings;
        PollIntervalBox.Text = settings.PollIntervalSeconds.ToString();
        RetentionDaysBox.Text = settings.RetentionDays.ToString();
        StartWithWindowsBox.IsChecked = settings.StartWithWindows;
        ShowMainWindowOnStartupBox.IsChecked = settings.ShowMainWindowOnStartup;
        MinimizeToTrayOnCloseBox.IsChecked = settings.MinimizeToTrayOnClose;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PollIntervalBox.Text, out var pollInterval) || pollInterval < 15)
        {
            MessageBox.Show(this, "Poll interval must be at least 15 seconds.", "Invalid Setting", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RetentionDaysBox.Text, out var retentionDays) || retentionDays < 1)
        {
            MessageBox.Show(this, "Retention must be at least 1 day.", "Invalid Setting", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = new AppSettings
        {
            PollIntervalSeconds = pollInterval,
            RetentionDays = retentionDays,
            StartWithWindows = StartWithWindowsBox.IsChecked == true,
            ShowMainWindowOnStartup = ShowMainWindowOnStartupBox.IsChecked == true,
            MinimizeToTrayOnClose = MinimizeToTrayOnCloseBox.IsChecked == true,
        };

        _monitoringService.ApplySettings(settings);

        var executablePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            StartupRegistration.SetEnabled(settings.StartWithWindows, executablePath);
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
