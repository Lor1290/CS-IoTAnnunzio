using project.Components;
using project.Services;

using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

using MudBlazor;
using MudBlazor.Services;

namespace project;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        // Add services to the container.
        builder.Services.AddHttpClient();
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.AddScoped<IEmailVerificationSender, EmailVerificationSender>();
        
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mySqlOptions => mySqlOptions.CommandTimeout(60)));

        builder.Services.AddScoped<SharedUserStore>();  

        

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(options => {
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
            });

        builder.Services.AddMudServices(config => {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
            config.SnackbarConfiguration.HideTransitionDuration = 200;
            config.SnackbarConfiguration.ShowTransitionDuration = 200;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}