namespace TrimbleBatteryMonitor.Core.Configuration;

public static class AppLogger
{
    private static readonly object Lock = new();
    private static string? _logFilePath;

    public static string LogFilePath
    {
        get
        {
            if (_logFilePath is not null)
            {
                return _logFilePath;
            }

            AppPaths.EnsureDirectories();
            var logsDir = Path.Combine(AppPaths.AppDataDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            _logFilePath = Path.Combine(logsDir, "app.log");
            return _logFilePath;
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? exception = null)
    {
        var details = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", details);
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}";
        lock (Lock)
        {
            try
            {
                File.AppendAllText(LogFilePath, line);
            }
            catch
            {
                // Best-effort logging only.
            }
        }
    }
}
