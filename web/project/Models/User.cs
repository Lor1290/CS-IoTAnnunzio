namespace project.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public string? TotpSecret { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
