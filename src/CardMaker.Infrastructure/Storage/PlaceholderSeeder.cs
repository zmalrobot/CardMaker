using CardMaker.Application.Assets;
using CardMaker.Contracts.Geometry;
using CardMaker.Domain.Assets;
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

        var created = 0;
        var existing = 0;
        var keys = new List<string>();

        // 1. Frame Segnaposto
        foreach (var baseSpec in PlaceholderFrameSpec.YuGiOhSet())
        {
            var spec = baseSpec with { ShowGuides = showGuides };
            var png = generator.Generate(spec, geometry);

            var before = await catalog.ListAsync(targetGameId, 500, cancellationToken).ConfigureAwait(false);
            var fileName = $"placeholder-{spec.Key}.png";

            using var stream = new MemoryStream(png, writable: false);
            var outcome = await catalog.UploadAsync(
                stream,
                new AssetUploadRequest
                {
                    FileName = fileName,
                    Category = spec.Layout == PlaceholderLayout.Back ? AssetCategory.CardBack : AssetCategory.Placeholder,
                    LicenseNote = "Generato proceduralmente da CardMaker: nessun materiale di terze parti.",
                    SourceNote = $"PlaceholderFrameGenerator, {geometry.MasterWidthPx}x{geometry.MasterHeightPx} px @ {geometry.Dpi} DPI",
                    UploadedByUserId = userId,
                    GameId = targetGameId,
                    Kind = UploadKind.Image,
                },
                cancellationToken).ConfigureAwait(false);

            if (!outcome.Succeeded)
            {
                continue;
            }

            keys.Add(spec.Key);
            if (before.Any(a => a.Id == outcome.Asset!.Id))
            {
                existing++;
            }
            else
            {
                created++;
            }
        }

        // 2. Simboli Procedurali Segnaposto (Attributi, Stelle, Frecce Link, Proprietà Magia/Trappola)
        if (targetGameId.HasValue)
        {
            var symbolSets = await db.SymbolSets
                .Include(s => s.Symbols)
                .Where(s => s.GameId == targetGameId.Value)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var set in symbolSets)
            {
                foreach (var symbol in set.Symbols)
                {
                    var symbolPng = symbolGenerator.Generate(set.Key, symbol.Key);
                    var symbolFileName = $"placeholder-symbol-{set.Key}-{symbol.Key}.png";

                    var before = await catalog.ListAsync(targetGameId, 500, cancellationToken).ConfigureAwait(false);

                    using var symStream = new MemoryStream(symbolPng, writable: false);
                    var outcome = await catalog.UploadAsync(
                        symStream,
                        new AssetUploadRequest
                        {
                            FileName = symbolFileName,
                            Category = AssetCategory.Symbol,
                            LicenseNote = "Generato proceduralmente da CardMaker: segnaposto simbolo.",
                            SourceNote = $"PlaceholderSymbolGenerator, set: {set.Key}, symbol: {symbol.Key}",
                            UploadedByUserId = userId,
                            GameId = targetGameId,
                            Kind = UploadKind.Image,
                        },
                        cancellationToken).ConfigureAwait(false);

                    if (outcome.Succeeded && outcome.Asset is not null)
                    {
                        symbol.AssetId = outcome.Asset.Id;
                        keys.Add($"symbol:{set.Key}/{symbol.Key}");
                        if (before.Any(a => a.Id == outcome.Asset.Id))
                        {
                            existing++;
                        }
                        else
                        {
                            created++;
                        }
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new PlaceholderSeedResult(created, existing, keys);
    }
}
