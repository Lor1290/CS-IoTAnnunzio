namespace admin.Models;

public class NewUserForm
{
    public string FullName      { get; set; } = string.Empty;
    public string Email         { get; set; } = string.Empty;
    public string Password      { get; set; } = string.Empty;
    public string Role          { get; set; } = "viewer";
    public string DeviceName    { get; set; } = string.Empty;
    public string Location      { get; set; } = string.Empty;
    public string SerialId      { get; set; } = string.Empty;
}
