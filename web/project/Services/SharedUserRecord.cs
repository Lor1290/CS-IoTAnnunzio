namespace project.Services;

public sealed class SharedUserRecord
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceLocation { get; set; } = string.Empty;
    public string DeviceStatus { get; set; } = string.Empty;
}