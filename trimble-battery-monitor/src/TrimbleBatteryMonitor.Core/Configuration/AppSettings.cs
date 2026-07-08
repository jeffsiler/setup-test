namespace TrimbleBatteryMonitor.Core.Configuration;

public sealed class AppSettings
{
    public int PollIntervalSeconds { get; set; } = 60;
    public int RetentionDays { get; set; } = 90;
    public bool StartWithWindows { get; set; } = true;
    public bool ShowMainWindowOnStartup { get; set; } = true;
    public bool MinimizeToTrayOnClose { get; set; } = true;
}

public static class AppPaths
{
    public const string AppFolderName = "TrimbleBatteryMonitor";
    public const string RunKeyName = "TrimbleBatteryMonitor";
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);

    public static string SettingsFilePath =>
        Path.Combine(AppDataDirectory, "appsettings.json");

    public static string DatabaseFilePath =>
        Path.Combine(AppDataDirectory, "data", "battery.db");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabaseFilePath)!);
    }
}
