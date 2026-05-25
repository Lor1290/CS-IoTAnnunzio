using project.Components;
using project.Services;
using Microsoft.Extensions.Options;

namespace project;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.AddSingleton<IEmailVerificationSender, EmailVerificationSender>();
        builder.Services.AddSingleton<SharedUserStore>();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
