using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace admin.Services;

[Table("USERS")]
public class UserEntity {
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DeviceEntity? Device { get; set; }
}

[Table("DEVICES")]
public class DeviceEntity {
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "offline";

    [Column("last_seen")]
    public DateTime? LastSeen { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public UserEntity? User { get; set; }

    [InverseProperty("Device")] 
    public List<SensorEntity> Sensors { get; set; } = [];
}

[Table("SENSORS")]
public class SensorEntity {
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("min_threshold")]
    public decimal? MinThreshold { get; set; }

    [Column("max_threshold")]
    public decimal? MaxThreshold { get; set; }

    [InverseProperty("User")]
    public DeviceEntity? Device { get; set; }
}