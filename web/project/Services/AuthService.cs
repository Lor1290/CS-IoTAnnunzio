using project.Models;
using MySql.Data.MySqlClient;


namespace project.Services;

public class AuthService {
    private readonly string _conn;
    private readonly EmailService _email;
    private static readonly Dictionary<string, (string Code, DateTime Expiry)> _codes = new();


    public AuthService(IConfiguration config, EmailService email) {
        _conn  = config.GetConnectionString("DefaultConnection")!;
        _email = email;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password) {
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        var cmd = new MySqlCommand (
            "SELECT id, email, password_hash, full_name, role, is_verified FROM USERS WHERE email = @e LIMIT 1",
            con
        );
        cmd.Parameters.AddWithValue("@e", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var hash = reader.GetString(reader.GetOrdinal("password_hash"));
        if (!BCrypt.Net.BCrypt.Verify(password, hash)) return null;

        return new User {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            Role = reader.GetString(reader.GetOrdinal("role")),
            IsVerified = reader.GetBoolean(reader.GetOrdinal("is_verified")),
        };
    }

    public async Task SendTwoFactorCodeAsync(string email, string fullName) {
        var code   = Random.Shared.Next(100000, 999999).ToString();
        var expiry = DateTime.UtcNow.AddMinutes(5);
        _codes[email] = (code, expiry);

        Console.WriteLine($"Invio codice 2FA a {email}: {code}");

        try {
            await _email.SendAsync (
                to: email,
                subject: "IoT Annunzio — Codice di verifica",
                body: $"""
                       <p>Ciao <b>{fullName}</b>,</p>
                       <p>Il tuo codice di accesso è:</p>
                       <h2 style="letter-spacing:8px">{code}</h2>
                       <p>Il codice scade tra <b>5 minuti</b>.</p>
                       """
            );
            Console.WriteLine("Email 2FA inviata con successo.");
        } catch (Exception ex) {
            Console.WriteLine($"Errore nell'invio dell'email 2FA: {ex.Message}");
            throw new Exception("Errore nell'invio del codice di verifica. Controlla la configurazione email.");
        }
    }

    public bool VerifyTwoFactorCode(string email, string code) {
        if (!_codes.TryGetValue(email, out var entry)) return false;
        if (DateTime.UtcNow > entry.Expiry) { _codes.Remove(email); return false; }
        if (entry.Code != code) return false;
        _codes.Remove(email);
        return true;
    }

    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);
}
