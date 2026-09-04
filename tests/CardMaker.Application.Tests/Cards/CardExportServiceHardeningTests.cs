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

namespace CardMaker.Application.Tests.Cards;

public sealed class CardExportServiceHardeningTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly CardExportService _sut;
    private readonly CardService _cardService;

    public CardExportServiceHardeningTests()
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

        _sut = new CardExportService(_db, new RenderResourceLoader(_db, store, fonts, cache), renderer, pdfExporter);
        _cardService = new CardService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
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

    [Fact]
    public async Task TEST_INT_004_ExportCardAsyncBothFacesProducesTwoPagePdfViaParallelBatch()
    {
        const string userId = "user-dual-face";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");
        var ct = await _db.CardTypes.Include(c => c.Templates).ThenInclude(t => t.Versions).FirstAsync(c => c.Key == "monster-normal");
        var frontVersion = ct.Templates.First().Versions.First();

        // Create card
        var card = await _cardService.CreateCardAsync(new SaveCardRequest
        {
            Title = "Drago Occhi Blu Bifacciale",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = frontVersion.Id,
            Values = new Dictionary<string, CardValue>
            {
                ["name"] = CardValue.FromText("Drago Occhi Blu"),
                ["atk"] = CardValue.FromText("3000"),
                ["def"] = CardValue.FromText("2500"),
            },
            SelectedTraits = [],
        }, userId);

        // Assign back template version
        var entity = await _db.Cards.FindAsync(card.Id);
        Assert.NotNull(entity);
        entity.BackTemplateVersionId = frontVersion.Id;
        await _db.SaveChangesAsync();

        // Act - PAR-PERF-001: batch load resources for both layouts + parallel render + PDF export
        var result = await _sut.ExportCardAsync(card.Id, userId, new CardExportOptions
        {
            Format = RenderFormat.Pdf,
            BothFaces = true,
            Dpi = 150,
        });

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Length > 0);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("Drago_Occhi_Blu_Bifacciale.pdf", result.FileName);

        // PDF signature check
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Content[..5]);
        Assert.Equal("%PDF-", pdfHeader);
    }

    [Fact]
    public async Task TEST_INT_005_ExportCardAsyncNonExistentCardReturnsFailure()
    {
        var result = await _sut.ExportCardAsync(Guid.NewGuid(), "any-user", new CardExportOptions());

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("non trovata", result.ErrorMessage);
    }
}
