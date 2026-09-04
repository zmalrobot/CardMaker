using CardMaker.Contracts.Layout;
using CardMaker.Infrastructure.Rendering;
using SkiaSharp;
using Xunit;

namespace CardMaker.Application.Tests.Rendering;

public sealed class PreloadedRenderResourcesTests
{
    private static byte[] Create1x1Png()
    {
        using var bitmap = new SKBitmap(1, 1);
        bitmap.SetPixel(0, 0, SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void TEST_UNIT_005_SymbolResourceKeyResolvesCaseInsensitiveWithoutStringConcat()
    {
        using var sut = new PreloadedRenderResources();
        var png = Create1x1Png();

        // Register symbol with mixed case
        sut.AddSymbol("AttributeSet", "FireSymbol", png);

        // Act & Assert - STR-PERF-002: case-insensitive struct lookup
        var exact = sut.GetSymbol("AttributeSet", "FireSymbol");
        var lower = sut.GetSymbol("attributeset", "firesymbol");
        var upper = sut.GetSymbol("ATTRIBUTESET", "FIRESYMBOL");

        Assert.NotNull(exact);
        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.Same(exact, lower);
        Assert.Same(exact, upper);
    }

    [Fact]
    public void TEST_UNIT_006_TypefaceCacheReusesSameSKTypefaceInstance()
    {
        using var sut1 = new PreloadedRenderResources();
        using var sut2 = new PreloadedRenderResources();

        // Get embedded font bytes via FontService
        var fontBytes = CardMaker.Infrastructure.Storage.FontService.GetEmbeddedFontBytes("card-name");
        Assert.NotNull(fontBytes);

        // Act - MEM-PERF-004: TypefaceCache reuses native instance
        sut1.AddFont("title-font", fontBytes);
        sut2.AddFont("title-font", fontBytes);

        var typeface1 = sut1.ResolveFont("title-font", out var isFallback1);
        var typeface2 = sut2.ResolveFont("title-font", out var isFallback2);

        // Assert
        Assert.False(isFallback1);
        Assert.False(isFallback2);
        Assert.NotNull(typeface1);
        Assert.NotNull(typeface2);
        Assert.Same(typeface1, typeface2);
    }

    [Fact]
    public void TEST_UNIT_007_LayoutReferencesCollectDeduplicatesSymbolsAndKeys()
    {
        var layout1 = new CardLayout
        {
            Layers =
            [
                new SymbolSlotLayer { Id = "s1", SymbolSetKey = "icons", SymbolKey = "STAR" },
                new SymbolSlotLayer { Id = "s2", SymbolSetKey = "icons", SymbolKey = "STAR" },
                new StaticImageLayer { Id = "i1", AssetKey = "frame-art" },
                new StaticImageLayer { Id = "i2", AssetKey = "frame-art" },
            ]
        };
        var layout2 = new CardLayout
        {
            Layers =
            [
                new SymbolSlotLayer { Id = "s3", SymbolSetKey = "icons", SymbolKey = "STAR" },
                new StaticImageLayer { Id = "i3", AssetKey = "frame-art" },
            ]
        };

        // Act - ALG-PERF-002: deduplication via HashSet
        var (assetIds, assetKeys, fontAliases, symbols) = LayoutReferences.Collect([layout1, layout2], new Dictionary<string, CardValue>());

        // Assert
        Assert.Single(symbols);
        Assert.Contains(("icons", "STAR"), symbols);
        Assert.Single(assetKeys);
        Assert.Contains("frame-art", assetKeys);
    }

    [Fact]
    public void TEST_UNIT_008_DisposingPreloadedRenderResourcesPreservesUnownedSharedCache()
    {
        var png = Create1x1Png();
        using var sharedData = SKData.CreateCopy(png);
        var sharedImage = SKImage.FromEncodedData(sharedData);

        var sut = new PreloadedRenderResources();
        sut.AddImage(Guid.NewGuid(), sharedImage, owned: false); // borrowed from shared cache

        // Act
        sut.Dispose();

        // Assert: unowned image was NOT disposed by PreloadedRenderResources
        Assert.NotNull(sharedImage);
        Assert.True(sharedImage.Width > 0);
        sharedImage.Dispose();
    }
}
