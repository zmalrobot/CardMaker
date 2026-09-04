using CardMaker.Application.Assets;
using CardMaker.Contracts.Geometry;
using CardMaker.Domain.Assets;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Symbols;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Rendering.Placeholders;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Storage;

/// <summary>
/// Produce i frame segnaposto e i simboli procedurali e li registra come asset normali.
/// Permette di sviluppare e collaudare l'intera catena con carte segnaposto complete al 100%.
/// </summary>
public sealed class PlaceholderSeeder(
    PlaceholderFrameGenerator generator,
    PlaceholderSymbolGenerator symbolGenerator,
    IAssetCatalog catalog,
    CardMakerDbContext db) : IPlaceholderSeeder
{
    public async Task<PlaceholderSeedResult> SeedYuGiOhAsync(
        string? userId = null,
        bool showGuides = false,
        Guid? gameId = null,
        CancellationToken cancellationToken = default)
    {
        var game = gameId.HasValue
            ? await db.Games.FirstOrDefaultAsync(g => g.Id == gameId.Value, cancellationToken).ConfigureAwait(false)
            : await db.Games.FirstOrDefaultAsync(g => g.Key == YuGiOhSeedData.GameKey, cancellationToken).ConfigureAwait(false);

        var targetGameId = game?.Id ?? gameId;
        var geometry = game is not null
            ? new CardGeometry
            {
                WidthMm = (double)game.WidthMm,
                HeightMm = (double)game.HeightMm,
                BleedMm = (double)game.BleedMm,
                CornerRadiusMm = (double)game.CornerRadiusMm,
                SafeZoneMm = (double)game.SafeZoneMm,
                Dpi = game.DefaultDpi,
            }
            : CardGeometry.YuGiOh();

        return await SeedCoreAsync(
            targetGameId,
            geometry,
            PlaceholderFrameSpec.YuGiOhSet(),
            frameLicenseNote: "Generato proceduralmente da CardMaker: nessun materiale di terze parti.",
            frameSourcePrefix: "PlaceholderFrameGenerator",
            symbolLicenseNote: "Generato proceduralmente da CardMaker: segnaposto simbolo.",
            symbolSourcePrefix: "PlaceholderSymbolGenerator",
            userId,
            showGuides,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlaceholderSeedResult> SeedPokemonAsync(
        string? userId = null,
        bool showGuides = false,
        Guid? gameId = null,
        CancellationToken cancellationToken = default)
    {
        var game = gameId.HasValue
            ? await db.Games.FirstOrDefaultAsync(g => g.Id == gameId.Value, cancellationToken).ConfigureAwait(false)
            : await db.Games.FirstOrDefaultAsync(g => g.Key == PokemonSeedData.GameKey, cancellationToken).ConfigureAwait(false);

        var targetGameId = game?.Id ?? gameId;
        var geometry = CardGeometry.PokerSize();

        return await SeedCoreAsync(
            targetGameId,
            geometry,
            PlaceholderFrameSpec.PokemonSet(),
            frameLicenseNote: "Generato proceduralmente da CardMaker per Pokémon TCG.",
            frameSourcePrefix: "PlaceholderFrameGenerator",
            symbolLicenseNote: "Generato proceduralmente da CardMaker: segnaposto simbolo Pokémon.",
            symbolSourcePrefix: "PlaceholderSymbolGenerator",
            userId,
            showGuides,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlaceholderSeedResult> SeedMtgAsync(
        string? userId = null,
        bool showGuides = false,
        Guid? gameId = null,
        CancellationToken cancellationToken = default)
    {
        var targetGameId = gameId;
        if (!targetGameId.HasValue)
        {
            var game = await db.Games.FirstOrDefaultAsync(g => g.Key == "mtg", cancellationToken).ConfigureAwait(false);
            targetGameId = game?.Id;
        }

        var geometry = CardGeometry.PokerSize();

        return await SeedCoreAsync(
            targetGameId,
            geometry,
            PlaceholderFrameSpec.MtgSet(),
            frameLicenseNote: "Generato proceduralmente da CardMaker: segnaposto frame Magic.",
            frameSourcePrefix: "PlaceholderFrameGenerator",
            symbolLicenseNote: "Generato proceduralmente da CardMaker: segnaposto simbolo Magic.",
            symbolSourcePrefix: "PlaceholderSymbolGenerator",
            userId,
            showGuides,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<PlaceholderSeedResult> SeedCoreAsync(
        Guid? targetGameId,
        CardGeometry geometry,
        IReadOnlyList<PlaceholderFrameSpec> baseSpecs,
        string frameLicenseNote,
        string frameSourcePrefix,
        string symbolLicenseNote,
        string symbolSourcePrefix,
        string? userId,
        bool showGuides,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var existing = 0;
        var keys = new List<string>();

        // Pre-carica l'indice degli asset esistenti una sola volta per eliminare N+1 query (DB-001, PERF-001)
        var existingAssets = targetGameId.HasValue
            ? (await catalog.ListAsync(targetGameId, 5000, cancellationToken).ConfigureAwait(false))
                .Select(a => a.Id)
                .ToHashSet()
            : [];

        // 1. Frame Segnaposto - Generazione CPU-bound in parallelo (CON-002)
        var frameSpecs = baseSpecs.Select(b => b with { ShowGuides = showGuides }).ToList();
        var generatedFrames = new (PlaceholderFrameSpec Spec, byte[] Png)[frameSpecs.Count];
        Parallel.For(0, frameSpecs.Count, i =>
        {
            generatedFrames[i] = (frameSpecs[i], generator.Generate(frameSpecs[i], geometry));
        });

        foreach (var (spec, png) in generatedFrames)
        {
            var fileName = $"placeholder-{spec.Key}.png";
            using var stream = new MemoryStream(png, writable: false);
            var outcome = await catalog.UploadAsync(
                stream,
                new AssetUploadRequest
                {
                    FileName = fileName,
                    Category = spec.Layout == PlaceholderLayout.Back ? AssetCategory.CardBack : AssetCategory.Placeholder,
                    LicenseNote = frameLicenseNote,
                    SourceNote = $"{frameSourcePrefix}, {geometry.MasterWidthPx}x{geometry.MasterHeightPx} px @ {geometry.Dpi} DPI",
                    UploadedByUserId = userId,
                    GameId = targetGameId,
                    Kind = UploadKind.Image,
                },
                cancellationToken).ConfigureAwait(false);

            if (!outcome.Succeeded || outcome.Asset is null)
            {
                continue;
            }

            keys.Add(spec.Key);
            if (existingAssets.Contains(outcome.Asset.Id))
            {
                existing++;
            }
            else
            {
                existingAssets.Add(outcome.Asset.Id);
                created++;
            }
        }

        // 2. Simboli Procedurali Segnaposto
        if (targetGameId.HasValue)
        {
            var symbolSets = await db.SymbolSets
                .Include(s => s.Symbols)
                .Where(s => s.GameId == targetGameId.Value)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var symbolPairs = symbolSets
                .SelectMany(set => set.Symbols.Select(sym => (Set: set, Symbol: sym)))
                .ToList();

            var generatedSymbols = new (SymbolSet Set, Symbol Symbol, byte[] Png)[symbolPairs.Count];
            Parallel.For(0, symbolPairs.Count, i =>
            {
                var pair = symbolPairs[i];
                generatedSymbols[i] = (pair.Set, pair.Symbol, symbolGenerator.Generate(pair.Set.Key, pair.Symbol.Key));
            });

            foreach (var (set, symbol, symbolPng) in generatedSymbols)
            {
                var symbolFileName = $"placeholder-symbol-{set.Key}-{symbol.Key}.png";
                using var symStream = new MemoryStream(symbolPng, writable: false);
                var outcome = await catalog.UploadAsync(
                    symStream,
                    new AssetUploadRequest
                    {
                        FileName = symbolFileName,
                        Category = AssetCategory.Symbol,
                        LicenseNote = symbolLicenseNote,
                        SourceNote = $"{symbolSourcePrefix}, set: {set.Key}, symbol: {symbol.Key}",
                        UploadedByUserId = userId,
                        GameId = targetGameId,
                        Kind = UploadKind.Image,
                    },
                    cancellationToken).ConfigureAwait(false);

                if (outcome.Succeeded && outcome.Asset is not null)
                {
                    symbol.AssetId = outcome.Asset.Id;
                    keys.Add($"symbol:{set.Key}/{symbol.Key}");
                    if (existingAssets.Contains(outcome.Asset.Id))
                    {
                        existing++;
                    }
                    else
                    {
                        existingAssets.Add(outcome.Asset.Id);
                        created++;
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new PlaceholderSeedResult(created, existing, keys);
    }
}
