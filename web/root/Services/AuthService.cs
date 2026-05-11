using System.Security.Cryptography;
using IoTDashboard.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace IoTDashboard.Services;

public class AuthService
{
    private readonly DatabaseService _db;
    private User? _currentUser;

    public AuthService(DatabaseService db) => _db = db;

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public async Task<(bool success, string? error)> LoginAsync(
        string email, string password, ProtectedLocalStorage storage)
    {
        var user = await _db.GetUserByEmailAsync(email.Trim().ToLower());
        if (user == null)
            return (false, "Email o password non corretti.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Email o password non corretti.");

        // create session token
        var token     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        await _db.CreateSessionAsync(user.Id, tokenHash, expiresAt);
        await storage.SetAsync("session_token", token);
        await _db.LogActionAsync(user.Id, "LOGIN");

        _currentUser = user;
        return (true, null);
    }

    public async Task<bool> RestoreSessionAsync(ProtectedLocalStorage storage)
    {
        try
        {
            var result = await storage.GetAsync<string>("session_token");
            if (!result.Success || result.Value == null) return false;

            var tokenHash = Convert.ToHexString(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(result.Value)));

            var (user, _) = await _db.ValidateSessionAsync(tokenHash);
            if (user == null) return false;

            _currentUser = user;
            return true;
        }
        catch { return false; }
    }

    public async Task LogoutAsync(ProtectedLocalStorage storage)
    {
        try
        {
            var result = await storage.GetAsync<string>("session_token");
            if (result.Success && result.Value != null)
            {
                var tokenHash = Convert.ToHexString(
                    SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(result.Value)));
                await _db.DeleteSessionAsync(tokenHash);
                if (_currentUser != null)
                    await _db.LogActionAsync(_currentUser.Id, "LOGOUT");
            }
        }
        catch { /* best effort */ }
        finally
        {
            await storage.DeleteAsync("session_token");
            _currentUser = null;
        }
    }
}
