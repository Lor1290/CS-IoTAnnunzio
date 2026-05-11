using Microsoft.AspNetCore.SignalR;

namespace IoTDashboard.Services;

/// <summary>
/// Clients connect to this hub and join groups named "device_{id}" or "sensor_{id}".
/// The ESP32 backend (or a background service polling the DB) calls PushReading
/// whenever new data arrives.
/// </summary>
public class SensorHub : Hub
{
    public async Task JoinDevice(int deviceId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");

    public async Task LeaveDevice(int deviceId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
}

/// <summary>
/// Background service: polls SENSORSREADING every 5 s and pushes new rows via SignalR.
/// Replace this with a proper MQTT/HTTP webhook from ESP32 when ready.
/// </summary>
public class SensorPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<SensorHub> _hub;
    private readonly ILogger<SensorPollingService> _logger;

    // tracks the last reading id seen per sensor so we only push new rows
    private readonly Dictionary<int, int> _lastSeenId = new();

    public SensorPollingService(
        IServiceScopeFactory scopeFactory,
        IHubContext<SensorHub> hub,
        ILogger<SensorPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub          = hub;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

                var devices = await db.GetDevicesAsync();
                foreach (var device in devices)
                {
                    var sensors = await db.GetSensorsWithLatestReadingAsync(device.Id);
                    foreach (var sensor in sensors)
                    {
                        if (sensor.LatestReading == null) continue;
                        var reading = sensor.LatestReading;

                        if (_lastSeenId.TryGetValue(sensor.Id, out var lastId) && lastId >= reading.Id)
                            continue;

                        _lastSeenId[sensor.Id] = reading.Id;

                        await _hub.Clients.Group($"device_{device.Id}")
                            .SendAsync("NewReading", new
                            {
                                sensorId    = sensor.Id,
                                sensorLabel = sensor.Label,
                                unit        = sensor.Unit,
                                value       = reading.Value,
                                timestamp   = reading.Timestamp
                            }, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sensor polling error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
