using Dapper;
using MySqlConnector;
using IoTDashboard.Models;

namespace IoTDashboard.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Connection string 'MySql' not found.");
    }

    private MySqlConnection OpenConnection() =>
        new MySqlConnection(_connectionString);

    // ── AUTH ──────────────────────────────────────────────────────────────

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var db = OpenConnection();
        return await db.QueryFirstOrDefaultAsync<User>(
            "SELECT id, email, password_hash AS PasswordHash, full_name AS FullName, " +
            "role, totp_secret AS TotpSecret, is_verified AS IsVerified, created_at AS CreatedAt " +
            "FROM USERS WHERE email = @email",
            new { email });
    }

    public async Task<Session> CreateSessionAsync(int userId, string tokenHash, DateTime expiresAt)
    {
        using var db = OpenConnection();
        await db.ExecuteAsync(
            "INSERT INTO SESSIONS (user_id, token_hash, expires_at) VALUES (@userId, @tokenHash, @expiresAt)",
            new { userId, tokenHash, expiresAt });
        return new Session { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt };
    }

    public async Task<(User? user, Session? session)> ValidateSessionAsync(string tokenHash)
    {
        using var db = OpenConnection();
        var row = await db.QueryFirstOrDefaultAsync(
            "SELECT u.id, u.email, u.full_name AS FullName, u.role, u.is_verified AS IsVerified, " +
            "s.expires_at AS ExpiresAt " +
            "FROM SESSIONS s JOIN USERS u ON s.user_id = u.id " +
            "WHERE s.token_hash = @tokenHash AND s.expires_at > NOW()",
            new { tokenHash });

        if (row == null) return (null, null);

        var user = new User
        {
            Id = (int)row.id, Email = (string)row.email,
            FullName = (string)row.FullName, Role = (string)row.role,
            IsVerified = (bool)row.IsVerified
        };
        var session = new Session { TokenHash = tokenHash, ExpiresAt = (DateTime)row.ExpiresAt };
        return (user, session);
    }

    public async Task DeleteSessionAsync(string tokenHash)
    {
        using var db = OpenConnection();
        await db.ExecuteAsync("DELETE FROM SESSIONS WHERE token_hash = @tokenHash", new { tokenHash });
    }

    // ── DASHBOARD STATS ───────────────────────────────────────────────────

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        using var db = OpenConnection();
        var stats = await db.QueryFirstAsync<DashboardStats>(
            @"SELECT
                (SELECT COUNT(*) FROM DEVICES)                                   AS TotalDevices,
                (SELECT COUNT(*) FROM DEVICES WHERE status = 'online')           AS OnlineDevices,
                (SELECT COUNT(*) FROM SENSORS)                                   AS TotalSensors,
                (SELECT COUNT(*) FROM ALERTS WHERE resolved_at IS NULL)          AS ActiveAlerts");
        return stats;
    }

    // ── DEVICES ───────────────────────────────────────────────────────────

    public async Task<List<Device>> GetDevicesAsync()
    {
        using var db = OpenConnection();
        var devices = (await db.QueryAsync<Device>(
            "SELECT id, name, location, esp32_serial_id AS Esp32SerialId, " +
            "status, last_seen AS LastSeen, created_at AS CreatedAt FROM DEVICES " +
            "ORDER BY status DESC, name")).ToList();
        return devices;
    }

    public async Task<Device?> GetDeviceWithSensorsAsync(int deviceId)
    {
        using var db = OpenConnection();
        var device = await db.QueryFirstOrDefaultAsync<Device>(
            "SELECT id, name, location, esp32_serial_id AS Esp32SerialId, " +
            "status, last_seen AS LastSeen, created_at AS CreatedAt " +
            "FROM DEVICES WHERE id = @deviceId", new { deviceId });

        if (device == null) return null;

        var sensors = await GetSensorsWithLatestReadingAsync(deviceId);
        device.Sensors = sensors;
        return device;
    }

    // ── SENSORS ───────────────────────────────────────────────────────────

    public async Task<List<Sensor>> GetSensorsWithLatestReadingAsync(int deviceId)
    {
        using var db = OpenConnection();
        var sensors = (await db.QueryAsync<Sensor>(
            "SELECT id, device_id AS DeviceId, type, label, unit, " +
            "min_threshold AS MinThreshold, max_threshold AS MaxThreshold " +
            "FROM SENSORS WHERE device_id = @deviceId", new { deviceId })).ToList();

        foreach (var sensor in sensors)
        {
            sensor.LatestReading = await db.QueryFirstOrDefaultAsync<SensorReading>(
                "SELECT id, sensor_id AS SensorId, value, timestamp " +
                "FROM SENSORSREADING WHERE sensor_id = @sensorId " +
                "ORDER BY timestamp DESC LIMIT 1", new { sensorId = sensor.Id });
        }
        return sensors;
    }

    public async Task<List<SensorReading>> GetSensorReadingsAsync(int sensorId, int limit = 50)
    {
        using var db = OpenConnection();
        var readings = (await db.QueryAsync<SensorReading>(
            "SELECT id, sensor_id AS SensorId, value, timestamp " +
            "FROM SENSORSREADING WHERE sensor_id = @sensorId " +
            "ORDER BY timestamp DESC LIMIT @limit",
            new { sensorId, limit })).ToList();
        readings.Reverse();
        return readings;
    }

    // ── ALERTS ────────────────────────────────────────────────────────────

    public async Task<List<Alert>> GetAlertsAsync(bool onlyActive = false)
    {
        using var db = OpenConnection();
        var where = onlyActive ? "WHERE a.resolved_at IS NULL" : "";
        return (await db.QueryAsync<Alert>(
            $@"SELECT a.id, a.sensor_id AS SensorId, a.triggered_at AS TriggeredAt,
               a.resolved_at AS ResolvedAt, a.severity, a.message,
               a.acknowledged_by AS AcknowledgedBy,
               s.label AS SensorLabel, d.name AS DeviceName
               FROM ALERTS a
               JOIN SENSORS s ON a.sensor_id = s.id
               JOIN DEVICES d ON s.device_id = d.id
               {where}
               ORDER BY a.triggered_at DESC LIMIT 100")).ToList();
    }

    public async Task AcknowledgeAlertAsync(int alertId, int userId)
    {
        using var db = OpenConnection();
        await db.ExecuteAsync(
            "UPDATE ALERTS SET resolved_at = NOW(), acknowledged_by = @userId WHERE id = @alertId",
            new { alertId, userId });
    }

    // ── AUDIT LOG ─────────────────────────────────────────────────────────

    public async Task LogActionAsync(int? userId, string action, string? ipAddress = null)
    {
        using var db = OpenConnection();
        await db.ExecuteAsync(
            "INSERT INTO AUDITLOG (user_id, action, ip_address) VALUES (@userId, @action, @ipAddress)",
            new { userId, action, ipAddress });
    }
}
