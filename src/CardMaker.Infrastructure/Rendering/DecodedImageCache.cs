using SkiaSharp;

namespace CardMaker.Infrastructure.Rendering;

/// <summary>
/// Cache condivisa fra le richieste di render: lo stesso frame o simbolo non va ridecodificato
/// a ogni anteprima. Chiave = SHA-256 dell'asset (identita' content-addressed gia' esistente).
/// </summary>
public interface IDecodedImageCache
{
    SKImage? TryGet(string sha256);

    void Store(string sha256, SKImage image);
}

public sealed class DecodedImageCache(int capacity = 256) : IDecodedImageCache
{
    private readonly LruCache<string, SKImage> _cache = new(capacity);

    public SKImage? TryGet(string sha256) => _cache.TryGet(sha256);

    public void Store(string sha256, SKImage image) => _cache.Set(sha256, image);
}
