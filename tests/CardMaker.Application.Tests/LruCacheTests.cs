using CardMaker.Infrastructure.Rendering;
using SkiaSharp;

namespace CardMaker.Application.Tests;

public class LruCacheTests
{
    private static SKImage MakeImage(SKColor color)
    {
        var info = new SKImageInfo(4, 4);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(color);
        return surface.Snapshot();
    }

    [Fact]
    public void UnElementoOltreLaCapacitaSmaltisceIlMenoUsatoDiRecente()
    {
        var a = MakeImage(SKColors.Red);
        var b = MakeImage(SKColors.Green);
        var c = MakeImage(SKColors.Blue);

        var lru = new LruCache<string, SKImage>(2);
        lru.Set("a", a);
        lru.Set("b", b);
        lru.Set("c", c); // "a" e' il meno usato di recente: viene smaltito

        Assert.Null(lru.TryGet("a"));
        Assert.NotNull(lru.TryGet("b"));
        Assert.NotNull(lru.TryGet("c"));
        Assert.Equal(2, lru.Count);
    }

    [Fact]
    public void UnAccessoRecenteProtegeDallESpulsione()
    {
        var lru = new LruCache<string, SKImage>(2);
        var a = MakeImage(SKColors.Red);
        var b = MakeImage(SKColors.Green);
        var c = MakeImage(SKColors.Blue);

        lru.Set("a", a);
        lru.Set("b", b);
        lru.TryGet("a"); // "a" torna il piu' recente, "b" diventa il candidato all'espulsione
        lru.Set("c", c);

        Assert.NotNull(lru.TryGet("a"));
        Assert.Null(lru.TryGet("b"));
    }

    [Fact]
    public void DecodedImageCacheRiusaLaStessaImmaginePerLoStessoSha()
    {
        var cache = new DecodedImageCache(capacity: 4);
        using var image = MakeImage(SKColors.Orange);

        Assert.Null(cache.TryGet("abc"));
        cache.Store("abc", image);

        Assert.Same(image, cache.TryGet("abc"));
    }
}
