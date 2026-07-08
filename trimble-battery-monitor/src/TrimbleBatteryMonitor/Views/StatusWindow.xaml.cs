using System.Collections.ObjectModel;
using System.Windows;
using TrimbleBatteryMonitor.Core.Models;
using TrimbleBatteryMonitor.Services;

namespace TrimbleBatteryMonitor.Views;

public partial class StatusWindow : Window
{
    private readonly BatteryMonitoringService _monitoringService;
    private readonly ObservableCollection<BatterySample> _history = new();

    public StatusWindow(BatteryMonitoringService monitoringService)
    {
        InitializeComponent();
        _monitoringService = monitoringService;
        HistoryGrid.ItemsSource = _history;
        RefreshData();
    }

    public void RefreshData()
    {
        var snapshot = _monitoringService.LatestSnapshot;
        if (snapshot is null || snapshot.Batteries.Count == 0)
        {
            CurrentStatusText.Text = "No battery data available.";
            AcStatusText.Text = string.Empty;
            TimeRemainingText.Text = string.Empty;
        }
        else
        {
            var primary = snapshot.Batteries[0];
            CurrentStatusText.Text = $"{primary.ChargePercent}% · {primary.StatusDisplay} · {primary.BatteryName}";
            AcStatusText.Text = snapshot.IsAcConnected ? "AC power connected" : "Running on battery";
            TimeRemainingText.Text = $"Estimated time remaining: {primary.TimeRemainingDisplay}";
        }

        _history.Clear();
        foreach (var sample in _monitoringService.GetHistory(TimeSpan.FromHours(24)))
        {
            _history.Add(sample);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshData();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
