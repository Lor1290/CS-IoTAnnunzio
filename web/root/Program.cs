using root.Components;
using root.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddHostedService<SensorPollingService>();
builder.Services.AddTransient<DatabaseService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapHub<SensorHub>("/hubs/sensors");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
