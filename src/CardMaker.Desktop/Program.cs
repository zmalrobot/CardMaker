using CardMaker.Application.Assets;
using CardMaker.Application.Content;
using CardMaker.Desktop;
using CardMaker.Desktop.Services;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure;
using CardMaker.Infrastructure.Identity;
using CardMaker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace CardMaker.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        EnsureWwwrootDirectory();

        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // 1. Ensure system data directories exist
        DesktopPathResolver.EnsureDirectoriesCreated();
        var dataRoot = DesktopPathResolver.GetDataDirectory();

        // Build basic in-memory configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataRoot"] = dataRoot,
            })
            .Build();

        // 2. Register Infrastructure and Core Services
        appBuilder.Services.AddSingleton<IConfiguration>(configuration);
        appBuilder.Services.AddCardMakerInfrastructure(configuration, dataRoot);

        // 3. Desktop in-process local admin auth bypass (ADR-009)
        appBuilder.Services.AddDataProtection();
        appBuilder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddCardMakerIdentityStores()
            .AddDefaultTokenProviders();

        appBuilder.Services.AddAuthorizationCore();
        appBuilder.Services.AddScoped<AuthenticationStateProvider, DesktopAuthenticationStateProvider>();
        appBuilder.Services.AddScoped<CardMaker.Application.Common.ILoadingService, CardMaker.UI.Services.LoadingService>();

        // 4. Configure Root Component
        appBuilder.RootComponents.Add<App>("#app");

        var app = appBuilder.Build();

        // 5. Configure Desktop Window
        app.MainWindow
            .SetTitle("CardMaker Studio Desktop")
            .SetSize(1280, 850)
            .SetUseOsDefaultSize(false)
            .SetResizable(true);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
        };

        app.Run();
    }

    private static void EnsureWwwrootDirectory()
    {
        var wwwrootOut = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(wwwrootOut))
        {
            Directory.CreateDirectory(wwwrootOut);
        }

        // Copy static resources from project directory if running in local debug environment
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? projectWwwroot = null;
        while (dir != null && dir.Exists)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CardMaker.Desktop", "wwwroot");
            if (Directory.Exists(candidate))
            {
                projectWwwroot = candidate;
                break;
            }
            dir = dir.Parent;
        }

        if (projectWwwroot != null && Directory.Exists(projectWwwroot))
        {
            foreach (var file in Directory.GetFiles(projectWwwroot, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(projectWwwroot, file);
                var dest = Path.Combine(wwwrootOut, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                if (!File.Exists(dest) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest))
                {
                    File.Copy(file, dest, true);
                }
            }
        }
    }
}
