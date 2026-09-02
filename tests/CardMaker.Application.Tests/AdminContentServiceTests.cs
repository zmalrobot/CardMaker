using System.Text;
using CardMaker.Application.Abstractions;
using CardMaker.Application.Admin;
using CardMaker.Domain.Cards;
using CardMaker.Infrastructure.Admin;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class AdminContentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly TestAssetStore _store;
    private readonly AdminContentService _sut;

    public AdminContentServiceTests()
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

        _store = new TestAssetStore();
        _sut = new AdminContentService(_db, _store);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GameLifecycleCreateUpdateReadDelete()
    {
        const string userId = "admin-1";

        // 1. Create
        var created = await _sut.SaveGameAsync(new SaveGameRequest
        {
            Key = "pokemon",
            NameIt = "Pokémon TCG",
            NameEn = "Pokemon TCG",
            WidthMm = 63m,
            HeightMm = 88m,
            BleedMm = 2m,
            SafeZoneMm = 3m,
            CornerRadiusMm = 3m,
            DefaultDpi = 600,
            IsPublished = true,
            SortOrder = 2,
        }, userId);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("pokemon", created.Key);
        Assert.Equal("Pokémon TCG", created.NameIt);

        // 2. Read
        var games = await _sut.GetGamesAsync();
        Assert.Contains(games, g => g.Key == "pokemon");

        // 3. Update
        var updated = await _sut.SaveGameAsync(new SaveGameRequest
        {
            Id = created.Id,
            Key = "pokemon",
            NameIt = "Pokémon Trading Card Game",
            NameEn = "Pokemon Trading Card Game",
            WidthMm = 63m,
            HeightMm = 88m,
            BleedMm = 2m,
            SafeZoneMm = 3m,
            CornerRadiusMm = 3m,
            DefaultDpi = 600,
            IsPublished = true,
            SortOrder = 2,
        }, userId);

        Assert.Equal("Pokémon Trading Card Game", updated.NameIt);

        // 4. Delete
        var deleted = await _sut.DeleteGameAsync(created.Id, userId);
        Assert.True(deleted);

        var gameAfterDel = await _sut.GetGameByIdAsync(created.Id);
        Assert.Null(gameAfterDel);
    }

    [Fact]
    public async Task SchemaEditorAddReorderDeleteField()
    {
        const string userId = "admin-1";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");

        // 1. Create CardType
        var ct = await _sut.SaveCardTypeAsync(new SaveCardTypeRequest
        {
            GameId = game.Id,
            Key = "token-monster",
            NameIt = "Segna-Mostro",
            NameEn = "Token Monster",
            SortOrder = 50,
        }, userId);

        Assert.NotEqual(Guid.Empty, ct.Id);

        // 2. Add Field 1
        var f1 = await _sut.SaveFieldDefinitionAsync(ct.Id, new SaveFieldDefinitionRequest
        {
            Key = "tokenName",
            LabelIt = "Nome Segnaposto",
            Kind = FieldKind.Text,
            IsRequired = true,
            SortOrder = 10,
        }, userId);

        // 3. Add Field 2
        var f2 = await _sut.SaveFieldDefinitionAsync(ct.Id, new SaveFieldDefinitionRequest
        {
            Key = "tokenAttack",
            LabelIt = "ATK",
            Kind = FieldKind.Integer,
            SortOrder = 20,
        }, userId);

        var details = await _sut.GetCardTypeByIdAsync(ct.Id);
        Assert.NotNull(details);
        Assert.Equal(2, details.Fields.Count);

        // 4. Reorder
        await _sut.ReorderFieldsAsync(ct.Id, [f2.Id, f1.Id], userId);
        var reordered = await _sut.GetCardTypeByIdAsync(ct.Id);
        Assert.NotNull(reordered);
        Assert.Equal(f2.Id, reordered.Fields[0].Id);
        Assert.Equal(f1.Id, reordered.Fields[1].Id);

        // 5. Delete field
        var deletedField = await _sut.DeleteFieldDefinitionAsync(f1.Id, userId);
        Assert.True(deletedField);

        var finalDetails = await _sut.GetCardTypeByIdAsync(ct.Id);
        Assert.NotNull(finalDetails);
        Assert.Single(finalDetails.Fields);
        Assert.Equal(f2.Id, finalDetails.Fields[0].Id);
    }

    [Fact]
    public async Task SafeAssetDeleteBlocksReferencedAsset()
    {
        const string userId = "admin-1";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");

        // Create asset
        var asset = new Domain.Assets.Asset
        {
            GameId = game.Id,
            Category = Domain.Assets.AssetCategory.Symbol,
            Sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            OriginalFileName = "test-symbol.png",
            ContentType = "image/png",
            ByteSize = 100,
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        // Create symbol set and symbol referencing the asset
        var symbolSet = await _sut.SaveSymbolSetAsync(new SaveSymbolSetRequest
        {
            GameId = game.Id,
            Key = "test-symbols",
            NameIt = "Simboli Test",
        }, userId);

        var symbol = await _sut.SaveSymbolAsync(symbolSet.Id, new SaveSymbolRequest
        {
            Key = "fire",
            NameIt = "FUOCO",
            AssetId = asset.Id,
        }, userId);

        // Attempt safe delete
        var usage = await _sut.CheckAssetUsageAsync(asset.Id);
        Assert.True(usage.IsInUse);
        Assert.NotEmpty(usage.UsageReasons);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SafeDeleteAssetAsync(asset.Id, userId));

        // Remove reference
        await _sut.DeleteSymbolAsync(symbol.Id, userId);

        var usageAfter = await _sut.CheckAssetUsageAsync(asset.Id);
        Assert.False(usageAfter.IsInUse);

        var deleted = await _sut.SafeDeleteAssetAsync(asset.Id, userId);
        Assert.True(deleted);
    }

    [Fact]
    public async Task ReplaceAssetBlobPreservesAssetIdAndUpdatesSha()
    {
        const string userId = "admin-1";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");

        var asset = new Domain.Assets.Asset
        {
            GameId = game.Id,
            Category = Domain.Assets.AssetCategory.Frame,
            Sha256 = "1111111111111111111111111111111111111111111111111111111111111111",
            OriginalFileName = "frame-old.png",
            ContentType = "image/png",
            ByteSize = 50,
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        var newBytes = Encoding.UTF8.GetBytes("new image content replacement");
        using var stream = new MemoryStream(newBytes);

        var replaceResult = await _sut.ReplaceAssetBlobAsync(asset.Id, stream, "frame-new.png", userId);
        Assert.True(replaceResult.Succeeded);
        Assert.NotNull(replaceResult.NewSha256);
        Assert.NotEqual("1111111111111111111111111111111111111111111111111111111111111111", replaceResult.NewSha256);

        var updatedAsset = await _db.Assets.FindAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(replaceResult.NewSha256, updatedAsset.Sha256);
        Assert.Equal("frame-new.png", updatedAsset.OriginalFileName);
        Assert.Equal(newBytes.Length, updatedAsset.ByteSize);
    }

    [Fact]
    public async Task AuditLogsAreRecordedAndRetrievable()
    {
        const string userId = "admin-audit-user";
        await _sut.SaveGameAsync(new SaveGameRequest
        {
            Key = "audit-game",
            NameIt = "Audit Game",
        }, userId);

        var logs = await _sut.GetAuditLogsAsync(50);
        Assert.NotEmpty(logs);
        var entry = logs.First(l => l.UserId == userId);
        Assert.Equal("Game.Create", entry.Action);
        Assert.Equal("Game", entry.EntityName);
    }

    private sealed class TestAssetStore : IAssetStore
    {
        private readonly Dictionary<string, byte[]> _storage = [];

        public Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default)
        {
            if (_storage.TryGetValue(sha256, out var bytes))
            {
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            }
            return Task.FromResult<Stream?>(null);
        }

        public async Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();
            var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            _storage[sha] = bytes;
            return new StoredBlob(sha, bytes.Length);
        }

        public bool Exists(string sha256) => _storage.ContainsKey(sha256);

        public Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default) =>
            Task.FromResult(_storage.Remove(sha256));
    }
}
