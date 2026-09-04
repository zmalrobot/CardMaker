using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests.Storage;

public sealed class FontServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly MemoryAssetStore _store;
    private readonly FontService _sut;
    private readonly Guid _gameId = Guid.NewGuid();

    public FontServiceTests()
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
            Key = "test-game",
            Name = LocalizedText.From("Test Game"),
        };
        _db.Games.Add(game);
        _db.SaveChanges();

        _store = new MemoryAssetStore();
        _sut = new FontService(_db, new NullAssetCatalog(), _store, new FakeFontProcessor());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class MemoryAssetStore : IAssetStore
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

    private sealed class NullAssetCatalog : IAssetCatalog
    {
        public Task<AssetUploadOutcome> UploadAsync(Stream content, AssetUploadRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<Asset?> FindAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Asset?>(null);
        public Task<IReadOnlyList<Asset>> ListAsync(Guid? gameId = null, int take = 100, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Asset>>([]);
        public Task<Stream?> OpenContentAsync(string sha256, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }

    private sealed class FakeFontProcessor : IFontProcessor
    {
        public FontInfo? Probe(byte[] source) => new("TestFamily", "Regular", 400, false);
    }

    [Fact]
    public async Task TEST_FS_006_GetBytesByAliasAsyncReturnsEmbeddedFallbackFont()
    {
        // Act - Request an embedded alias (e.g. card-name)
        var bytes = await _sut.GetBytesByAliasAsync(_gameId, "card-name");

        // Assert - FS-PERF-003 & CACHE-PERF-001: Returns embedded font bytes and caches
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task TEST_FS_007_GetBytesByAliasHitsInMemoryCacheOnRepeatedCalls()
    {
        // Act
        var firstCall = await _sut.GetBytesByAliasAsync(_gameId, "card-name");
        var secondCall = await _sut.GetBytesByAliasAsync(_gameId, "card-name");

        // Assert - CACHE-PERF-001: Same byte reference returned from memory cache
        Assert.NotNull(firstCall);
        Assert.Same(firstCall, secondCall);
    }

    [Fact]
    public async Task TEST_FS_008_RemoveAsyncInvalidatesInMemoryFontBytesCache()
    {
        // Arrange - Register an asset and font in DB
        var fontBytes = "FakeFontBytesContent"u8.ToArray();
        var blob = await _store.SaveAsync(new MemoryStream(fontBytes));

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "custom.ttf",
            Sha256 = blob.Sha256,
            ByteSize = fontBytes.Length,
            ContentType = "font/ttf",
            Category = AssetCategory.Font,
            GameId = _gameId,
            UploadedByUserId = "test-user",
        };
        _db.Assets.Add(asset);

        var fontAsset = new FontAsset
        {
            Id = Guid.NewGuid(),
            AssetId = asset.Id,
            GameId = _gameId,
            Alias = "custom-alias",
            FamilyName = "Custom Family",
            StyleName = "Regular",
            Weight = 400,
        };
        _db.FontAssets.Add(fontAsset);
        await _db.SaveChangesAsync();

        // Populate cache
        var loaded = await _sut.GetBytesByAliasAsync(_gameId, "custom-alias");
        Assert.NotNull(loaded);
        Assert.Equal(fontBytes, loaded);

        // Act - Remove font asset
        var removed = await _sut.RemoveAsync(fontAsset.Id);
        Assert.True(removed);

        // Next request should hit fallback or return null, not stale cache
        var postRemove = await _sut.GetBytesByAliasAsync(_gameId, "custom-alias");
        // It should either return embedded fallback or null, but NOT the deleted custom bytes
        if (postRemove is not null)
        {
            Assert.NotEqual(fontBytes, postRemove);
        }
    }

    [Fact]
    public async Task TEST_FS_009_GetBytesByAliasReturnsNullForInvalidAlias()
    {
        var result = await _sut.GetBytesByAliasAsync(_gameId, "   ");
        Assert.Null(result);
    }
}
