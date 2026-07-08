namespace TrimbleBatteryMonitor.Core.Models;

public enum BatteryStatusKind
{
    Unknown = 0,
    Discharging = 1,
    AcConnected = 2,
    FullyCharged = 3,
    Charging = 4,
    Critical = 5,
    Low = 6,
}

public sealed class BatterySample
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string BatteryName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int ChargePercent { get; set; }
    public bool IsAcConnected { get; set; }
    public BatteryStatusKind Status { get; set; }
    public int? EstimatedMinutesRemaining { get; set; }
    public int? DesignCapacityMwh { get; set; }
    public int? FullChargeCapacityMwh { get; set; }

    public string StatusDisplay => Status switch
    {
        BatteryStatusKind.Discharging => "Discharging",
        BatteryStatusKind.AcConnected => "On AC",
        BatteryStatusKind.FullyCharged => "Full",
        BatteryStatusKind.Charging => "Charging",
        BatteryStatusKind.Critical => "Critical",
        BatteryStatusKind.Low => "Low",
        _ => "Unknown",
    };

    public string TimeRemainingDisplay
    {
        get
        {
            if (!EstimatedMinutesRemaining.HasValue || EstimatedMinutesRemaining.Value < 0)
            {
                return "Unknown";
            }

            var total = EstimatedMinutesRemaining.Value;
            if (total >= 60)
            {
                return $"{total / 60}h {total % 60}m";
            }

            return $"{total}m";
        }
    }
}

public sealed class SystemPowerSnapshot
{
    public bool IsAcConnected { get; init; }
    public int? AggregateChargePercent { get; init; }
    public int? AggregateMinutesRemaining { get; init; }
    public IReadOnlyList<BatterySample> Batteries { get; init; } = Array.Empty<BatterySample>();
}
