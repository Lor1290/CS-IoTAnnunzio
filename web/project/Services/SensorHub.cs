using project.Models;
using Microsoft.AspNetCore.SignalR;

namespace project.Services;

public class SensorHub : Hub {
    public async Task Subscribe() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "sensors");
}

public class SensorBroadcastService : BackgroundService {
    private readonly IHubContext<SensorHub> _hub;
    private readonly IServiceScopeFactory  _scope;

    public SensorBroadcastService(IHubContext<SensorHub> hub, IServiceScopeFactory scope) {
        _hub   = hub;
        _scope = scope;
    }

    protected override async Task ExecuteAsync(CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            await Task.Delay(3000, ct);

            using var scope   = _scope.CreateScope();
            var sensorService = scope.ServiceProvider.GetRequiredService<SensorService>();

            var latest = await sensorService.GetLatestReadingsAsync();
            var alerts = await sensorService.GetActiveAlertsAsync();

            await _hub.Clients.Group("sensors").SendAsync("ReceiveUpdate", latest, alerts, cancellationToken: ct);
        }
    }
}
