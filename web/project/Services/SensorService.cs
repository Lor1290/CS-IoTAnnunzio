using project.Models;
using MySql.Data.MySqlClient;

namespace project.Services;

public class SensorService {
    private readonly string _conn;
    public SensorService(IConfiguration config) =>
        _conn = config.GetConnectionString("DefaultConnection")!;


    public async Task<List<Sensor>> GetSensorsAsync() {
        var list = new List<Sensor>();
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        var cmd = new MySqlCommand("SELECT * FROM SENSORS ORDER BY id", con);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Sensor {
                Id = r.GetInt32("id"),
                DeviceId = r.GetInt32("device_id"),
                Type = r.GetString("type"),
                Label = r.GetString("label"),
                Unit = r.IsDBNull(r.GetOrdinal("unit")) ? null : r.GetString("unit"),
                MinThreshold = r.IsDBNull(r.GetOrdinal("min_threshold")) ? null : r.GetDecimal("min_threshold"),
                MaxThreshold = r.IsDBNull(r.GetOrdinal("max_threshold")) ? null : r.GetDecimal("max_threshold"),
            });

        return list;
    }


    public async Task<Dictionary<int, SensorReading>> GetLatestReadingsAsync() {
        var dict = new Dictionary<int, SensorReading>();
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        var sql = """
            SELECT sr.sensor_id, sr.value, sr.timestamp, s.label, s.unit, s.type
            FROM SENSORSREADING sr
            INNER JOIN SENSORS s ON s.id = sr.sensor_id
            WHERE sr.id IN (
                SELECT MAX(id) FROM SENSORSREADING GROUP BY sensor_id
            )
            """;

        var cmd = new MySqlCommand(sql, con);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) {
            var sensorId = r.GetInt32("sensor_id");
            dict[sensorId] = new SensorReading {
                SensorId    = sensorId,
                Value       = r.GetDecimal("value"),
                Timestamp   = r.GetDateTime("timestamp"),
                SensorLabel = r.GetString("label"),
                Unit        = r.IsDBNull(r.GetOrdinal("unit")) ? null : r.GetString("unit"),
                Type        = r.GetString("type"),
            };
        }

        return dict;
    }

    public async Task<List<SensorReading>> GetReadingsAsync(int sensorId, int limit = 50) {
        var list = new List<SensorReading>();
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT id, sensor_id, value, timestamp FROM SENSORSREADING WHERE sensor_id = @s ORDER BY timestamp DESC LIMIT @l",
            con);
        cmd.Parameters.AddWithValue("@s", sensorId);
        cmd.Parameters.AddWithValue("@l", limit);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new SensorReading {
                Id        = r.GetInt32("id"),
                SensorId  = r.GetInt32("sensor_id"),
                Value     = r.GetDecimal("value"),
                Timestamp = r.GetDateTime("timestamp"),
            });

        list.Reverse(); 
        return list;
    }

    public async Task<List<Alert>> GetActiveAlertsAsync() {
        var list = new List<Alert>();
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        var sql = """
            SELECT a.id, a.sensor_id, a.triggered_at, a.severity, a.message, s.label
            FROM ALERTS a
            INNER JOIN SENSORS s ON s.id = a.sensor_id
            WHERE a.resolved_at IS NULL
            ORDER BY a.triggered_at DESC
            LIMIT 20
            """;

        var cmd = new MySqlCommand(sql, con);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Alert {
                Id = r.GetInt32("id"),
                SensorId = r.GetInt32("sensor_id"),
                TriggeredAt = r.GetDateTime("triggered_at"),
                Severity = r.GetString("severity"),
                Message = r.GetString("message"),
                SensorLabel = r.GetString("label"),
            });

        return list;
    }
}
