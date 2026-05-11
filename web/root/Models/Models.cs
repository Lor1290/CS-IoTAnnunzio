namespace root.Models;

// ── USERS ──────────────────────────────────────────────────────────────────
public class User
{
    public int      Id           { get; set; }
    public string   Email        { get; set; } = "";
    public string   PasswordHash { get; set; } = "";
    public string   FullName     { get; set; } = "";
    public string   Role         { get; set; } = "viewer";
    public string?  TotpSecret   { get; set; }
    public bool     IsVerified   { get; set; }
    public DateTime CreatedAt    { get; set; }
}

// ── DEVICES ────────────────────────────────────────────────────────────────
public class Device
{
    public int       Id            { get; set; }
    public string    Name          { get; set; } = "";
    public string?   Location      { get; set; }
    public string    Esp32SerialId { get; set; } = "";
    public string    Status        { get; set; } = "offline";
    public DateTime? LastSeen      { get; set; }
    public DateTime  CreatedAt     { get; set; }

    // navigation (populated in queries)
    public List<Sensor> Sensors { get; set; } = new();
}

// ── SENSORS ────────────────────────────────────────────────────────────────
public class Sensor
{
    public int      Id           { get; set; }
    public int      DeviceId     { get; set; }
    public string   Type         { get; set; } = "";
    public string   Label        { get; set; } = "";
    public string?  Unit         { get; set; }
    public decimal? MinThreshold { get; set; }
    public decimal? MaxThreshold { get; set; }

    // latest reading (populated in queries)
    public SensorReading? LatestReading { get; set; }
}

// ── SENSOR READINGS ────────────────────────────────────────────────────────
public class SensorReading
{
    public int      Id        { get; set; }
    public int      SensorId  { get; set; }
    public decimal  Value     { get; set; }
    public DateTime Timestamp { get; set; }
}

// ── ALERTS ─────────────────────────────────────────────────────────────────
public class Alert
{
    public int       Id             { get; set; }
    public int       SensorId       { get; set; }
    public DateTime  TriggeredAt    { get; set; }
    public DateTime? ResolvedAt     { get; set; }
    public string    Severity       { get; set; } = "";
    public string    Message        { get; set; } = "";
    public int?      AcknowledgedBy { get; set; }

    // joined fields
    public string SensorLabel { get; set; } = "";
    public string DeviceName  { get; set; } = "";
}

// ── AUDIT LOG ──────────────────────────────────────────────────────────────
public class AuditLog
{
    public int      Id        { get; set; }
    public int?     UserId    { get; set; }
    public string   Action    { get; set; } = "";
    public string?  IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── SESSION ────────────────────────────────────────────────────────────────
public class Session
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── VIEW MODELS ────────────────────────────────────────────────────────────
public class LoginRequest
{
    public string Email    { get; set; } = "";
    public string Password { get; set; } = "";
}

public class DashboardStats
{
    public int TotalDevices  { get; set; }
    public int OnlineDevices { get; set; }
    public int TotalSensors  { get; set; }
    public int ActiveAlerts  { get; set; }
}
