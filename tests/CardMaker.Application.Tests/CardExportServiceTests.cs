using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Application.Cards;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Infrastructure.Cards;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using CardMaker.Rendering;
using CardMaker.Rendering.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class CardExportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly CardExportService _sut;
    private readonly CardService _cardService;

    public CardExportServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new CardMakerDbContext(options);
        _db.Database.EnsureCreated();

        var seeder = new YuGiOhContentSeeder(_db);
        seeder.SeedAsync().GetAwaiter().GetResult();

        var store = new NullAssetStore();
        var fonts = new NullFontCatalog();
        var cache = new DecodedImageCache();
        var textEngine = new TextEngine();
        var renderer = new CardRenderer(textEngine);
        var pdfExporter = new PdfExporter();

        _sut = new CardExportService(_db, store, fonts, cache, renderer, pdfExporter);
        _cardService = new CardService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ExportCardAsyncPngFormatProducesValidImage()
    {
        const string userId = "user-export";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");
        var ct = await _db.CardTypes.Include(c => c.Templates).ThenInclude(t => t.Versions).FirstAsync(c => c.Key == "monster-normal");
        var tv = ct.Templates.First().Versions.First();

        var card = await _cardService.CreateCardAsync(new SaveCardRequest
        {
            Title = "Drago Alato",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = tv.Id,
            Values = new Dictionary<string, CardValue>
            {
                ["name"] = CardValue.FromText("Drago Alato"),
                ["atk"] = CardValue.FromText("1400"),
                ["def"] = CardValue.FromText("1200"),
            },
            SelectedTraits = [],
        }, userId);

        var result = await _sut.ExportCardAsync(card.Id, userId, new CardExportOptions
        {
            Format = RenderFormat.Png,
            Dpi = 300,
            IncludeBleed = false,
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("Drago_Alato.png", result.FileName);
    }

    [Fact]
    public async Task ExportCardAsyncPdfFormatProducesValidPdf()
    {
        const string userId = "user-export";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");
        var ct = await _db.CardTypes.Include(c => c.Templates).ThenInclude(t => t.Versions).FirstAsync(c => c.Key == "monster-normal");
        var tv = ct.Templates.First().Versions.First();

        var card = await _cardService.CreateCardAsync(new SaveCardRequest
        {
            Title = "Mago Spada",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = tv.Id,
            Values = new Dictionary<string, CardValue>
            {
                ["name"] = CardValue.FromText("Mago Spada"),
                ["atk"] = CardValue.FromText("1800"),
                ["def"] = CardValue.FromText("1500"),
            },
            SelectedTraits = [],
        }, userId);

        var result = await _sut.ExportCardAsync(card.Id, userId, new CardExportOptions
        {
            Format = RenderFormat.Pdf,
            Dpi = 600,
            IncludeBleed = true,
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("Mago_Spada.pdf", result.FileName);
        // Verify PDF magic header %PDF
        Assert.True(result.Content.Length > 4);
        Assert.Equal(0x25, result.Content[0]); // %
        Assert.Equal(0x50, result.Content[1]); // P
        Assert.Equal(0x44, result.Content[2]); // D
        Assert.Equal(0x46, result.Content[3]); // F
    }

    private sealed class NullAssetStore : IAssetStore
    {
        public Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredBlob(string.Empty, 0));

        public bool Exists(string sha256) => false;

        public Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class NullFontCatalog : IFontCatalog
    {
        public Task<IReadOnlyList<Domain.Assets.FontAsset>> ListAsync(Guid? gameId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Domain.Assets.FontAsset>>([]);

        public Task<Domain.Assets.FontAsset?> FindByAliasAsync(Guid? gameId, string alias, CancellationToken cancellationToken = default) =>
            Task.FromResult<Domain.Assets.FontAsset?>(null);

        public Task<byte[]?> GetBytesAsync(Guid fontId, CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>(null);

        public Task<byte[]?> GetBytesByAliasAsync(Guid? gameId, string alias, CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>(null);

        public Task<FontRegistrationOutcome> RegisterAsync(Stream content, FontRegistrationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FontRegistrationOutcome(false, null, "unsupported"));

        public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}

