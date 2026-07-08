using Microsoft.Win32;
using TrimbleBatteryMonitor.Core.Configuration;

namespace TrimbleBatteryMonitor.Core.Configuration;

public static class StartupRegistration
{
    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppPaths.RunKeyPath, writable: false);
        return key?.GetValue(AppPaths.RunKeyName) is string;
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppPaths.RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(AppPaths.RunKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(AppPaths.RunKeyName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(AppPaths.RunKeyName, throwOnMissingValue: false);
        }
    }
}
