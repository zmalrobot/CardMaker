using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Application.Rendering;
using CardMaker.Contracts.Layout;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Rendering;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CardMaker.Infrastructure.Rendering;

public sealed class CardPreviewService(
    CardMakerDbContext db,
    IAssetStore store,
    IFontCatalog fonts,
    IDecodedImageCache imageCache,
    CardRenderer renderer) : ICardPreviewService
{
    public async Task<CardPreviewResult> RenderAsync(
        CardPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CardLayout? layout;
        try
        {
            layout = LayoutSerializer.Deserialize(request.LayoutJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return CardPreviewResult.Fail([$"Layout non leggibile: {ex.Message}"]);
        }

        var validation = LayoutSerializer.Validate(layout);
        if (!validation.IsValid)
        {
            return CardPreviewResult.Fail([.. validation.Issues.Select(i => $"{i.Code}: {i.Message}")]);
        }

        using var resources = await LoadResourcesAsync(layout!, request, cancellationToken).ConfigureAwait(false);

        var result = renderer.Render(new CardRenderRequest
        {
            Layout = layout!,
            Values = request.Values,
            Resources = resources,
            Dpi = Math.Clamp(request.Dpi, 48, 1200),
            IncludeBleed = request.IncludeBleed,
            RoundCorners = request.RoundCorners,
            ShowGuides = request.ShowGuides,
            Format = string.Equals(request.Format, "jpg", StringComparison.OrdinalIgnoreCase)
                ? RenderOutputFormat.Jpeg
                : RenderOutputFormat.Png,
        });

        return new CardPreviewResult(
            true,
            result.Content,
            result.ContentType,
            result.WidthPx,
            result.HeightPx,
            [.. result.Warnings.Select(w => new PreviewWarning(w.Code, w.Message, w.LayerId))],
            result.Duration.TotalMilliseconds,
            []);
    }

    private async Task<PreloadedRenderResources> LoadResourcesAsync(
        CardLayout layout,
        CardPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var resources = new PreloadedRenderResources();
        var (assetIds, assetKeys, fontAliases, symbols) = LayoutReferences.Collect(layout, request.Values);

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
            // Chiave logica: cerca prima il file esatto (es. "monster-effect.png"), poi il segnaposto ("placeholder-monster-effect.png")
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

            if (asset is null)
            {
                continue;
            }

            var image = await GetOrDecodeAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
            if (image is not null)
            {
                resources.AddImageKey(key, image, owned: false);
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

            if (symbol is null)
            {
                continue;
            }

            var image = await GetOrDecodeAsync(symbol.Sha, cancellationToken).ConfigureAwait(false);
            if (image is not null)
            {
                resources.AddSymbol(setKey, symbolKey, image, owned: false);
            }
        }

        foreach (var alias in fontAliases)
        {
            var bytes = await fonts.GetBytesByAliasAsync(request.GameId, alias, cancellationToken)
                .ConfigureAwait(false);
            if (bytes is not null)
            {
                resources.AddFont(alias, bytes);
            }
        }

        return resources;
    }

    /// <summary>Decodifica una volta sola per SHA-256: la stessa immagine su piu' render la riusa (F2).</summary>
    private async Task<SKImage?> GetOrDecodeAsync(string sha256, CancellationToken cancellationToken)
    {
        var cached = imageCache.TryGet(sha256);
        if (cached is not null)
        {
            return cached;
        }

        var bytes = await ReadAsync(sha256, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }

        using var data = SKData.CreateCopy(bytes);
        var image = SKImage.FromEncodedData(data);
        if (image is not null)
        {
            imageCache.Store(sha256, image);
        }

        return image;
    }

    private async Task<byte[]?> ReadAsync(string sha256, CancellationToken cancellationToken)
    {
        var stream = await store.OpenReadAsync(sha256, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer.ToArray();
        }
    }
}
