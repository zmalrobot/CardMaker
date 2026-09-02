using CardMaker.Rendering.Fonts;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

public class FontRegistryTests
{
    private sealed class StubSource(Dictionary<string, byte[]> fonts) : IFontSource
    {
        public List<string> Requested { get; } = [];

        public byte[]? GetFontBytes(string roleAlias)
        {
            Requested.Add(roleAlias);
            return fonts.GetValueOrDefault(roleAlias);
        }
    }

    [Fact]
    public void UnAliasMancanteRicadeSulFontDiRipiegoSenzaFallire()
    {
        var source = new StubSource([]);
        using var registry = new FontRegistry(source);

        var resolved = registry.Resolve("card-name");

        Assert.True(resolved.IsFallback);
        Assert.NotNull(resolved.Typeface);
        Assert.Equal("card-name", resolved.Alias);
    }

    [Fact]
    public void UnAliasVuotoRicadeSulFontDiRipiego()
    {
        using var registry = new FontRegistry(new StubSource([]));

        Assert.True(registry.Resolve(null).IsFallback);
        Assert.True(registry.Resolve("   ").IsFallback);
    }

    [Fact]
    public void IlFontVieneCaricatoUnaVoltaSolaPerAlias()
    {
        var source = new StubSource([]);
        using var registry = new FontRegistry(source);

        registry.Resolve("effect");
        registry.Resolve("effect");
        registry.Resolve("effect");

        Assert.Single(source.Requested);
    }

    [Fact]
    public void ByteNonValidiNonProduconoUnTypeface()
    {
        Assert.Null(FontRegistry.FromBytes([1, 2, 3, 4]));
        Assert.Null(FontRegistry.FromBytes([]));
        Assert.Null(FontRegistry.FromBytes(null));
    }

    [Fact]
    public void LAnteprimaDelFontProduceUnPng()
    {
        var renderer = new FontPreviewRenderer();

        var png = renderer.Render(TestFonts.Default);

        using var decoded = SKBitmap.Decode(png);
        Assert.Equal(900, decoded.Width);
        Assert.True(decoded.Height > 0);
    }
}
