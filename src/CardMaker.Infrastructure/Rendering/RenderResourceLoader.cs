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

    Task<PreloadedRenderResources> LoadResourcesAsync(
        IEnumerable<CardLayout> layouts,
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
    public Task<PreloadedRenderResources> LoadResourcesAsync(
        CardLayout layout,
        IReadOnlyDictionary<string, CardValue> values,
        Guid? gameId,
        CancellationToken cancellationToken = default) =>
        LoadResourcesAsync([layout], values, gameId, cancellationToken);

    public async Task<PreloadedRenderResources> LoadResourcesAsync(
        IEnumerable<CardLayout> layouts,
        IReadOnlyDictionary<string, CardValue> values,
        Guid? gameId,
        CancellationToken cancellationToken = default)
    {
        var resources = new PreloadedRenderResources();
        var (assetIds, assetKeys, fontAliases, symbols) = LayoutReferences.Collect(layouts, values);

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

        if (assetKeys.Count > 0)
        {
            var targetFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in assetKeys)
            {
                targetFileNames.Add(key + ".png");
                targetFileNames.Add(key.StartsWith("placeholder-", StringComparison.Ordinal)
                    ? key + ".png"
                    : "placeholder-" + key + ".png");
            }

            var matchingAssets = await db.Assets.AsNoTracking()
                .Where(a => targetFileNames.Contains(a.OriginalFileName))
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => new { a.OriginalFileName, a.Sha256 })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var assetsByFile = matchingAssets
                .GroupBy(a => a.OriginalFileName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Sha256, StringComparer.OrdinalIgnoreCase);

            foreach (var key in assetKeys)
            {
                var fileName = key + ".png";
                var placeholderName = key.StartsWith("placeholder-", StringComparison.Ordinal)
                    ? fileName
                    : "placeholder-" + key + ".png";

                if (assetsByFile.TryGetValue(fileName, out var sha) ||
                    assetsByFile.TryGetValue(placeholderName, out sha))
                {
                    var image = await GetOrDecodeAsync(sha, cancellationToken).ConfigureAwait(false);
                    if (image is not null)
                    {
                        resources.AddImageKey(key, image, owned: false);
                    }
                }
            }
        }

        if (symbols.Count > 0)
        {
            var setKeys = symbols.Select(s => s.Set).Distinct().ToList();
            var symbolKeys = symbols.Select(s => s.Key).Distinct().ToList();

            var dbSymbols = await db.Symbols.AsNoTracking()
                .Where(s => setKeys.Contains(s.SymbolSet.Key) && symbolKeys.Contains(s.Key) && s.Asset != null)
                .Select(s => new { SetKey = s.SymbolSet.Key, s.Key, Sha = s.Asset!.Sha256 })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var symbolMap = dbSymbols
                .GroupBy(s => (s.SetKey, s.Key))
                .ToDictionary(g => g.Key, g => g.First().Sha);

            var missingForFallback = new List<(string Set, string Key)>();

            foreach (var (setKey, symbolKey) in symbols)
            {
                if (symbolMap.TryGetValue((setKey, symbolKey), out var sha))
                {
                    var image = await GetOrDecodeAsync(sha, cancellationToken).ConfigureAwait(false);
                    if (image is not null)
                    {
                        resources.AddSymbol(setKey, symbolKey, image, owned: false);
                        continue;
                    }
                }
                missingForFallback.Add((setKey, symbolKey));
            }

            if (missingForFallback.Count > 0)
            {
                var fallbackNames = missingForFallback
                    .Select(s => $"placeholder-symbol-{s.Set}-{s.Key}.png")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var fallbackAssets = await db.Assets.AsNoTracking()
                    .Where(a => fallbackNames.Contains(a.OriginalFileName))
                    .Select(a => new { a.OriginalFileName, a.Sha256 })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                var fallbackMap = fallbackAssets
                    .GroupBy(a => a.OriginalFileName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Sha256, StringComparer.OrdinalIgnoreCase);

                foreach (var (setKey, symbolKey) in missingForFallback)
                {
                    var symbolFileName = $"placeholder-symbol-{setKey}-{symbolKey}.png";
                    if (fallbackMap.TryGetValue(symbolFileName, out var fallbackSha))
                    {
                        var fallbackImg = await GetOrDecodeAsync(fallbackSha, cancellationToken).ConfigureAwait(false);
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
            using var buffer = stream.CanSeek ? new MemoryStream((int)stream.Length) : new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            using var data = SKData.CreateCopy(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
            var image = SKImage.FromEncodedData(data);
            if (image is not null)
            {
                imageCache.Store(sha256, image);
            }
            return image;
        }
    }
}
