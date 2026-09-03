namespace CardMaker.Application.Assets;

/// <summary>
/// Risolve l'URI di visualizzazione per un asset o per l'anteprima di un font.
/// Su Web genera URL relativi sicuri (/assets/{id}, /fonts/{id}/preview.png).
/// Su Desktop genera Data URI (data:image/...;base64,...) in-process per evitare
/// problemi di scheme custom e crash di WebKitGTK.
/// </summary>
public interface IAssetUriService
{
    ValueTask<string> GetAssetUriAsync(Guid assetId, CancellationToken cancellationToken = default);

    ValueTask<string> GetFontPreviewUriAsync(Guid fontId, string? sample = null, CancellationToken cancellationToken = default);
}
