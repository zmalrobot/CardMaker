using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Rendering;
using CardMaker.Rendering.Placeholders;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CardMaker.Infrastructure.Rendering;

/// <summary>
/// Contratto per il caricamento delle risorse grafiche (immagini, simboli, font) prima del render.
/// Centralizza la logica di caricamento tra anteprima ed esportazione (DUP-005).
/// </summary>
public interface IRenderResourceLoader
{
    Task<PreloadedRenderResources> LoadResourcesAsync(
        CardLayout layout,
        IReadOnlyDictionary<string, CardValue> values,
        Guid? gameId,
        CancellationToken cancellationToken = default);
}

public sealed class RenderResourceLoader(
    CardMakerDbContext db,
    IAssetStore store,
    IFontCatalog fonts,
    IDecodedImageCache imageCache) : IRenderResourceLoader
{
    public async Task<PreloadedRenderResources> LoadResourcesAsync(
        CardLayout layout,
        IReadOnlyDictionary<string, CardValue> values,
        Guid? gameId,
        CancellationToken cancellationToken = default)
    {
        var resources = new PreloadedRenderResources();
        var (assetIds, assetKeys, fontAliases, symbols) = LayoutReferences.Collect(layout, values);

        if (assetIds.Count > 0)
        {
            var assets = await db.Assets.AsNoTracking()
                .Where(a => assetIds.Contains(a.Id))
                .Select(a => new { a.Id, a.Sha256 })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var asset in assets)
            {
                var image = await GetOrDecodeAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
                if (image is not null)
                {
                    resources.AddImage(asset.Id, image, owned: false);
                }
            }
        }

        foreach (var key in assetKeys)
        {
            var fileName = key + ".png";
            var placeholderName = key.StartsWith("placeholder-", StringComparison.Ordinal)
                ? fileName
                : "placeholder-" + key + ".png";

            var asset = await db.Assets.AsNoTracking()
                .Where(a => a.OriginalFileName == fileName || a.OriginalFileName == placeholderName)
                .OrderBy(a => a.OriginalFileName == fileName ? 0 : 1)
                .ThenByDescending(a => a.CreatedAtUtc)
                .Select(a => new { a.Sha256 })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (asset is not null)
            {
                var image = await GetOrDecodeAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
                if (image is not null)
                {
                    resources.AddImageKey(key, image, owned: false);
                }
            }
        }

        foreach (var (setKey, symbolKey) in symbols)
        {
            var symbol = await db.Symbols.AsNoTracking()
                .Include(s => s.SymbolSet)
                .Include(s => s.Asset)
                .Where(s => s.SymbolSet.Key == setKey && s.Key == symbolKey && s.Asset != null)
                .Select(s => new { Sha = s.Asset!.Sha256 })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (symbol is not null)
            {
                var image = await GetOrDecodeAsync(symbol.Sha, cancellationToken).ConfigureAwait(false);
                if (image is not null)
                {
                    resources.AddSymbol(setKey, symbolKey, image, owned: false);
                    continue;
                }
            }

            // Fallback 1: cerca asset con nome file segnaposto
            var symbolFileName = $"placeholder-symbol-{setKey}-{symbolKey}.png";
            var fallbackAsset = await db.Assets.AsNoTracking()
                .Where(a => a.OriginalFileName == symbolFileName)
                .Select(a => new { a.Sha256 })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (fallbackAsset is not null)
            {
                var fallbackImg = await GetOrDecodeAsync(fallbackAsset.Sha256, cancellationToken).ConfigureAwait(false);
                if (fallbackImg is not null)
                {
                    resources.AddSymbol(setKey, symbolKey, fallbackImg, owned: false);
                    continue;
                }
            }

            // Fallback 2: genera al volo il simbolo procedurale
            try
            {
                var placeholderGen = new PlaceholderSymbolGenerator();
                var generatedBytes = placeholderGen.Generate(setKey, symbolKey, 256);
                resources.AddSymbol(setKey, symbolKey, generatedBytes);
            }
            catch
            {
                // Ignora se non gestito dal generatore di segnaposto
            }
        }

        foreach (var alias in fontAliases)
        {
            var bytes = await fonts.GetBytesByAliasAsync(gameId, alias, cancellationToken).ConfigureAwait(false);
            if (bytes is not null)
            {
                resources.AddFont(alias, bytes);
            }
        }

        return resources;
    }

    private async Task<SKImage?> GetOrDecodeAsync(string sha256, CancellationToken cancellationToken)
    {
        var cached = imageCache.TryGet(sha256);
        if (cached is not null)
        {
            return cached;
        }

        var stream = await store.OpenReadAsync(sha256, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            using var data = SKData.CreateCopy(buffer.ToArray());
            var image = SKImage.FromEncodedData(data);
            if (image is not null)
            {
                imageCache.Store(sha256, image);
            }
            return image;
        }
    }
}
