using CardMaker.Application.Assets;

namespace CardMaker.Infrastructure.Assets;

/// <summary>
/// Risolutore URI per l'applicazione Web: restituisce gli endpoint HTTP di ASP.NET Core.
/// </summary>
public sealed class WebAssetUriService : IAssetUriService
{
    public ValueTask<string> GetAssetUriAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty)
        {
            return ValueTask.FromResult(string.Empty);
        }

        return ValueTask.FromResult($"/assets/{assetId}");
    }

    public ValueTask<string> GetFontPreviewUriAsync(Guid fontId, string? sample = null, CancellationToken cancellationToken = default)
    {
        if (fontId == Guid.Empty)
        {
            return ValueTask.FromResult(string.Empty);
        }

        var url = $"/fonts/{fontId}/preview.png";
        if (!string.IsNullOrWhiteSpace(sample))
        {
            url += $"?sample={Uri.EscapeDataString(sample)}";
        }

        return ValueTask.FromResult(url);
    }
}
