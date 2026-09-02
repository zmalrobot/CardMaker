using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardMaker.Application.Abstractions;
using CardMaker.Application.Content;
using CardMaker.Domain.Assets;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Games;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Content;

internal sealed class PackageManifest
{
    public int SchemaVersion { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public DateTimeOffset ExportedAtUtc { get; set; }

    public PackageManifest() { }
    public PackageManifest(int schemaVersion, string gameKey, DateTimeOffset exportedAtUtc)
    {
        SchemaVersion = schemaVersion;
        GameKey = gameKey;
        ExportedAtUtc = exportedAtUtc;
    }
}

internal sealed class PackageGraph
{
    public Game Game { get; set; } = null!;
    public List<SymbolSet> SymbolSets { get; set; } = [];
    public List<OptionList> OptionLists { get; set; } = [];
    public List<Trait> Traits { get; set; } = [];
    public List<CardType> CardTypes { get; set; } = [];
    public List<Asset> Assets { get; set; } = [];
    public List<FontAsset> Fonts { get; set; } = [];

    public PackageGraph() { }
    public PackageGraph(
        Game game,
        List<SymbolSet> symbolSets,
        List<OptionList> optionLists,
        List<Trait> traits,
        List<CardType> cardTypes,
        List<Asset> assets,
        List<FontAsset> fonts)
    {
        Game = game;
        SymbolSets = symbolSets;
        OptionLists = optionLists;
        Traits = traits;
        CardTypes = cardTypes;
        Assets = assets;
        Fonts = fonts;
    }
}

/// <summary>
/// Formato <c>.cmpkg</c>: uno zip con <c>manifest.json</c>, <c>game.json</c> (il grafo dei
/// contenuti, per intero) e <c>assets/{sha256}</c> per i binari effettivamente presenti nello
/// store al momento dell'export. Import protetto da zip-slip: solo voci sotto <c>assets/</c>,
/// nessun <c>..</c> ne' percorso assoluto (ADR-025).
/// </summary>
public sealed class GamePackageService(CardMakerDbContext db, IAssetStore assetStore) : IGamePackageService
{
    private const int SchemaVersion = 1;
    private const string ManifestEntry = "manifest.json";
    private const string GraphEntry = "game.json";
    private const string AssetsPrefix = "assets/";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new LocalizedTextJsonConverter() },
    };

    public async Task<byte[]> ExportAsync(string gameKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);

        var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Key == gameKey, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Gioco '{gameKey}' non trovato.");

        var symbolSets = await db.SymbolSets.AsNoTracking().Include(s => s.Symbols)
            .Where(s => s.GameId == game.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var optionLists = await db.OptionLists.AsNoTracking().Include(o => o.Items)
            .Where(o => o.GameId == game.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var traits = await db.Traits.AsNoTracking().Where(t => t.GameId == game.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var cardTypes = await db.CardTypes.AsNoTracking()
            .Include(c => c.Fields)
            .Include(c => c.AllowedTraits)
            .Include(c => c.Templates).ThenInclude(t => t.Versions)
            .Where(c => c.GameId == game.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var fonts = await db.FontAssets.AsNoTracking().Where(f => f.GameId == game.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        var assetIds = new HashSet<Guid>();
        foreach (var set in symbolSets)
        {
            foreach (var symbol in set.Symbols)
            {
                if (symbol.AssetId is { } id)
                {
                    assetIds.Add(id);
                }
            }
        }

        foreach (var font in fonts)
        {
            assetIds.Add(font.AssetId);
        }

        foreach (var cardType in cardTypes)
        {
            if (cardType.IconAssetId is { } id)
            {
                assetIds.Add(id);
            }
        }

        var assets = assetIds.Count == 0
            ? []
            : await db.Assets.AsNoTracking().Where(a => assetIds.Contains(a.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);

        var graph = new PackageGraph(game, symbolSets, optionLists, traits, cardTypes, assets, fonts);
        var manifest = new PackageManifest(SchemaVersion, game.Key, DateTimeOffset.UtcNow);

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteJsonEntryAsync(zip, ManifestEntry, manifest, cancellationToken).ConfigureAwait(false);
            await WriteJsonEntryAsync(zip, GraphEntry, graph, cancellationToken).ConfigureAwait(false);

            foreach (var asset in assets)
            {
                var content = await assetStore.OpenReadAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
                if (content is null)
                {
                    continue;
                }

                await using (content.ConfigureAwait(false))
                {
                    var entry = zip.CreateEntry(AssetsPrefix + asset.Sha256, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await content.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return stream.ToArray();
    }

    public async Task<GameImportResult> ImportAsync(Stream cmpkgStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cmpkgStream);

        using var zip = new ZipArchive(cmpkgStream, ZipArchiveMode.Read);

        var manifestEntry = zip.GetEntry(ManifestEntry);
        var graphEntry = zip.GetEntry(GraphEntry);
        if (manifestEntry is null || graphEntry is null)
        {
            return new GameImportResult(false, "package.invalid", null);
        }

        var manifest = await ReadJsonEntryAsync<PackageManifest>(manifestEntry, cancellationToken).ConfigureAwait(false);
        if (manifest is null || manifest.SchemaVersion > SchemaVersion)
        {
            return new GameImportResult(false, "package.unsupportedSchemaVersion", null);
        }

        var graph = await ReadJsonEntryAsync<PackageGraph>(graphEntry, cancellationToken).ConfigureAwait(false);
        if (graph is null)
        {
            return new GameImportResult(false, "package.invalid", null);
        }

        var alreadyExists = await db.Games.AsNoTracking().AnyAsync(g => g.Key == graph.Game.Key, cancellationToken).ConfigureAwait(false);
        if (alreadyExists)
        {
            return new GameImportResult(false, "package.gameAlreadyExists", graph.Game.Key);
        }

        var assetIdRemap = new Dictionary<Guid, Guid>();
        var insertedAssetIds = new HashSet<Guid>();
        var assetsToInsert = new List<Asset>();

        foreach (var asset in graph.Assets)
        {
            var existing = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Sha256 == asset.Sha256, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                assetIdRemap[asset.Id] = existing.Id;
                continue;
            }

            // Zip-slip: solo voci sotto "assets/", nessun ".." e nessun percorso assoluto.
            var entry = zip.Entries.FirstOrDefault(e => IsSafeAssetEntry(e.FullName) && e.FullName == AssetsPrefix + asset.Sha256);
            if (entry is null)
            {
                // Il pacchetto non contiene il binario (non era presente nello store al momento dell'export):
                // si perde il legame binario ma non lo schema, che resta importabile.
                continue;
            }

            await using var entryStream = entry.Open();
            var stored = await assetStore.SaveAsync(entryStream, cancellationToken).ConfigureAwait(false);
            asset.Sha256 = stored.Sha256;
            insertedAssetIds.Add(asset.Id);
            assetsToInsert.Add(asset);
        }

        foreach (var set in graph.SymbolSets)
        {
            foreach (var symbol in set.Symbols)
            {
                symbol.AssetId = ResolveOptionalAssetId(symbol.AssetId, assetIdRemap, insertedAssetIds);
            }
        }

        foreach (var cardType in graph.CardTypes)
        {
            cardType.IconAssetId = ResolveOptionalAssetId(cardType.IconAssetId, assetIdRemap, insertedAssetIds);
        }

        var fontsToInsert = new List<FontAsset>();
        foreach (var font in graph.Fonts)
        {
            var resolved = ResolveOptionalAssetId(font.AssetId, assetIdRemap, insertedAssetIds);
            if (resolved is { } id)
            {
                font.AssetId = id;
                fontsToInsert.Add(font);
            }

            // Altrimenti: font senza asset risolvibile (non incluso nel pacchetto, non gia' presente):
            // scartato, non si puo' inserire in sicurezza un ruolo font senza il file.
        }

        db.Assets.AddRange(assetsToInsert);
        db.Games.Add(graph.Game);
        db.SymbolSets.AddRange(graph.SymbolSets);
        db.OptionLists.AddRange(graph.OptionLists);
        db.Traits.AddRange(graph.Traits);
        db.CardTypes.AddRange(graph.CardTypes);
        db.FontAssets.AddRange(fontsToInsert);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new GameImportResult(true, null, graph.Game.Key);
    }

    private static Guid? ResolveOptionalAssetId(Guid? originalId, Dictionary<Guid, Guid> remap, HashSet<Guid> insertedIds)
    {
        if (originalId is not { } id)
        {
            return null;
        }

        if (remap.TryGetValue(id, out var mapped))
        {
            return mapped;
        }

        return insertedIds.Contains(id) ? id : null;
    }

    private static bool IsSafeAssetEntry(string fullName) =>
        fullName.StartsWith(AssetsPrefix, StringComparison.Ordinal)
        && !fullName.Contains("..", StringComparison.Ordinal)
        && !Path.IsPathRooted(fullName);

    private static async Task WriteJsonEntryAsync<T>(ZipArchive zip, string entryName, T value, CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await JsonSerializer.SerializeAsync(entryStream, value, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T?> ReadJsonEntryAsync<T>(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var entryStream = entry.Open();
        return await JsonSerializer.DeserializeAsync<T>(entryStream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
