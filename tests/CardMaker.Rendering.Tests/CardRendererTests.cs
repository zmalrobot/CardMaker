using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Placeholders;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

public class CardRendererTests
{
    private static readonly CardRenderer Renderer = new(new TextEngine());

    /// <summary>Risorse finte: registra cosa viene richiesto, cosi' i test verificano la fase RESOLVE.</summary>
    private sealed class StubResources : IRenderResources, IDisposable
    {
        private readonly List<SKImage> _created = [];

        public List<string> RequestedSymbols { get; } = [];

        public List<string?> RequestedFonts { get; } = [];

        public Dictionary<string, SKImage> ImagesByKey { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<Guid, SKImage> ImagesById { get; } = [];

        public bool HasSymbols { get; init; }

        public SKImage? GetImage(Guid assetId) => ImagesById.GetValueOrDefault(assetId);

        public SKImage? GetImageByKey(string assetKey) => ImagesByKey.GetValueOrDefault(assetKey);

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey)
        {
            RequestedSymbols.Add($"{symbolSetKey}/{symbolKey}");
            return HasSymbols ? MakeSolid(SKColors.Orange) : null;
        }

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            RequestedFonts.Add(roleAlias);
            isFallback = true;
            return TestFonts.Default;
        }

        public SKImage MakeSolid(SKColor color, int size = 256)
        {
            var info = new SKImageInfo(size, size);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(color);
            var image = surface.Snapshot();
            _created.Add(image);
            return image;
        }

        public void Dispose()
        {
            foreach (var image in _created)
            {
                image.Dispose();
            }
        }
    }

    private static CardRenderRequest BuildRequest(
        CardLayout layout,
        IRenderResources resources,
        int dpi = 150,
        bool includeBleed = false) => new()
        {
            Layout = layout,
            Values = DemoLayouts.SampleValues(),
            Resources = resources,
            Dpi = dpi,
            IncludeBleed = includeBleed,
        };

    [Fact]
    public void ProduceUnPngDelleDimensioniDelTrim()
    {
        using var resources = new StubResources();
        resources.ImagesByKey[DemoLayouts.FrameAssetKey] = resources.MakeSolid(SKColors.Bisque);

        var result = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources));

        var geometry = CardGeometry.YuGiOh(150);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(geometry.TrimWidthPx, result.WidthPx);
        Assert.Equal(geometry.TrimHeightPx, result.HeightPx);
    }

    [Fact]
    public void ConAbbondanzaLImmagineEQuellaDelMasterCanvas()
    {
        using var resources = new StubResources();

        var result = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources, includeBleed: true));

        var geometry = CardGeometry.YuGiOh(150);
        Assert.Equal(geometry.MasterWidthPx, result.WidthPx);
        Assert.Equal(geometry.MasterHeightPx, result.HeightPx);
    }

    [Fact]
    public void GliAngoliVengonoArrotondati()
    {
        using var resources = new StubResources();
        resources.ImagesByKey[DemoLayouts.FrameAssetKey] = resources.MakeSolid(SKColors.Black);

        var result = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources));

        using var decoded = SKBitmap.Decode(result.Content);
        Assert.Equal(0, decoded.GetPixel(0, 0).Alpha);
        Assert.Equal(255, decoded.GetPixel(decoded.Width / 2, decoded.Height / 2).Alpha);
    }

    [Fact]
    public void UnaRisorsaMancanteProduceUnAvvisoNonUnErrore()
    {
        using var resources = new StubResources();

        var result = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources));

        Assert.Contains(result.Warnings, w => w.Code == "asset.missing");
        Assert.Contains(result.Warnings, w => w.Code == "symbol.missing");
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public void UnRuoloFontNonAssegnatoProduceUnAvviso()
    {
        using var resources = new StubResources();

        var result = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources));

        Assert.Contains(result.Warnings, w => w.Code == "font.fallback");
        Assert.Contains("card-name", resources.RequestedFonts);
        Assert.Contains("effect", resources.RequestedFonts);
    }

    [Fact]
    public void LeCondizioniNascondonoILayer()
    {
        using var resources = new StubResources();
        var values = DemoLayouts.SampleValues();
        values["summonMethod"] = CardValue.FromText("Link");

        var layout = DemoLayouts.YuGiOhMonster();
        var withDef = Renderer.Render(BuildRequest(layout, resources));
        var withoutDef = Renderer.Render(new CardRenderRequest
        {
            Layout = layout,
            Values = values,
            Resources = resources,
            Dpi = 150,
        });

        // La DEF sparisce sui Link: il contenuto renderizzato deve differire.
        Assert.NotEqual(withDef.Content, withoutDef.Content);
    }

    [Fact]
    public void ILayerVengonoDisegnatiNellOrdineDelloZIndex()
    {
        using var resources = new StubResources();
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new ShapeLayer { Id = "sopra", Z = 10, Rect = new NormalizedRect(0, 0, 1, 1), FillColor = "#FF0000" },
                new ShapeLayer { Id = "sotto", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), FillColor = "#0000FF" },
            ],
        };

        var result = Renderer.Render(BuildRequest(layout, resources));

        using var decoded = SKBitmap.Decode(result.Content);
        var center = decoded.GetPixel(decoded.Width / 2, decoded.Height / 2);
        Assert.Equal(255, center.Red);
        Assert.Equal(0, center.Blue);
    }

    [Fact]
    public void ArtworkASottorisoluzioneProduceUnAvviso()
    {
        using var resources = new StubResources();
        var artworkId = Guid.CreateVersion7();
        resources.ImagesById[artworkId] = resources.MakeSolid(SKColors.Green, 64);

        var values = DemoLayouts.SampleValues();
        values["artwork"] = CardValue.FromText(artworkId.ToString());

        var result = Renderer.Render(new CardRenderRequest
        {
            Layout = DemoLayouts.YuGiOhMonster(),
            Values = values,
            Resources = resources,
            Dpi = 150,
        });

        Assert.Contains(result.Warnings, w => w.Code == "artwork.lowResolution");
    }

    [Fact]
    public void IlJpegNonHaTrasparenza()
    {
        using var resources = new StubResources();
        resources.ImagesByKey[DemoLayouts.FrameAssetKey] = resources.MakeSolid(SKColors.Bisque);

        var result = Renderer.Render(new CardRenderRequest
        {
            Layout = DemoLayouts.YuGiOhMonster(),
            Values = DemoLayouts.SampleValues(),
            Resources = resources,
            Dpi = 150,
            Format = RenderOutputFormat.Jpeg,
        });

        Assert.Equal("image/jpeg", result.ContentType);
        using var decoded = SKBitmap.Decode(result.Content);
        Assert.Equal(255, decoded.GetPixel(0, 0).Alpha);
    }

    [Fact]
    public void AnteprimaEdExportProduconoLoStessoLayoutAScaleDiverse()
    {
        using var resources = new StubResources();
        resources.ImagesByKey[DemoLayouts.FrameAssetKey] = resources.MakeSolid(SKColors.Bisque);

        var preview = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources, dpi: 150));
        var export = Renderer.Render(BuildRequest(DemoLayouts.YuGiOhMonster(), resources, dpi: 600));

        var ratioWidth = (double)export.WidthPx / preview.WidthPx;
        var ratioHeight = (double)export.HeightPx / preview.HeightPx;

        Assert.Equal(4.0, ratioWidth, 0.02);
        Assert.Equal(4.0, ratioHeight, 0.02);
        Assert.Equal(preview.Warnings.Count, export.Warnings.Count);
    }

    [Fact]
    public void IlLayoutDimostrativoUsaIFrameSegnaposto()
    {
        var placeholderKeys = PlaceholderFrameSpec.YuGiOhSet().Select(s => "placeholder-" + s.Key);

        Assert.Contains(DemoLayouts.FrameAssetKey, placeholderKeys);
    }
}
