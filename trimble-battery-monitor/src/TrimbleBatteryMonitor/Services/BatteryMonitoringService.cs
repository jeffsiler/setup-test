using System.Windows.Threading;
using TrimbleBatteryMonitor.Core;
using TrimbleBatteryMonitor.Core.Configuration;
using TrimbleBatteryMonitor.Core.Models;
using TrimbleBatteryMonitor.Core.Storage;

namespace TrimbleBatteryMonitor.Services;

public sealed class BatteryMonitoringService : IDisposable
{
    private readonly BatteryCollector _collector = new();
    private readonly BatteryDataStore _dataStore = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly DispatcherTimer _timer;
    private AppSettings _settings;
    private SystemPowerSnapshot? _latestSnapshot;

    public event Action<SystemPowerSnapshot>? SnapshotUpdated;

    public BatteryMonitoringService()
    {
        _settings = _settingsStore.Load();
        _dataStore.Initialize();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(15, _settings.PollIntervalSeconds)),
        };
        _timer.Tick += (_, _) => Poll();
    }

    public AppSettings Settings => _settings;

    public SystemPowerSnapshot? LatestSnapshot => _latestSnapshot;

    public void Start()
    {
        Poll();
        _timer.Start();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _settingsStore.Save(settings);
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(15, settings.PollIntervalSeconds));
    }

    public IReadOnlyList<BatterySample> GetHistory(TimeSpan lookback)
    {
        return _dataStore.GetRecent(DateTime.UtcNow.Subtract(lookback));
    }

    private void Poll()
    {
        try
        {
            var snapshot = _collector.Collect();
            _latestSnapshot = snapshot;

            if (snapshot.Batteries.Count > 0)
            {
                _dataStore.InsertSamples(snapshot.Batteries);
            }

            var purgeCutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _settings.RetentionDays));
            _dataStore.PurgeOlderThan(purgeCutoff);

            SnapshotUpdated?.Invoke(snapshot);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Battery polling failed", ex);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _dataStore.Dispose();
    }
}
