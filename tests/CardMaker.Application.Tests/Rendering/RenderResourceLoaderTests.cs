using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Assets;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Symbols;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Xunit;

namespace CardMaker.Application.Tests.Rendering;

public sealed class RenderResourceLoaderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly TestAssetStore _store;
    private readonly DecodedImageCache _imageCache;
    private readonly RenderResourceLoader _sut;
    private readonly Guid _gameId = Guid.NewGuid();

    public RenderResourceLoaderTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new CardMakerDbContext(options);
        _db.Database.EnsureCreated();

        var game = new Game
        {
            Id = _gameId,
            Key = "yugioh",
            Name = LocalizedText.From("Yu-Gi-Oh!"),
        };
        _db.Games.Add(game);
        _db.SaveChanges();

        _store = new TestAssetStore();
        _imageCache = new DecodedImageCache();
        _sut = new RenderResourceLoader(_db, _store, new NullFontCatalog(), _imageCache);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static byte[] Create1x1Png()
    {
        using var bitmap = new SKBitmap(1, 1);
        bitmap.SetPixel(0, 0, SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed class TestAssetStore : IAssetStore
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var bytes = ms.ToArray();
            var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
            Files[hash] = bytes;
            return Task.FromResult(new StoredBlob(hash, bytes.Length));
        }

        public Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(sha256, out var bytes))
            {
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            }

            return Task.FromResult<Stream?>(null);
        }

        public bool Exists(string sha256) => Files.ContainsKey(sha256);

        public Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default) =>
            Task.FromResult(Files.Remove(sha256));
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
    public async Task TEST_DB_001_LoadResourcesAsyncBatchesAssetKeysCorrectly()
    {
        // Arrange - Seed multiple assets matching asset keys
        var pngBytes = Create1x1Png();
        var blob1 = await _store.SaveAsync(new MemoryStream(pngBytes));
        var blob2 = await _store.SaveAsync(new MemoryStream(pngBytes));

        _db.Assets.AddRange(
            new Asset
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "frame-spell.png",
                Sha256 = blob1.Sha256,
                ByteSize = pngBytes.Length,
                ContentType = "image/png",
                Category = AssetCategory.Frame,
                GameId = _gameId,
                UploadedByUserId = "user-1",
            },
            new Asset
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "frame-trap.png",
                Sha256 = blob2.Sha256,
                ByteSize = pngBytes.Length,
                ContentType = "image/png",
                Category = AssetCategory.Frame,
                GameId = _gameId,
                UploadedByUserId = "user-1",
            });
        await _db.SaveChangesAsync();

        var layout = new CardLayout
        {
            Layers =
            [
                new StaticImageLayer { Id = "l1", AssetKey = "frame-spell" },
                new StaticImageLayer { Id = "l2", AssetKey = "frame-trap" }
            ]
        };

        // Act - DB-PERF-001: batch query for asset keys
        using var resources = await _sut.LoadResourcesAsync(layout, new Dictionary<string, CardValue>(), _gameId);

        // Assert
        Assert.NotNull(resources.GetImageByKey("frame-spell"));
        Assert.NotNull(resources.GetImageByKey("frame-trap"));
        Assert.Null(resources.GetImageByKey("frame-monster"));
    }

    [Fact]
    public async Task TEST_DB_002_LoadResourcesAsyncBatchesSymbolsCorrectly()
    {
        // Arrange - Seed symbol sets and symbols
        var pngBytes = Create1x1Png();
        var bDark = await _store.SaveAsync(new MemoryStream(pngBytes));
        var bLight = await _store.SaveAsync(new MemoryStream(pngBytes));

        var darkAsset = new Asset
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "dark.png",
            Sha256 = bDark.Sha256,
            ByteSize = pngBytes.Length,
            ContentType = "image/png",
            Category = AssetCategory.Symbol,
            GameId = _gameId,
            UploadedByUserId = "user-1",
        };
        var lightAsset = new Asset
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "light.png",
            Sha256 = bLight.Sha256,
            ByteSize = pngBytes.Length,
            ContentType = "image/png",
            Category = AssetCategory.Symbol,
            GameId = _gameId,
            UploadedByUserId = "user-1",
        };
        _db.Assets.AddRange(darkAsset, lightAsset);

        var symbolSet = new SymbolSet
        {
            Id = Guid.NewGuid(),
            GameId = _gameId,
            Key = "attributes",
            Name = LocalizedText.From("Attributi"),
        };
        _db.SymbolSets.Add(symbolSet);

        _db.Symbols.AddRange(
            new Symbol
            {
                Id = Guid.NewGuid(),
                SymbolSetId = symbolSet.Id,
                Key = "DARK",
                Name = LocalizedText.From("Oscurità"),
                AssetId = darkAsset.Id,
            },
            new Symbol
            {
                Id = Guid.NewGuid(),
                SymbolSetId = symbolSet.Id,
                Key = "LIGHT",
                Name = LocalizedText.From("Luce"),
                AssetId = lightAsset.Id,
            });
        await _db.SaveChangesAsync();

        var layout = new CardLayout
        {
            Layers =
            [
                new SymbolSlotLayer { Id = "sym-1", SymbolSetKey = "attributes", SymbolKey = "DARK" },
                new SymbolSlotLayer { Id = "sym-2", SymbolSetKey = "attributes", SymbolKey = "LIGHT" }
            ]
        };

        // Act - DB-PERF-002: batch query for symbols
        using var resources = await _sut.LoadResourcesAsync(layout, new Dictionary<string, CardValue>(), _gameId);

        // Assert
        Assert.NotNull(resources.GetSymbol("attributes", "DARK"));
        Assert.NotNull(resources.GetSymbol("attributes", "LIGHT"));
        Assert.Null(resources.GetSymbol("attributes", "EARTH"));
    }

    [Fact]
    public async Task TEST_INT_001_LoadResourcesAsyncMultiLayoutUnifiesFrontAndBackReferences()
    {
        // Arrange
        var pngBytes = Create1x1Png();
        var blob = await _store.SaveAsync(new MemoryStream(pngBytes));
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "shared.png",
            Sha256 = blob.Sha256,
            ByteSize = pngBytes.Length,
            ContentType = "image/png",
            Category = AssetCategory.Artwork,
            GameId = _gameId,
            UploadedByUserId = "user-1",
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        var frontLayout = new CardLayout
        {
            Layers = [new StaticImageLayer { Id = "front-art", AssetId = asset.Id }]
        };
        var backLayout = new CardLayout
        {
            Layers = [new StaticImageLayer { Id = "back-art", AssetId = asset.Id }]
        };

        // Act - PAR-PERF-001: multi-layout batch loading
        using var sharedResources = await _sut.LoadResourcesAsync([frontLayout, backLayout], new Dictionary<string, CardValue>(), _gameId);

        // Assert - single shared resources object resolves asset for both layouts
        var img = sharedResources.GetImage(asset.Id);
        Assert.NotNull(img);
    }
}
