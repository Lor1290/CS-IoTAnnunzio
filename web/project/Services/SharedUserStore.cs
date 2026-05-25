using Microsoft.EntityFrameworkCore;

namespace project.Services;

public sealed class SharedUserStore(AppDbContext db)
{
    public async Task<List<SharedUserRecord>> GetAllAsync()
    {
        var users = await db.Users
            .Include(u => u.Device)
            .ToListAsync();

        return users.Select(u => new SharedUserRecord {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Password = u.PasswordHash,
            FullName = u.FullName,
            DeviceName = u.Device?.Name ?? string.Empty,
            DeviceLocation = u.Device?.Location ?? string.Empty,
            DeviceStatus = u.Device?.Status ?? "offline"
        }).ToList();
    }

    public async Task<SharedUserRecord?> FindByEmailAsync(string email)
    {
        var u = await db.Users
            .Include(u => u.Device)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());

        if (u is null) return null;

        return new SharedUserRecord {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Password = u.PasswordHash,
            FullName = u.FullName,
            DeviceName = u.Device?.Name ?? string.Empty,
            DeviceLocation = u.Device?.Location ?? string.Empty,
            DeviceStatus = u.Device?.Status ?? "offline"
        };
    }

    public async Task UpsertAsync(SharedUserRecord record)
    {
        var existing = await db.Users
            .Include(u => u.Device)
            .FirstOrDefaultAsync(u => u.Id == record.Id || u.Email == record.Email);

        if (existing is null) {
            var user = new UserEntity {
                Username = record.Username,
                Email = record.Email,
                PasswordHash = record.Password,
                FullName = record.FullName,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        } else {
            existing.Username = record.Username;
            existing.Email = record.Email;
            existing.PasswordHash = record.Password;
            existing.FullName = record.FullName;
            await db.SaveChangesAsync();
        }
    }

    // --- Sensor readings ---

    public async Task<List<SensorWithLatestReading>> GetSensorReadingsAsync(int userId)
    {
        var device = await db.Devices
            .Include(d => d.Sensors)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (device is null) return [];

        var sensorIds = device.Sensors.Select(s => s.Id).ToList();

        var latestReadings = await db.SensorReadings
            .Where(r => sensorIds.Contains(r.SensorId))
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync();

        return device.Sensors.Select(s => {
            var reading = latestReadings.FirstOrDefault(r => r.SensorId == s.Id);
            return new SensorWithLatestReading {
                SensorId = s.Id,
                Label = s.Label,
                Type = s.Type,
                Unit = s.Unit ?? string.Empty,
                Value = reading?.Value,
                Timestamp = reading?.Timestamp,
                MinThreshold = s.MinThreshold,
                MaxThreshold = s.MaxThreshold
            };
        }).ToList();
    }

    public async Task<List<AlertRecord>> GetActiveAlertsAsync(int userId)
    {
        var device = await db.Devices
            .Include(d => d.Sensors)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (device is null) return [];

        var sensorIds = device.Sensors.Select(s => s.Id).ToList();

        var alerts = await db.Alerts
            .Include(a => a.Sensor)
            .Where(a => sensorIds.Contains(a.SensorId) && a.ResolvedAt == null)
            .OrderByDescending(a => a.TriggeredAt)
            .Take(10)
            .ToListAsync();

        return alerts.Select(a => new AlertRecord {
            SensorLabel = a.Sensor?.Label ?? string.Empty,
            Severity = a.Severity,
            Message = a.Message,
            TriggeredAt = a.TriggeredAt
        }).ToList();
    }
}

// --- DTOs ---

public sealed class SensorWithLatestReading {
    public int SensorId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public DateTime? Timestamp { get; set; }
    public decimal? MinThreshold { get; set; }
    public decimal? MaxThreshold { get; set; }

    public bool HasValue => Value.HasValue;

    public int Percentage {
        get {
            if (Value is null || MinThreshold is null || MaxThreshold is null) return 0;
            if (MaxThreshold == MinThreshold) return 0;
            var pct = (double)(Value.Value - MinThreshold.Value) / (double)(MaxThreshold.Value - MinThreshold.Value) * 100;
            return (int)Math.Clamp(pct, 0, 100);
        }
    }
}

public sealed class AlertRecord {
    public string SensorLabel { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
}