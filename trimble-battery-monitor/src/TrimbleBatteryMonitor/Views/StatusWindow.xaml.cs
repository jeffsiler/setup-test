using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using TrimbleBatteryMonitor.Core.Models;
using TrimbleBatteryMonitor.Services;

namespace TrimbleBatteryMonitor.Views;

public partial class StatusWindow : Window
{
    private readonly BatteryMonitoringService _monitoringService;
    private readonly Action? _onMinimizeToTray;
    private readonly Action? _onExit;
    private readonly ObservableCollection<BatterySample> _history = new();

    public StatusWindow(
        BatteryMonitoringService monitoringService,
        Action? onMinimizeToTray = null,
        Action? onExit = null)
    {
        InitializeComponent();
        _monitoringService = monitoringService;
        _onMinimizeToTray = onMinimizeToTray;
        _onExit = onExit;
        HistoryGrid.ItemsSource = _history;
        Closing += StatusWindow_Closing;
        RefreshData();
    }

    public void RefreshData()
    {
        var snapshot = _monitoringService.LatestSnapshot;
        var history = _monitoringService.GetHistory(TimeSpan.FromHours(24)).OrderBy(s => s.TimestampUtc).ToList();

        if (snapshot is null || snapshot.Batteries.Count == 0)
        {
            CurrentStatusText.Text = "No battery data available.";
            AcStatusText.Text = "Waiting for first reading...";
            TimeRemainingText.Text = string.Empty;
            ChargeProgressBar.Value = 0;
            ChargePercentLabel.Text = string.Empty;
            BatteryCardsPanel.ItemsSource = null;
        }
        else
        {
            var primary = snapshot.Batteries[0];
            CurrentStatusText.Text = $"{primary.ChargePercent}% · {primary.StatusDisplay}";
            AcStatusText.Text = snapshot.IsAcConnected ? "AC power connected" : "Running on battery";
            TimeRemainingText.Text = $"Estimated time remaining: {primary.TimeRemainingDisplay} · {primary.BatteryName}";
            ChargeProgressBar.Value = primary.ChargePercent;
            ChargePercentLabel.Text = $"{primary.ChargePercent}%";

            BatteryCardsPanel.ItemsSource = snapshot.Batteries.Select(b => new BatteryCardViewModel(b)).ToList();
        }

        ChargeChart.SetSamples(history);

        _history.Clear();
        foreach (var sample in history.OrderByDescending(s => s.TimestampUtc))
        {
            _history.Add(sample);
        }
    }

    private void StatusWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_monitoringService.Settings.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            Hide();
            _onMinimizeToTray?.Invoke();
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshData();

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_monitoringService)
        {
            Owner = this,
        };
        settingsWindow.ShowDialog();
        RefreshData();
    }

    private void MinimizeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _onMinimizeToTray?.Invoke();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _onExit?.Invoke();
    }

    private sealed class BatteryCardViewModel
    {
        public BatteryCardViewModel(BatterySample sample)
        {
            Title = sample.BatteryName;
            Detail = $"{sample.ChargePercent}% · {sample.StatusDisplay} · {sample.TimeRemainingDisplay} left";
            ChargePercent = sample.ChargePercent;
        }

        public string Title { get; }
        public string Detail { get; }
        public int ChargePercent { get; }
    }
}
