using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Application.Cards;
using CardMaker.Application.Common;
using CardMaker.Application.Rendering;
using CardMaker.Contracts.Layout;
using CardMaker.Desktop.Services;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure;
using CardMaker.Infrastructure.Cards;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Identity;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using CardMaker.Rendering;
using CardMaker.Rendering.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CardMaker.Application.Tests.Smoke;

public sealed class DesktopAndWebSmokeTests
{
    [Fact]
    public void TEST_SMOKE_001_DesktopDependencyInjectionBuildsAndResolvesCriticalServices()
    {
        var services = new ServiceCollection();
        var tempDir = Path.Combine(Path.GetTempPath(), "CardMaker_Smoke_Desktop_" + Guid.NewGuid().ToString("N"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataRoot"] = tempDir,
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddCardMakerInfrastructure(configuration, tempDir);

        services.AddDataProtection();
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddCardMakerIdentityStores()
            .AddDefaultTokenProviders();

        services.AddAuthorizationCore();
        services.AddScoped<AuthenticationStateProvider, DesktopAuthenticationStateProvider>();
        services.AddScoped<ILoadingService, CardMaker.UI.Services.LoadingService>();
        services.AddMemoryCache();
        services.AddScoped<IAssetUriService, DesktopAssetUriService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CardMakerDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DatabaseInitializer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICardService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICardPreviewService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICardExportService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAssetUriService>());
    }

    [Fact]
    public void TEST_SMOKE_002_WebDependencyInjectionBuildsAndResolvesServices()
    {
        var services = new ServiceCollection();
        var tempDir = Path.Combine(Path.GetTempPath(), "CardMaker_Smoke_Web_" + Guid.NewGuid().ToString("N"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataRoot"] = tempDir,
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCardMakerInfrastructure(configuration, tempDir);

        services.AddDataProtection();
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddCardMakerIdentityStores()
            .AddDefaultTokenProviders();

        services.AddAuthorizationCore();
        services.AddScoped<ILoadingService, CardMaker.UI.Services.LoadingService>();
        services.AddScoped<IAssetUriService, CardMaker.Infrastructure.Assets.WebAssetUriService>();
        services.AddMemoryCache();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CardMakerDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DatabaseInitializer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAssetUriService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILoadingService>());
    }

    [Fact]
    public async Task TEST_E2E_001_CompleteCardLifecycleEndToEnd()
    {
        // 1. Setup in-memory SQLite and Seeder
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new CardMakerDbContext(options);
        db.Database.EnsureCreated();

        var seeder = new YuGiOhContentSeeder(db);
        await seeder.SeedAsync();

        // 2. Setup Services
        var assetStore = new NullAssetStore();
        var fontCatalog = new NullFontCatalog();
        var imageCache = new DecodedImageCache();
        var textEngine = new TextEngine();
        var renderer = new CardRenderer(textEngine);
        var resourceLoader = new RenderResourceLoader(db, assetStore, fontCatalog, imageCache);
        var previewService = new CardPreviewService(resourceLoader, renderer);
        var pdfExporter = new PdfExporter();
        var exportService = new CardExportService(db, resourceLoader, renderer, pdfExporter);
        var cardService = new CardService(db);

        const string userId = "e2e-user";
        var game = await db.Games.FirstAsync(g => g.Key == "yugioh");
        var ct = await db.CardTypes.Include(c => c.Templates).ThenInclude(t => t.Versions).FirstAsync(c => c.Key == "monster-normal");
        var templateVersion = ct.Templates.First().Versions.First();

        // 3. Create Card via CardService
        var created = await cardService.CreateCardAsync(new SaveCardRequest
        {
            Title = "Drago Finale E2E",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = templateVersion.Id,
            Values = new Dictionary<string, CardValue>
            {
                ["name"] = CardValue.FromText("Drago Finale E2E"),
                ["atk"] = CardValue.FromText("4500"),
                ["def"] = CardValue.FromText("3800"),
            },
            SelectedTraits = [],
        }, userId);

        Assert.NotEqual(Guid.Empty, created.Id);

        // 4. Retrieve List Summary
        var list = await cardService.GetUserCardsAsync(userId);
        Assert.Single(list);
        Assert.Equal("Drago Finale E2E", list[0].Title);

        // 5. Render Preview PNG via CardPreviewService
        var preview = await previewService.RenderAsync(new CardPreviewRequest
        {
            LayoutJson = templateVersion.LayoutJson,
            Values = created.Values,
            GameId = game.Id,
            Dpi = 150,
        });

        Assert.True(preview.Succeeded);
        Assert.NotNull(preview.Content);
        Assert.True(preview.Content.Length > 0);

        // 6. Export Card as PDF
        var export = await exportService.ExportCardAsync(created.Id, userId, new CardExportOptions
        {
            Format = RenderFormat.Pdf,
            BothFaces = false,
            Dpi = 150,
        });

        Assert.True(export.Succeeded);
        Assert.NotNull(export.Content);
        Assert.True(export.Content.Length > 0);
        Assert.Equal("application/pdf", export.ContentType);
        Assert.Equal("Drago_Finale_E2E.pdf", export.FileName);
    }

    private sealed class NullAssetStore : IAssetStore
    {
        public Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredBlob(string.Empty, 0));

        public Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(new MemoryStream());

        public bool Exists(string sha256) => false;

        public Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class NullFontCatalog : IFontCatalog
    {
        public Task<IReadOnlyList<Domain.Assets.FontAsset>> ListAsync(Guid? gameId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Domain.Assets.FontAsset>>([]);

        public Task<Domain.Assets.FontAsset?> FindByAliasAsync(Guid? gameId, string alias, CancellationToken cancellationToken = default) =>
            Task.FromResult<Domain.Assets.FontAsset?>(null);

        public Task<byte[]?> GetBytesAsync(Guid fontId, CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>(null);

        public Task<byte[]?> GetBytesByAliasAsync(Guid? gameId, string roleAlias, CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>(null);

        public Task<FontRegistrationOutcome> RegisterAsync(Stream content, FontRegistrationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FontRegistrationOutcome(false, null, "unsupported"));

        public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
