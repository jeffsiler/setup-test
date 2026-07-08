namespace TrimbleBatteryMonitor.Core.Configuration;

public sealed class AppSettings
{
    public int PollIntervalSeconds { get; set; } = 60;
    public int RetentionDays { get; set; } = 90;
    public bool StartWithWindows { get; set; } = true;
    public bool ShowMainWindowOnStartup { get; set; } = true;
    public bool MinimizeToTrayOnClose { get; set; } = true;
}
