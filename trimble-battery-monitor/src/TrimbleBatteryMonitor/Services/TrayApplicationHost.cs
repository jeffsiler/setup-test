using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using TrimbleBatteryMonitor.Core.Configuration;
using TrimbleBatteryMonitor.Core.Models;
using TrimbleBatteryMonitor.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TrimbleBatteryMonitor.Services;

public sealed class TrayApplicationHost : IDisposable
{
    private readonly BatteryMonitoringService _monitoringService = new();
    private readonly NotifyIcon _notifyIcon;
    private StatusWindow? _statusWindow;
    private SettingsWindow? _settingsWindow;

    public TrayApplicationHost()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "Trimble Battery Monitor",
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowStatusWindow();
        _monitoringService.SnapshotUpdated += OnSnapshotUpdated;
    }

    public void Start()
    {
        var settings = _monitoringService.Settings;
        var executablePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            StartupRegistration.SetEnabled(settings.StartWithWindows, executablePath);
        }

        _monitoringService.Start();
        UpdateTrayText(_monitoringService.LatestSnapshot);
        BuildContextMenu();
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowStatusWindow());
        menu.Items.Add("Settings", null, (_, _) => ShowSettingsWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = menu;
    }

    private void ShowStatusWindow()
    {
        if (_statusWindow is { IsLoaded: true })
        {
            _statusWindow.Activate();
            _statusWindow.Focus();
            return;
        }

        _statusWindow = new StatusWindow(_monitoringService);
        _statusWindow.Closed += (_, _) => _statusWindow = null;
        _statusWindow.Show();
        _statusWindow.Activate();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            _settingsWindow.Focus();
            return;
        }

        _settingsWindow = new SettingsWindow(_monitoringService);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.ShowDialog();
        _settingsWindow = null;
    }

    private void OnSnapshotUpdated(SystemPowerSnapshot snapshot)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => UpdateTrayText(snapshot));
            return;
        }

        UpdateTrayText(snapshot);
        _statusWindow?.RefreshData();
    }

    private void UpdateTrayText(SystemPowerSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Batteries.Count == 0)
        {
            _notifyIcon.Text = "Trimble Battery Monitor";
            return;
        }

        var primary = snapshot.Batteries[0];
        var powerSource = snapshot.IsAcConnected ? "Charging" : "On battery";
        var text = $"{primary.ChargePercent}% · {powerSource}";
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    private void ExitApplication()
    {
        var result = MessageBox.Show(
            "Exit Trimble Battery Monitor?",
            "Confirm Exit",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    public void Dispose()
    {
        _monitoringService.SnapshotUpdated -= OnSnapshotUpdated;
        _monitoringService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
