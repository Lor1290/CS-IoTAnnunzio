namespace project.Services;

public class UserEntity {
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DeviceEntity? Device { get; set; }
}

public class DeviceEntity {
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserEntity? User { get; set; }
    public List<SensorEntity> Sensors { get; set; } = [];
}

public class SensorEntity {
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? MinThreshold { get; set; }
    public decimal? MaxThreshold { get; set; }
    public DeviceEntity? Device { get; set; }
    public List<SensorReadingEntity> Readings { get; set; } = [];
    public List<AlertEntity> Alerts { get; set; } = [];
}

public class SensorReadingEntity {
    public int Id { get; set; }
    public int SensorId { get; set; }
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public SensorEntity? Sensor { get; set; }
}

public class AlertEntity {
    public int Id { get; set; }
    public int SensorId { get; set; }
    public int? AcknowledgedBy { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public SensorEntity? Sensor { get; set; } 
}