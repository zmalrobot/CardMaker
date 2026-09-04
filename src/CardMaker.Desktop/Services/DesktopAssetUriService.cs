using CardMaker.Application.Assets;
using CardMaker.Rendering.Fonts;
using Microsoft.Extensions.Caching.Memory;

namespace CardMaker.Desktop.Services;

/// <summary>
/// Risolutore URI per l'applicazione Desktop: genera Data URI (Base64) in-memory
/// con cache, evitando chiamate di rete, scheme custom difettosi di WebKitGTK o problemi di CORS.
/// </summary>
public sealed class DesktopAssetUriService(
    IAssetCatalog catalog,
    IFontCatalog fontCatalog,
    FontPreviewRenderer fontRenderer,
    IMemoryCache cache) : IAssetUriService
{
    public async ValueTask<string> GetAssetUriAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty)
        {
            return string.Empty;
        }

        var cacheKey = $"desktop_asset_{assetId}";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        var asset = await catalog.FindAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (asset is null)
        {
            return string.Empty;
        }

        using var stream = await catalog.OpenContentAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return string.Empty;
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var base64 = Convert.ToBase64String(ms.ToArray());
        var uri = $"data:{asset.ContentType};base64,{base64}";

        cache.Set(cacheKey, uri, TimeSpan.FromHours(1));
        return uri;
    }

    public async ValueTask<string> GetFontPreviewUriAsync(Guid fontId, string? sample = null, CancellationToken cancellationToken = default)
    {
        if (fontId == Guid.Empty)
        {
            return string.Empty;
        }

        var sampleText = string.IsNullOrWhiteSpace(sample) ? "CardMaker 12345" : sample;
        var cacheKey = $"desktop_font_{fontId}_{sampleText}";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        var bytes = await fontCatalog.GetBytesAsync(fontId, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return string.Empty;
        }

        var pngBytes = fontRenderer.Render(bytes, sampleText);
        var uri = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";

        cache.Set(cacheKey, uri, TimeSpan.FromHours(1));
        return uri;
    }
}
