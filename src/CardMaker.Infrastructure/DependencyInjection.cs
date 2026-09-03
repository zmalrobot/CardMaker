using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Application.Rendering;
using CardMaker.Infrastructure.Identity;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using CardMaker.Infrastructure.Storage;
using CardMaker.Rendering;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Placeholders;
using CardMaker.Rendering.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardMaker.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra persistenza e archivio asset. Usata sia dall'host web sia, in F7, da quello desktop.
    /// </summary>
    public static IServiceCollection AddCardMakerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string dataRoot)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);

        Directory.CreateDirectory(dataRoot);
        var databasePath = Path.Combine(dataRoot, "cardmaker.db");

        services.AddDbContext<CardMakerDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.Configure<AssetStoreOptions>(o => o.RootPath = Path.Combine(dataRoot, "assets"));
        services.Configure<UploadLimits>(configuration.GetSection("Uploads"));

        services.AddSingleton<IAssetStore, FileSystemAssetStore>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<IFontProcessor, SkiaFontProcessor>();
        services.AddSingleton<PlaceholderFrameGenerator>();
        services.AddSingleton<PlaceholderSymbolGenerator>();
        services.AddSingleton<FontPreviewRenderer>();
        services.AddSingleton<TextEngine>();
        services.AddSingleton<CardRenderer>();
        services.AddSingleton<PdfExporter>();
        services.AddSingleton<IDecodedImageCache, DecodedImageCache>();
        services.AddScoped<IAssetCatalog, AssetService>();
        services.AddScoped<IFontCatalog, FontService>();
        services.AddScoped<ICardPreviewService, CardPreviewService>();
        services.AddScoped<IPlaceholderSeeder, PlaceholderSeeder>();
        services.AddScoped<IYuGiOhFontSeeder, YuGiOhFontSeeder>();
        services.AddScoped<IPokemonFontSeeder, PokemonFontSeeder>();
        services.AddScoped<IMtgFontSeeder, MtgFontSeeder>();
        services.AddScoped<CardMaker.Application.Content.IYuGiOhContentSeeder, CardMaker.Infrastructure.Content.YuGiOhContentSeeder>();
        services.AddScoped<CardMaker.Application.Content.IPokemonContentSeeder, CardMaker.Infrastructure.Content.PokemonContentSeeder>();
        services.AddScoped<CardMaker.Application.Content.IMtgContentSeeder, CardMaker.Infrastructure.Content.MtgContentSeeder>();
        services.AddScoped<CardMaker.Application.Content.IGamePackageService, CardMaker.Infrastructure.Content.GamePackageService>();
        services.AddScoped<CardMaker.Application.Content.ITemplateSelector, CardMaker.Application.Content.TemplateSelector>();
        services.AddScoped<CardMaker.Application.Cards.ICardService, CardMaker.Infrastructure.Cards.CardService>();
        services.AddScoped<CardMaker.Application.Cards.ICardExportService, CardMaker.Infrastructure.Cards.CardExportService>();
        services.AddScoped<CardMaker.Application.Admin.IAdminContentService, CardMaker.Infrastructure.Admin.AdminContentService>();
        services.AddScoped<CardMaker.Application.Admin.ITemplateAdminService, CardMaker.Infrastructure.Admin.TemplateAdminService>();
        services.AddScoped<CardMaker.Application.Identity.IInvitationService, CardMaker.Infrastructure.Identity.InvitationService>();
        services.AddScoped<CardMaker.Application.Admin.IBackupService, CardMaker.Infrastructure.Admin.BackupService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    public static string ResolveDataRoot(IConfiguration configuration, string fallbackRoot)
    {
        var configured = configuration["Storage:DataRoot"];
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(fallbackRoot, "data")
            : Path.GetFullPath(configured);
    }
}

public static class IdentityServiceExtensions
{
    public static IdentityBuilder AddCardMakerIdentityStores(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEntityFrameworkStores<CardMakerDbContext>();
    }

    public static Type ApplicationUserType => typeof(ApplicationUser);
}
