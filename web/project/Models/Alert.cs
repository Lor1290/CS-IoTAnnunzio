namespace project.Models;

public class Alert
{
    public int Id { get; set; }
    public int SensorId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SensorLabel { get; set; }
}
