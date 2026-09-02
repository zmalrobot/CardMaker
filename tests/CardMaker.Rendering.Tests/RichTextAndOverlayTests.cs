using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

public class RichTextParserTests
{
    [Fact]
    public void IlGrassettoEIlCorsivoVengonoRiconosciuti()
    {
        var paragraphs = RichTextParser.Parse("Testo **grassetto** e *corsivo* normale", null);

        var runs = Assert.Single(paragraphs).Runs;
        Assert.Equal(RichRunKind.Text, runs[0].Kind);
        Assert.Contains(runs, r => r.Kind == RichRunKind.Bold && r.Text == "grassetto");
        Assert.Contains(runs, r => r.Kind == RichRunKind.Italic && r.Text == "corsivo");
    }

    [Fact]
    public void UnaSezioneEtichettataVieneSeparataDalTesto()
    {
        var paragraphs = RichTextParser.Parse("[EFFETTO] Pesca una carta.", null);

        var runs = Assert.Single(paragraphs).Runs;
        Assert.Equal(RichRunKind.SectionLabel, runs[0].Kind);
        Assert.Equal("[EFFETTO]", runs[0].Text);
        Assert.Contains(runs, r => r.Kind == RichRunKind.Text && r.Text.Contains("Pesca", StringComparison.Ordinal));
    }

    [Fact]
    public void UnSimboloConSetEsplicitoVieneRiconosciuto()
    {
        var paragraphs = RichTextParser.Parse("Costo {sym:mana.blue} da pagare", null);

        var runs = Assert.Single(paragraphs).Runs;
        var symbol = Assert.Single(runs, r => r.Kind == RichRunKind.Symbol);
        Assert.Equal("mana", symbol.SymbolSetKey);
        Assert.Equal("blue", symbol.SymbolKey);
    }

    [Fact]
    public void UnSimboloSenzaSetUsaIlSetDiDefault()
    {
        var paragraphs = RichTextParser.Parse("{sym:blue}", "mana");

        var symbol = Assert.Single(Assert.Single(paragraphs).Runs);
        Assert.Equal("mana", symbol.SymbolSetKey);
        Assert.Equal("blue", symbol.SymbolKey);
    }

    [Fact]
    public void UnPuntoElencoVieneRiconosciuto()
    {
        var paragraphs = RichTextParser.Parse("- primo punto\nsecondo, senza punto", null);

        Assert.True(paragraphs[0].IsBullet);
        Assert.False(paragraphs[1].IsBullet);
    }

    [Fact]
    public void OgniRigaProduceUnParagrafo()
    {
        var paragraphs = RichTextParser.Parse("riga uno\nriga due\nriga tre", null);

        Assert.Equal(3, paragraphs.Count);
    }
}

/// <summary>Layer F2: testo con formattazione mista (richText) e overlay con maschera/blend.</summary>
public class RichTextAndOverlayRenderingTests
{
    private static readonly CardRenderer Renderer = new(new Text.TextEngine());

    private sealed class FakeResources : IRenderResources, IDisposable
    {
        private readonly List<SKImage> _created = [];
        private readonly Dictionary<string, SKImage> _bySymbol = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SKImage> _byKey = new(StringComparer.Ordinal);

        public List<string> MissingSymbolRequests { get; } = [];

        public SKImage MakeSolid(SKColor color, int size = 64)
        {
            var info = new SKImageInfo(size, size);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(color);
            var image = surface.Snapshot();
            _created.Add(image);
            return image;
        }

        public void RegisterSymbol(string setKey, string symbolKey, SKColor color) =>
            _bySymbol[$"{setKey}/{symbolKey}"] = MakeSolid(color);

        public void RegisterKey(string key, SKColor color) => _byKey[key] = MakeSolid(color);

        public SKImage? GetImage(Guid assetId) => null;

        public SKImage? GetImageByKey(string assetKey) => _byKey.GetValueOrDefault(assetKey);

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey)
        {
            var image = _bySymbol.GetValueOrDefault($"{symbolSetKey}/{symbolKey}");
            if (image is null)
            {
                MissingSymbolRequests.Add($"{symbolSetKey}/{symbolKey}");
            }

            return image;
        }

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            isFallback = true;
            return TestFonts.Default;
        }

        public void Dispose()
        {
            foreach (var image in _created)
            {
                image.Dispose();
            }
        }
    }

    private static CardLayout LayoutWith(LayerDefinition layer, Dictionary<string, TextStyle>? styles = null) => new()
    {
        Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
        TextStyles = styles ?? new Dictionary<string, TextStyle>(StringComparer.Ordinal),
        Layers = [layer],
    };

    private static CardRenderRequest Request(CardLayout layout, IRenderResources resources, Dictionary<string, CardValue>? values = null, int dpi = 150) => new()
    {
        Layout = layout,
        Values = values ?? [],
        Resources = resources,
        Dpi = dpi,
    };

    [Fact]
    public void UnaCondizioneNascondeUnRichTextDentroUnGruppo()
    {
        using var resources = new FakeResources();
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new GroupLayer
                {
                    Id = "group",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    Children =
                    [
                        new RichTextLayer
                        {
                            Id = "effect", Rect = new NormalizedRect(0.05, 0.05, 0.9, 0.3), Source = "Testo effetto",
                            VisibleWhen = Condition.Equal("kind", "monster"),
                        },
                    ],
                },
            ],
        };

        var withoutValue = Renderer.Render(Request(layout, resources));
        var withValue = Renderer.Render(Request(layout, resources, new Dictionary<string, CardValue> { ["kind"] = CardValue.FromText("monster") }));

        Assert.NotEqual(withoutValue.Content, withValue.Content);
    }

    [Fact]
    public void IlRichTextRisolveIBindingEDisegnaLeParole()
    {
        using var resources = new FakeResources();
        var styles = new Dictionary<string, TextStyle>(StringComparer.Ordinal)
        {
            ["effect"] = new() { SizePt = 10, Color = "#101010" },
        };
        var layer = new RichTextLayer
        {
            Id = "effect",
            Rect = new NormalizedRect(0.05, 0.05, 0.9, 0.3),
            Source = "[EFFETTO] {{text}}",
            Style = "effect",
        };
        var values = new Dictionary<string, CardValue> { ["text"] = CardValue.FromText("Pesca una carta.") };

        var result = Renderer.Render(Request(LayoutWith(layer, styles), resources, values));

        Assert.DoesNotContain(result.Warnings, w => w.Code == "text.overflow");
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public void UnSimboloMancanteNelRichTextProduceUnAvviso()
    {
        using var resources = new FakeResources();
        var layer = new RichTextLayer
        {
            Id = "effect",
            Rect = new NormalizedRect(0.05, 0.05, 0.9, 0.3),
            Source = "Costo {sym:mana.blue}",
        };

        var result = Renderer.Render(Request(LayoutWith(layer), resources));

        Assert.Contains(result.Warnings, w => w.Code == "symbol.missing" && w.LayerId == "effect");
        Assert.Contains("mana/blue", resources.MissingSymbolRequests);
    }

    [Fact]
    public void UnTestoTroppoLungoProduceUnAvvisoDiOverflow()
    {
        using var resources = new FakeResources();
        var layer = new RichTextLayer
        {
            Id = "effect",
            Rect = new NormalizedRect(0.05, 0.05, 0.9, 0.05),
            Source = string.Join(" ", Enumerable.Repeat("parola", 200)),
        };

        var result = Renderer.Render(Request(LayoutWith(layer), resources));

        Assert.Contains(result.Warnings, w => w.Code == "text.overflow" && w.LayerId == "effect");
    }

    [Fact]
    public void UnOverlaySenzaMascheraSiSovrapponeAllInteraArea()
    {
        using var resources = new FakeResources();
        resources.RegisterKey("frame", SKColors.Blue);
        resources.RegisterKey("foil", SKColors.White);

        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new StaticImageLayer { Id = "frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = "frame", Fit = ImageFit.Stretch },
                new OverlayLayer { Id = "foil", Z = 2, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = "foil", Fit = ImageFit.Stretch, Opacity = 1.0 },
            ],
        };

        var result = Renderer.Render(Request(layout, resources));
        using var decoded = SKBitmap.Decode(result.Content);

        var pixel = decoded.GetPixel(decoded.Width / 2, decoded.Height / 2);
        Assert.Equal(255, pixel.Red);
        Assert.Equal(255, pixel.Green);
        Assert.Equal(255, pixel.Blue);
    }

    [Fact]
    public void UnaMascheraLimitaLOverlayAllaSuaArea()
    {
        using var resources = new FakeResources();
        resources.RegisterKey("frame", SKColors.Blue);
        resources.RegisterKey("foil", SKColors.White);
        resources.RegisterKey("mask-left", SKColors.Black); // sara' sostituita da un half-mask reale sotto

        var geometry = CardGeometry.YuGiOh(150);
        var maskInfo = new SKImageInfo(geometry.TrimWidthPx, geometry.TrimHeightPx);
        using var maskSurface = SKSurface.Create(maskInfo);
        maskSurface.Canvas.Clear(SKColors.Transparent);
        using (var leftPaint = new SKPaint { Color = SKColors.White })
        {
            maskSurface.Canvas.DrawRect(0, 0, maskInfo.Width / 2f, maskInfo.Height, leftPaint);
        }

        using var halfMask = maskSurface.Snapshot();

        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new StaticImageLayer { Id = "frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = "frame", Fit = ImageFit.Stretch },
                new OverlayLayer
                {
                    Id = "foil", Z = 2, Rect = new NormalizedRect(0, 0, 1, 1),
                    AssetKey = "foil", MaskAssetId = Guid.Empty, Fit = ImageFit.Stretch,
                },
            ],
        };

        var resourcesWithMask = new MaskedResources(resources, Guid.Empty, halfMask);
        var result = Renderer.Render(Request(layout, resourcesWithMask));
        using var decoded = SKBitmap.Decode(result.Content);

        var left = decoded.GetPixel(decoded.Width / 4, decoded.Height / 2);
        var right = decoded.GetPixel(decoded.Width * 3 / 4, decoded.Height / 2);

        Assert.Equal(255, left.Red); // coperto dalla maschera: overlay bianco visibile
        Assert.Equal(0, right.Red);  // fuori dalla maschera: resta il frame blu (Red=0)
    }

    private sealed class MaskedResources(FakeResources inner, Guid maskId, SKImage mask) : IRenderResources
    {
        public SKImage? GetImage(Guid assetId) => assetId == maskId ? mask : null;

        public SKImage? GetImageByKey(string assetKey) => inner.GetImageByKey(assetKey);

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey) => inner.GetSymbol(symbolSetKey, symbolKey);

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback) => inner.ResolveFont(roleAlias, out isFallback);
    }

    [Fact]
    public void ImageSlotConCropAFettaMostraSoloLaFettaScelta()
    {
        using var resources = new FakeResources();

        // Immagine sorgente: meta' sinistra rossa, meta' destra verde.
        var info = new SKImageInfo(200, 100);
        using var surface = SKSurface.Create(info);
        using (var red = new SKPaint { Color = SKColors.Red })
        {
            surface.Canvas.DrawRect(0, 0, 100, 100, red);
        }

        using (var green = new SKPaint { Color = SKColors.Green })
        {
            surface.Canvas.DrawRect(100, 0, 100, 100, green);
        }

        using var artwork = surface.Snapshot();
        var artworkId = Guid.CreateVersion7();

        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new ImageSlotLayer
                {
                    Id = "art", Rect = new NormalizedRect(0, 0, 1, 1), FieldKey = "artwork",
                    Fit = ImageFit.Stretch, SliceCount = 2, SliceIndex = 1, SliceAxis = SliceAxis.Horizontal,
                },
            ],
        };
        var values = new Dictionary<string, CardValue> { ["artwork"] = CardValue.FromText(artworkId.ToString()) };

        var result = Renderer.Render(new CardRenderRequest
        {
            Layout = layout,
            Values = values,
            Resources = new ArtworkResources(artworkId, artwork),
            Dpi = 150,
        });

        using var decoded = SKBitmap.Decode(result.Content);
        var pixel = decoded.GetPixel(decoded.Width / 2, decoded.Height / 2);

        Assert.Equal(0, pixel.Red);
        Assert.True(pixel.Green > 0);
    }

    private sealed class ArtworkResources(Guid artworkId, SKImage artwork) : IRenderResources
    {
        public SKImage? GetImage(Guid assetId) => assetId == artworkId ? artwork : null;

        public SKImage? GetImageByKey(string assetKey) => null;

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey) => null;

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            isFallback = true;
            return TestFonts.Default;
        }
    }
}
