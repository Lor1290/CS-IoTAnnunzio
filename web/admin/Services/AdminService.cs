using MySql.Data.MySqlClient;
using admintool.Models;

namespace admintool.Services;

public class AdminService {
    private readonly string _conn;
    public AdminService(IConfiguration config) =>
        _conn = config.GetConnectionString("DefaultConnection")!;


    public async Task<List<(int Id, string Email, string FullName, string Role)>> GetUsersAsync() {
        var list = new List<(int, string, string, string)>();
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT id, email, full_name, role FROM USERS ORDER BY id", con);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add((r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3)));

        return list;
    }

    public async Task<string> AddUserAsync(NewUserForm form) {
        if (string.IsNullOrWhiteSpace(form.Email))    return "Email obbligatoria.";
        if (string.IsNullOrWhiteSpace(form.Password)) return "Password obbligatoria.";
        if (string.IsNullOrWhiteSpace(form.FullName)) return "Nome obbligatorio.";
        if (string.IsNullOrWhiteSpace(form.SerialId)) return "Serial ID dispositivo obbligatorio.";

        var hash = BCrypt.Net.BCrypt.HashPassword(form.Password);

        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();
        await using var tx = await con.BeginTransactionAsync();

        try {
            var cmdUser = new MySqlCommand("""
                INSERT INTO USERS (email, password_hash, full_name, role, is_verified)
                VALUES (@email, @hash, @name, @role, 1)
                """, con, tx);
            cmdUser.Parameters.AddWithValue("@email", form.Email);
            cmdUser.Parameters.AddWithValue("@hash",  hash);
            cmdUser.Parameters.AddWithValue("@name",  form.FullName);
            cmdUser.Parameters.AddWithValue("@role",  form.Role);
            await cmdUser.ExecuteNonQueryAsync();
            var userId = (int)cmdUser.LastInsertedId;

            // 2. Insert device
            var cmdDev = new MySqlCommand("""
                INSERT INTO DEVICES (name, location, esp32_serial_id, status)
                VALUES (@name, @loc, @serial, 'offline')
                """, con, tx);
            cmdDev.Parameters.AddWithValue("@name",   string.IsNullOrWhiteSpace(form.DeviceName) ? $"ESP32-{form.Email}" : form.DeviceName);
            cmdDev.Parameters.AddWithValue("@loc",    form.Location);
            cmdDev.Parameters.AddWithValue("@serial", form.SerialId);
            await cmdDev.ExecuteNonQueryAsync();
            var deviceId = (int)cmdDev.LastInsertedId;

            var cmdProc = new MySqlCommand("CALL AddStandardSensors(@did)", con, tx);
            cmdProc.Parameters.AddWithValue("@did", deviceId);
            await cmdProc.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return string.Empty; 
        }
        catch (Exception ex) {
            await tx.RollbackAsync();
            return ex.Message;
        }
    }

    public async Task<string> DeleteUserAsync(int userId) {
        await using var con = new MySqlConnection(_conn);
        await con.OpenAsync();

        try {
            var cmd = new MySqlCommand("DELETE FROM USERS WHERE id = @id", con);
            cmd.Parameters.AddWithValue("@id", userId);
            await cmd.ExecuteNonQueryAsync();
            return string.Empty;
        }
        catch (Exception ex) {
            return ex.Message;
        }
    }
}
