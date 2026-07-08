using Microsoft.Data.Sqlite;
using TrimbleBatteryMonitor.Core.Configuration;
using TrimbleBatteryMonitor.Core.Models;

namespace TrimbleBatteryMonitor.Core.Storage;

public sealed class BatteryDataStore : IDisposable
{
    private readonly string _connectionString;
    private bool _initialized;

    public BatteryDataStore(string? databasePath = null)
    {
        var path = databasePath ?? AppPaths.DatabaseFilePath;
        AppPaths.EnsureDirectories();
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS BatterySamples (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                BatteryName TEXT NOT NULL,
                DeviceId TEXT NOT NULL,
                ChargePercent INTEGER NOT NULL,
                IsAcConnected INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                EstimatedMinutesRemaining INTEGER NULL,
                DesignCapacityMwh INTEGER NULL,
                FullChargeCapacityMwh INTEGER NULL
            );

            CREATE INDEX IF NOT EXISTS IX_BatterySamples_TimestampUtc
                ON BatterySamples (TimestampUtc DESC);

            CREATE INDEX IF NOT EXISTS IX_BatterySamples_DeviceId
                ON BatterySamples (DeviceId, TimestampUtc DESC);
            """;
        command.ExecuteNonQuery();
        _initialized = true;
    }

    public void InsertSamples(IEnumerable<BatterySample> samples)
    {
        Initialize();

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var sample in samples)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO BatterySamples (
                    TimestampUtc,
                    BatteryName,
                    DeviceId,
                    ChargePercent,
                    IsAcConnected,
                    Status,
                    EstimatedMinutesRemaining,
                    DesignCapacityMwh,
                    FullChargeCapacityMwh
                ) VALUES (
                    $timestamp,
                    $batteryName,
                    $deviceId,
                    $chargePercent,
                    $isAcConnected,
                    $status,
                    $estimatedMinutes,
                    $designCapacity,
                    $fullChargeCapacity
                );
                """;

            command.Parameters.AddWithValue("$timestamp", sample.TimestampUtc.ToString("O"));
            command.Parameters.AddWithValue("$batteryName", sample.BatteryName);
            command.Parameters.AddWithValue("$deviceId", sample.DeviceId);
            command.Parameters.AddWithValue("$chargePercent", sample.ChargePercent);
            command.Parameters.AddWithValue("$isAcConnected", sample.IsAcConnected ? 1 : 0);
            command.Parameters.AddWithValue("$status", (int)sample.Status);
            command.Parameters.AddWithValue("$estimatedMinutes", (object?)sample.EstimatedMinutesRemaining ?? DBNull.Value);
            command.Parameters.AddWithValue("$designCapacity", (object?)sample.DesignCapacityMwh ?? DBNull.Value);
            command.Parameters.AddWithValue("$fullChargeCapacity", (object?)sample.FullChargeCapacityMwh ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public IReadOnlyList<BatterySample> GetRecent(DateTime? sinceUtc = null, int limit = 500)
    {
        Initialize();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        if (sinceUtc.HasValue)
        {
            command.CommandText = """
                SELECT Id, TimestampUtc, BatteryName, DeviceId, ChargePercent, IsAcConnected,
                       Status, EstimatedMinutesRemaining, DesignCapacityMwh, FullChargeCapacityMwh
                FROM BatterySamples
                WHERE TimestampUtc >= $sinceUtc
                ORDER BY TimestampUtc DESC
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$sinceUtc", sinceUtc.Value.ToString("O"));
        }
        else
        {
            command.CommandText = """
                SELECT Id, TimestampUtc, BatteryName, DeviceId, ChargePercent, IsAcConnected,
                       Status, EstimatedMinutesRemaining, DesignCapacityMwh, FullChargeCapacityMwh
                FROM BatterySamples
                ORDER BY TimestampUtc DESC
                LIMIT $limit;
                """;
        }

        command.Parameters.AddWithValue("$limit", limit);

        var results = new List<BatterySample>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(ReadSample(reader));
        }

        return results;
    }

    public BatterySample? GetLatest()
    {
        var recent = GetRecent(limit: 1);
        return recent.Count > 0 ? recent[0] : null;
    }

    public int PurgeOlderThan(DateTime cutoffUtc)
    {
        Initialize();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM BatterySamples WHERE TimestampUtc < $cutoff;";
        command.Parameters.AddWithValue("$cutoff", cutoffUtc.ToString("O"));
        return command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static BatterySample ReadSample(SqliteDataReader reader)
    {
        return new BatterySample
        {
            Id = reader.GetInt64(0),
            TimestampUtc = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
            BatteryName = reader.GetString(2),
            DeviceId = reader.GetString(3),
            ChargePercent = reader.GetInt32(4),
            IsAcConnected = reader.GetInt32(5) == 1,
            Status = (BatteryStatusKind)reader.GetInt32(6),
            EstimatedMinutesRemaining = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            DesignCapacityMwh = reader.IsDBNull(8) ? null : reader.GetInt32(8),
            FullChargeCapacityMwh = reader.IsDBNull(9) ? null : reader.GetInt32(9),
        };
    }

    public void Dispose()
    {
        // Connections are short-lived per operation.
    }
}
