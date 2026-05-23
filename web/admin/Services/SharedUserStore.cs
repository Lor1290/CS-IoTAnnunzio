using System.Text.Json;

namespace admin.Services;

public sealed class SharedUserStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _storagePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SharedUserStore(IWebHostEnvironment environment)
    {
        _storagePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "shared-data", "users.json"));
    }

    public async Task<List<SharedUserRecord>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureSeededAsync();
            var json = await File.ReadAllTextAsync(_storagePath);
            return JsonSerializer.Deserialize<List<SharedUserRecord>>(json, JsonOptions) ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SharedUserRecord?> FindByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim();
        var users = await GetAllAsync();
        return users.FirstOrDefault(user => string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));
    }

    public async Task UpsertAsync(SharedUserRecord user)
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureSeededAsync();
            var users = await ReadUsersUnlockedAsync();
            var existing = users.FirstOrDefault(item => item.Id == user.Id || string.Equals(item.Email, user.Email, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                if (user.Id <= 0)
                {
                    user.Id = users.Count == 0 ? 1 : users.Max(item => item.Id) + 1;
                }

                users.Add(user);
            }
            else
            {
                user.Id = existing.Id;
                users[users.IndexOf(existing)] = user;
            }

            await WriteUsersUnlockedAsync(users);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(int id)
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureSeededAsync();
            var users = await ReadUsersUnlockedAsync();
            users.RemoveAll(user => user.Id == id);
            await WriteUsersUnlockedAsync(users);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureSeededAsync()
    {
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(_storagePath))
        {
            return;
        }

        await WriteUsersUnlockedAsync(CreateSeedUsers());
    }

    private async Task<List<SharedUserRecord>> ReadUsersUnlockedAsync()
    {
        if (!File.Exists(_storagePath))
        {
            return CreateSeedUsers();
        }

        var json = await File.ReadAllTextAsync(_storagePath);
        return JsonSerializer.Deserialize<List<SharedUserRecord>>(json, JsonOptions) ?? [];
    }

    private async Task WriteUsersUnlockedAsync(List<SharedUserRecord> users)
    {
        var json = JsonSerializer.Serialize(users, JsonOptions);
        await File.WriteAllTextAsync(_storagePath, json);
    }

    private static List<SharedUserRecord> CreateSeedUsers() =>
    [
        new SharedUserRecord
        {
            Id = 1,
            Username = "admin",
            Email = "admin@iot.local",
            Password = "Admin2026!",
            FullName = "Admin IoT",
            Role = "admin",
            Verified = true,
            DeviceName = "ESP32-Admin",
            DeviceLocation = "Server Room",
            DeviceSerialId = "ESP32-001",
            DeviceStatus = "online"
        },
        new SharedUserRecord
        {
            Id = 2,
            Username = "mario",
            Email = "mario@iot.local",
            Password = "Mario1234!",
            FullName = "Mario Rossi",
            Role = "viewer",
            Verified = true,
            DeviceName = "ESP32-Mario",
            DeviceLocation = "Home",
            DeviceSerialId = "ESP32-002",
            DeviceStatus = "online"
        },
        new SharedUserRecord
        {
            Id = 3,
            Username = "giulia",
            Email = "giulia@iot.local",
            Password = "Giulia1234!",
            FullName = "Giulia Bianchi",
            Role = "viewer",
            Verified = true,
            DeviceName = "ESP32-Giulia",
            DeviceLocation = "Home",
            DeviceSerialId = "ESP32-003",
            DeviceStatus = "offline"
        }
    ];
}

public sealed class SharedUserRecord
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool Verified { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string DeviceLocation { get; set; } = string.Empty;

    public string DeviceSerialId { get; set; } = string.Empty;

    public string DeviceStatus { get; set; } = string.Empty;
}