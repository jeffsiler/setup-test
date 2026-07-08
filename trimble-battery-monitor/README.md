# Trimble Battery Monitor

Background battery monitoring application for **Trimble T110** Windows 11 tablets. Runs in the system tray, collects battery metrics on a schedule, stores history locally in SQLite, and provides a simple status/history viewer.

## Features

- Background polling of battery status (default: every 60 seconds)
- System tray icon with live charge percentage
- Status window with current battery info and 24-hour history
- Per-battery tracking via WMI (supports hot-swappable packs)
- Local SQLite database with configurable retention (default: 90 days)
- Auto-start with Windows
- MSI installer for deployment

## Collected Data

Each sample records:

| Field | Description |
|-------|-------------|
| Timestamp (UTC) | When the sample was taken |
| Battery name / device ID | Distinguishes main pack vs internal bridge battery |
| Charge % | 0–100 |
| AC connected | Whether tablet is on external power |
| Status | Charging, discharging, full, etc. |
| Time remaining | Estimated minutes (when Windows provides it) |
| Design / full charge capacity | When available via WMI |

## Data Locations

| Item | Path |
|------|------|
| SQLite database | `%AppData%\TrimbleBatteryMonitor\data\battery.db` |
| Settings | `%AppData%\TrimbleBatteryMonitor\appsettings.json` |
| Install location | `C:\Program Files\Trimble Battery Monitor\` |
| Log file | `%AppData%\TrimbleBatteryMonitor\logs\app.log` |

History in AppData is preserved across reinstalls.

## Installing on a Trimble T110

### Option A: Download MSI from GitHub Actions

1. Push this repo to GitHub (or use an existing remote).
2. Open **Actions** → **Build Trimble Battery Monitor MSI** → run workflow (or wait for a push to `trimble-battery-monitor/`).
3. Download the **TrimbleBatteryMonitor-msi** artifact.
4. Copy `TrimbleBatteryMonitor.msi` to the T110 tablet.
5. Double-click the MSI and follow the installer (**accept the UAC admin prompt** — required for v1.0.2+).
6. After install, look for a **desktop shortcut** and a **Start Menu** entry.

### Option B: Build on a Windows machine

```powershell
cd trimble-battery-monitor
dotnet tool install --global wix
.\installer\build-msi.ps1
```

Output: `trimble-battery-monitor\dist\TrimbleBatteryMonitor.msi`

## Verify installation on the T110

1. **Settings → Apps → Installed apps** — search for **Trimble Battery Monitor**
2. **File Explorer** — open `C:\Program Files\Trimble Battery Monitor\` and confirm `TrimbleBatteryMonitor.exe` exists
3. **Task Manager** — look for `TrimbleBatteryMonitor.exe` after install (it launches automatically)
4. **System tray** — click the **^** arrow near the clock to find the icon

If v1.0.0 or v1.0.1 was installed previously, uninstall it first from **Settings → Apps**, then install v1.0.2.

## First Run

After install:

1. The app starts automatically and appears in the system tray.
2. Double-click the tray icon (or right-click → **Open**) to view status and history.
3. Right-click → **Settings** to change poll interval, retention, or auto-start.

## SmartScreen Notice

The MSI is **unsigned** by default. Windows SmartScreen may show an “unknown publisher” warning on first install. For fleet deployment, sign the MSI with your organization’s code-signing certificate.

## Development

### Requirements

- .NET 8 SDK
- Windows (for running/testing the tray app and building MSI)
- WiX Toolset v5 (`dotnet tool install --global wix`)

### Build

```bash
dotnet build trimble-battery-monitor/TrimbleBatteryMonitor.sln
```

### Project Structure

```
trimble-battery-monitor/
├── src/
│   ├── TrimbleBatteryMonitor/       # WPF tray application
│   └── TrimbleBatteryMonitor.Core/  # Collection, storage, settings
├── installer/
│   ├── Product.wxs                  # WiX installer definition
│   └── build-msi.ps1                # Publish + MSI build script
└── .github/workflows/trimble-battery-monitor-msi.yml  # CI pipeline (repo root)
```

## Uninstall

Use **Settings → Apps → Installed apps** and remove **Trimble Battery Monitor**. The SQLite history in AppData is kept unless you delete `%AppData%\TrimbleBatteryMonitor` manually.

## License

Internal use — adjust as needed for your organization.
