using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using CardMaker.Application.Abstractions;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Application.Tests;

internal sealed class InMemoryAssetStore : IAssetStore
{
    private readonly Dictionary<string, byte[]> _blobs = [];

    public Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        var bytes = ms.ToArray();
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        _blobs[hash] = bytes;
        return Task.FromResult(new StoredBlob(hash, bytes.Length));
    }

    public Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default)
    {
        if (_blobs.TryGetValue(sha256, out var bytes))
        {
            return Task.FromResult<Stream?>(new MemoryStream(bytes));
        }

        return Task.FromResult<Stream?>(null);
    }

    public bool Exists(string sha256) => _blobs.ContainsKey(sha256);

    public Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default) =>
        Task.FromResult(_blobs.Remove(sha256));
}

public class GamePackageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CardMakerDbContext> _options;
    private readonly InMemoryAssetStore _assetStore;

    public GamePackageServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new CardMakerDbContext(_options);
        db.Database.EnsureCreated();

        _assetStore = new InMemoryAssetStore();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EsportaEImportaPacchettoRoundtrip()
    {
        // 1. Popola il DB con il seed Yu-Gi-Oh!
        await using (var db = new CardMakerDbContext(_options))
        {
            var seeder = new YuGiOhContentSeeder(db);
            await seeder.SeedAsync();

            // Aggiungi un asset finto e associalo al primo simbolo
            var blob = await _assetStore.SaveAsync(new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03]));
            var asset = new Asset
            {
                Sha256 = blob.Sha256,
                ByteSize = blob.ByteSize,
                OriginalFileName = "attribute-dark.png",
                ContentType = "image/png",
                Category = AssetCategory.Symbol,
                LicenseNote = "Test license",
            };
            db.Assets.Add(asset);

            var firstSymbol = await db.Symbols.FirstAsync();
            firstSymbol.AssetId = asset.Id;
            await db.SaveChangesAsync();
        }

        // 2. Esporta il pacchetto .cmpkg
        byte[] packageBytes;
        await using (var db = new CardMakerDbContext(_options))
        {
            var packageService = new GamePackageService(db, _assetStore);
            packageBytes = await packageService.ExportAsync("yugioh");
        }

        Assert.NotEmpty(packageBytes);

        // Verifica struttura zip
        using (var zip = new ZipArchive(new MemoryStream(packageBytes), ZipArchiveMode.Read))
        {
            Assert.NotNull(zip.GetEntry("manifest.json"));
            Assert.NotNull(zip.GetEntry("game.json"));
            Assert.Contains(zip.Entries, e => e.FullName.StartsWith("assets/", StringComparison.Ordinal));
        }

        // 3. Importa in un secondo database pulito
        using var secondConnection = new SqliteConnection("DataSource=:memory:");
        secondConnection.Open();
        var secondOptions = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(secondConnection)
            .Options;

        using (var secondDb = new CardMakerDbContext(secondOptions))
        {
            secondDb.Database.EnsureCreated();
        }

        var secondAssetStore = new InMemoryAssetStore();

        await using (var secondDb = new CardMakerDbContext(secondOptions))
        {
            var secondPackageService = new GamePackageService(secondDb, secondAssetStore);
            var importResult = await secondPackageService.ImportAsync(new MemoryStream(packageBytes));

            Assert.True(importResult.Succeeded);
            Assert.Null(importResult.ErrorCode);
            Assert.Equal("yugioh", importResult.GameKey);

            // Verifica che tutte le entità siano state inserite
            Assert.Equal(1, await secondDb.Games.CountAsync());
            Assert.Equal(26, await secondDb.CardTypes.CountAsync());
            Assert.Equal(28, await secondDb.Templates.CountAsync());
            Assert.Equal(28, await secondDb.TemplateVersions.CountAsync());
            Assert.Equal(5, await secondDb.SymbolSets.CountAsync());
            Assert.Equal(4, await secondDb.OptionLists.CountAsync());
            Assert.Equal(6, await secondDb.Traits.CountAsync());
            Assert.Equal(1, await secondDb.Assets.CountAsync());

            // Verifica che l'asset binario sia presente nel secondo store
            var importedAsset = await secondDb.Assets.FirstAsync();
            Assert.True(secondAssetStore.Exists(importedAsset.Sha256));
        }
    }

    [Fact]
    public async Task ImportFallisceSeGiocoGiaEsistente()
    {
        await using var db = new CardMakerDbContext(_options);
        var seeder = new YuGiOhContentSeeder(db);
        await seeder.SeedAsync();

        var packageService = new GamePackageService(db, _assetStore);
        var packageBytes = await packageService.ExportAsync("yugioh");

        // Riprova l'import nello stesso DB dove "yugioh" esiste già
        var importResult = await packageService.ImportAsync(new MemoryStream(packageBytes));

        Assert.False(importResult.Succeeded);
        Assert.Equal("package.gameAlreadyExists", importResult.ErrorCode);
    }

    [Fact]
    public async Task ImportRifiutaPacchettoInvalidoOVersioneSchemaNonSupportata()
    {
        await using var db = new CardMakerDbContext(_options);
        var packageService = new GamePackageService(db, _assetStore);

        // 1. Stream zip vuoto
        using var emptyZipStream = new MemoryStream();
        using (var zip = new ZipArchive(emptyZipStream, ZipArchiveMode.Create, true))
        {
            // Senza manifest né game.json
        }
        emptyZipStream.Position = 0;

        var invalidResult = await packageService.ImportAsync(emptyZipStream);
        Assert.False(invalidResult.Succeeded);
        Assert.Equal("package.invalid", invalidResult.ErrorCode);

        // 2. Manifest con SchemaVersion futura
        using var futureVersionStream = new MemoryStream();
        using (var zip = new ZipArchive(futureVersionStream, ZipArchiveMode.Create, true))
        {
            var manifestEntry = zip.CreateEntry("manifest.json");
            await using (var entryStream = manifestEntry.Open())
            {
                await JsonSerializer.SerializeAsync(entryStream, new { SchemaVersion = 999, GameKey = "future", ExportedAtUtc = DateTimeOffset.UtcNow });
            }
            var graphEntry = zip.CreateEntry("game.json");
            await using (var entryStream = graphEntry.Open())
            {
                await JsonSerializer.SerializeAsync(entryStream, new { });
            }
        }
        futureVersionStream.Position = 0;

        var unsupportedResult = await packageService.ImportAsync(futureVersionStream);
        Assert.False(unsupportedResult.Succeeded);
        Assert.Equal("package.unsupportedSchemaVersion", unsupportedResult.ErrorCode);
    }

    [Fact]
    public async Task ImportIgnoraVociZipSlipMaliziose()
    {
        // 1. Popola ed esporta un pacchetto valido
        byte[] validPackageBytes;
        await using (var db = new CardMakerDbContext(_options))
        {
            var seeder = new YuGiOhContentSeeder(db);
            await seeder.SeedAsync();
            var packageService = new GamePackageService(db, _assetStore);
            validPackageBytes = await packageService.ExportAsync("yugioh");
        }

        // 2. Modifica il pacchetto aggiungendo un'entry malevola zip-slip
        using var maliciousZipStream = new MemoryStream();
        using (var sourceZip = new ZipArchive(new MemoryStream(validPackageBytes), ZipArchiveMode.Read))
        using (var targetZip = new ZipArchive(maliciousZipStream, ZipArchiveMode.Create, true))
        {
            foreach (var entry in sourceZip.Entries)
            {
                var copy = targetZip.CreateEntry(entry.FullName);
                await using var sourceStream = entry.Open();
                await using var targetStream = copy.Open();
                await sourceStream.CopyToAsync(targetStream);
            }

            // Inserisci entry zip-slip che tenta di uscire da assets/
            var maliciousEntry = targetZip.CreateEntry("assets/../../evil.txt");
            await using (var s = maliciousEntry.Open())
            {
                s.WriteByte(0x42);
            }
        }

        maliciousZipStream.Position = 0;

        // 3. Importa in un DB pulito
        using var cleanConnection = new SqliteConnection("DataSource=:memory:");
        cleanConnection.Open();
        var cleanOptions = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(cleanConnection)
            .Options;

        using (var cleanDb = new CardMakerDbContext(cleanOptions))
        {
            cleanDb.Database.EnsureCreated();
        }

        var cleanAssetStore = new InMemoryAssetStore();
        await using (var cleanDb = new CardMakerDbContext(cleanOptions))
        {
            var cleanPackageService = new GamePackageService(cleanDb, cleanAssetStore);
            var result = await cleanPackageService.ImportAsync(maliciousZipStream);

            // L'import del grafo ha successo ma la voce zip-slip è stata ignorata/non salvata nello store
            Assert.True(result.Succeeded);
            Assert.False(cleanAssetStore.Exists("evil.txt"));
        }
    }
}

