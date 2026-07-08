using System.Runtime.InteropServices;
using System.Management;
using TrimbleBatteryMonitor.Core.Models;

namespace TrimbleBatteryMonitor.Core;

public sealed class BatteryCollector
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("Kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    public SystemPowerSnapshot Collect()
    {
        var timestamp = DateTime.UtcNow;
        var isAcConnected = false;
        int? aggregatePercent = null;
        int? aggregateMinutes = null;

        if (GetSystemPowerStatus(out var powerStatus))
        {
            isAcConnected = powerStatus.ACLineStatus == 1;
            if (powerStatus.BatteryLifePercent is > 0 and <= 100)
            {
                aggregatePercent = powerStatus.BatteryLifePercent;
            }

            if (powerStatus.BatteryLifeTime >= 0)
            {
                aggregateMinutes = powerStatus.BatteryLifeTime / 60;
            }
        }

        var batteries = CollectFromWmi(timestamp, isAcConnected);

        if (batteries.Count == 0 && aggregatePercent.HasValue)
        {
            batteries.Add(new BatterySample
            {
                TimestampUtc = timestamp,
                BatteryName = "System",
                DeviceId = "system",
                ChargePercent = aggregatePercent.Value,
                IsAcConnected = isAcConnected,
                Status = MapStatus(isAcConnected, aggregatePercent.Value, null),
                EstimatedMinutesRemaining = aggregateMinutes,
            });
        }

        return new SystemPowerSnapshot
        {
            IsAcConnected = isAcConnected,
            AggregateChargePercent = aggregatePercent,
            AggregateMinutesRemaining = aggregateMinutes,
            Batteries = batteries,
        };
    }

    private static List<BatterySample> CollectFromWmi(DateTime timestamp, bool isAcConnected)
    {
        var samples = new List<BatterySample>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, EstimatedChargeRemaining, EstimatedRunTime, BatteryStatus, DesignCapacity, FullChargeCapacity FROM Win32_Battery");

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                using (obj)
                {
                    var charge = Convert.ToInt32(obj["EstimatedChargeRemaining"] ?? 0);
                    var runtime = Convert.ToInt32(obj["EstimatedRunTime"] ?? unchecked((int)0xFFFFFFFF));
                    var batteryStatus = Convert.ToUInt16(obj["BatteryStatus"] ?? 0);
                    var designCapacity = ConvertNullableInt(obj["DesignCapacity"]);
                    var fullChargeCapacity = ConvertNullableInt(obj["FullChargeCapacity"]);

                    samples.Add(new BatterySample
                    {
                        TimestampUtc = timestamp,
                        DeviceId = obj["DeviceID"]?.ToString() ?? "unknown",
                        BatteryName = obj["Name"]?.ToString() ?? "Battery",
                        ChargePercent = Math.Clamp(charge, 0, 100),
                        IsAcConnected = isAcConnected,
                        Status = MapStatus(isAcConnected, charge, batteryStatus),
                        EstimatedMinutesRemaining = runtime == unchecked((int)0xFFFFFFFF) ? null : runtime,
                        DesignCapacityMwh = designCapacity,
                        FullChargeCapacityMwh = fullChargeCapacity,
                    });
                }
            }
        }
        catch (ManagementException)
        {
            // WMI may be unavailable in non-Windows environments or restricted contexts.
        }
        catch (PlatformNotSupportedException)
        {
            // Expected when running outside Windows.
        }

        return samples;
    }

    private static int? ConvertNullableInt(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            var converted = Convert.ToInt32(value);
            return converted > 0 ? converted : null;
        }
        catch
        {
            return null;
        }
    }

    private static BatteryStatusKind MapStatus(bool isAcConnected, int chargePercent, ushort? wmiStatus)
    {
        if (wmiStatus.HasValue)
        {
            return wmiStatus.Value switch
            {
                1 => BatteryStatusKind.Discharging,
                2 => BatteryStatusKind.AcConnected,
                3 => BatteryStatusKind.FullyCharged,
                4 => BatteryStatusKind.Low,
                5 => BatteryStatusKind.Critical,
                6 => BatteryStatusKind.Charging,
                7 => BatteryStatusKind.Charging,
                8 => BatteryStatusKind.Charging,
                9 => BatteryStatusKind.FullyCharged,
                _ => isAcConnected ? BatteryStatusKind.Charging : BatteryStatusKind.Discharging,
            };
        }

        if (isAcConnected)
        {
            return chargePercent >= 99 ? BatteryStatusKind.FullyCharged : BatteryStatusKind.Charging;
        }

        if (chargePercent <= 5)
        {
            return BatteryStatusKind.Critical;
        }

        if (chargePercent <= 20)
        {
            return BatteryStatusKind.Low;
        }

        return BatteryStatusKind.Discharging;
    }
}
