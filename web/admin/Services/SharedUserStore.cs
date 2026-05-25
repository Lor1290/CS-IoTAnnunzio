using Microsoft.EntityFrameworkCore;

namespace admin.Services;

public sealed class SharedUserStore(AppDbContext db) {
    public async Task<List<SharedUserRecord>> GetAllAsync() {
        var users = await db.Users
            .Include(u => u.Device)
            .ToListAsync();

        return users.Select(u => new SharedUserRecord {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Password = u.PasswordHash,
            FullName = u.FullName,
            DeviceName = u.Device?.Name ?? string.Empty,
            DeviceLocation = u.Device?.Location ?? string.Empty,
            DeviceStatus = u.Device?.Status ?? "offline"
        }).ToList();
    }

    public async Task<SharedUserRecord?> FindByEmailAsync(string email) {
        var u = await db.Users
            .Include(u => u.Device)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());

        if (u is null) return null;

        return new SharedUserRecord {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Password = u.PasswordHash,
            FullName = u.FullName,
            DeviceName = u.Device?.Name ?? string.Empty,
            DeviceLocation = u.Device?.Location ?? string.Empty,
            DeviceStatus = u.Device?.Status ?? "offline"
        };
    }

    public async Task UpsertAsync(SharedUserRecord record) {
        var existing = await db.Users
            .Include(u => u.Device)
            .FirstOrDefaultAsync(u => u.Id == record.Id || u.Email == record.Email);

        if (existing is null) {
            // --- CREATE user ---
            var user = new UserEntity {
                Username = record.Username,
                Email = record.Email,
                PasswordHash = record.Password,
                FullName = record.FullName,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(); 

            // --- CREATE device ---
            var device = new DeviceEntity {
                UserId    = user.Id,
                Name      = record.DeviceName,
                Location  = record.DeviceLocation,
                Status    = record.DeviceStatus,
                CreatedAt = DateTime.UtcNow
            };
            db.Devices.Add(device);
            await db.SaveChangesAsync(); 

            // --- ADD standard sensors ---
            db.Sensors.AddRange(BuildStandardSensors(device.Id));
            await db.SaveChangesAsync();
        } else {
            // --- UPDATE user ---
            existing.Username = record.Username;
            existing.Email = record.Email;
            existing.PasswordHash = record.Password;
            existing.FullName = record.FullName;

            // --- UPDATE device ---
            if (existing.Device is not null) {
                existing.Device.Name = record.DeviceName;
                existing.Device.Location = record.DeviceLocation;
                existing.Device.Status = record.DeviceStatus;
            } else {
                var device = new DeviceEntity {
                    UserId    = existing.Id,
                    Name      = record.DeviceName,
                    Location  = record.DeviceLocation,
                    Status    = record.DeviceStatus,
                    CreatedAt = DateTime.UtcNow
                };

                db.Devices.Add(device);
                await db.SaveChangesAsync();
                db.Sensors.AddRange(BuildStandardSensors(device.Id));
            }

            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id) {
        var user = await db.Users
            .Include(u => u.Device)
                .ThenInclude(d => d!.Sensors)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return;

        if (user.Device is not null) {
            db.Sensors.RemoveRange(user.Device.Sensors);
            db.Devices.Remove(user.Device);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }

    private static List<SensorEntity> BuildStandardSensors(int deviceId) =>
    [
        new() { DeviceId = deviceId, Type = "temperature", Label = "DHT22 Temperatura", Unit = "°C", MinThreshold = -10, MaxThreshold = 50 },
        new() { DeviceId = deviceId, Type = "humidity", Label = "DHT22 Umidità", Unit = "%", MinThreshold = 0, MaxThreshold = 100 },
        new() { DeviceId = deviceId, Type = "temperature", Label = "BMP180 Temperatura", Unit = "°C", MinThreshold = -10, MaxThreshold = 50 },
        new() { DeviceId = deviceId, Type = "pressure", Label = "BMP180 Pressione", Unit = "Pa", MinThreshold = 90000, MaxThreshold = 110000 },
        new() { DeviceId = deviceId, Type = "temperature", Label = "NTC Temperatura", Unit = "°C", MinThreshold = -10, MaxThreshold = 50 },
        new() { DeviceId = deviceId, Type = "light", Label = "Luminosità", Unit = "lux", MinThreshold = 0, MaxThreshold = 10000 },
        new() { DeviceId = deviceId, Type = "gas", Label = "Gas", Unit = "", MinThreshold = 0, MaxThreshold = 0.60m },
        new() { DeviceId = deviceId, Type = "wind", Label = "Vento", Unit = "", MinThreshold = 0, MaxThreshold = 1 },
        new() { DeviceId = deviceId, Type = "water", Label = "Acqua", Unit = "", MinThreshold = 0, MaxThreshold = 1 },
    ];
}