namespace project.Models;

public class SensorReading
{
    public int Id { get; set; }
    public int SensorId { get; set; }
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? SensorLabel { get; set; }
    public string? Unit { get; set; }
    public string? Type { get; set; }
}
