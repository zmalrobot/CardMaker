using CardMaker.Application.Assets;
using CardMaker.Contracts.Geometry;
using CardMaker.Domain.Assets;
using CardMaker.Rendering.Placeholders;

namespace CardMaker.Infrastructure.Storage;

/// <summary>
/// Produce i frame segnaposto e li registra come asset normali.
/// Serve a sviluppare e collaudare l'intera catena prima che esistano gli asset reali:
/// stesse misure della specifica, quindi la sostituzione sara' un semplice ricaricamento.
/// </summary>
public sealed class PlaceholderSeeder(PlaceholderFrameGenerator generator, IAssetCatalog catalog) : IPlaceholderSeeder
{
    public async Task<PlaceholderSeedResult> SeedYuGiOhAsync(
        string? userId = null,
        bool showGuides = false,
        CancellationToken cancellationToken = default)
    {
        var geometry = CardGeometry.YuGiOh();
        var created = 0;
        var existing = 0;
        var keys = new List<string>();

        foreach (var baseSpec in PlaceholderFrameSpec.YuGiOhSet())
        {
            var spec = baseSpec with { ShowGuides = showGuides };
            var png = generator.Generate(spec, geometry);

            var before = await catalog.ListAsync(null, 500, cancellationToken).ConfigureAwait(false);
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

        return new PlaceholderSeedResult(created, existing, keys);
    }
}
