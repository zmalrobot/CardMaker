using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

/// <summary>
/// Layer F2: stelle Livello/Rank a passo fisso e frecce Link on/off in posizioni fisse.
/// </summary>
public class SymbolRepeaterAndToggleGroupTests
{
    private static readonly CardRenderer Renderer = new(new TextEngine());

    /// <summary>Risorse finte con un colore distinto per ogni chiave "set/simbolo".</summary>
    private sealed class ColorSymbolResources : IRenderResources, IDisposable
    {
        private readonly List<SKImage> _created = [];
        private readonly Dictionary<string, SKImage> _bySymbol = new(StringComparer.Ordinal);

        public List<string> RequestedSymbols { get; } = [];

        public void Register(string setKey, string symbolKey, SKColor color)
        {
            var info = new SKImageInfo(64, 64);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(color);
            var image = surface.Snapshot();
            _created.Add(image);
            _bySymbol[$"{setKey}/{symbolKey}"] = image;
        }

        public SKImage? GetImage(Guid assetId) => null;

        public SKImage? GetImageByKey(string assetKey) => null;

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey)
        {
            RequestedSymbols.Add($"{symbolSetKey}/{symbolKey}");
            return _bySymbol.GetValueOrDefault($"{symbolSetKey}/{symbolKey}");
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

    private static CardLayout LayoutWith(LayerDefinition layer) => new()
    {
        Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
        Layers = [layer],
    };

    private static CardRenderRequest Request(CardLayout layout, IRenderResources resources, Dictionary<string, CardValue> values, int dpi = 150) => new()
    {
        Layout = layout,
        Values = values,
        Resources = resources,
        Dpi = dpi,
    };

    [Fact]
    public void LeStelleDiLivelloSiRiempionoDaDestra()
    {
        using var resources = new ColorSymbolResources();
        resources.Register("stars", "level", SKColors.Gold);

        var layer = new SymbolRepeaterLayer
        {
            Id = "level",
            Rect = new NormalizedRect(0, 0, 1, 1),
            SymbolSetKey = "stars",
            SymbolKey = "level",
            FieldKey = "level",
            MaxCount = 4,
            Direction = RepeaterDirection.RightToLeft,
        };
        var values = new Dictionary<string, CardValue> { ["level"] = CardValue.FromNumber(2) };

        var result = Renderer.Render(Request(LayoutWith(layer), resources, values));
        using var decoded = SKBitmap.Decode(result.Content);

        var geometry = CardGeometry.YuGiOh(150);
        var cellWidth = geometry.TrimWidthPx / 4f;
        var midY = geometry.TrimHeightPx / 2;

        bool IsFilled(int cellIndex)
        {
            var x = (int)(cellWidth * (cellIndex + 0.5f));
            return decoded.GetPixel(x, midY).Alpha > 0;
        }

        Assert.False(IsFilled(0), "la prima posizione da sinistra deve restare vuota");
        Assert.False(IsFilled(1), "la seconda posizione da sinistra deve restare vuota");
        Assert.True(IsFilled(2), "il livello 2 deve riempire le ultime due posizioni da destra");
        Assert.True(IsFilled(3), "l'ultima posizione a destra deve essere riempita");
    }

    [Fact]
    public void IlConteggioNonSuperaIlMassimoDiPosizioni()
    {
        using var resources = new ColorSymbolResources();
        resources.Register("stars", "rank", SKColors.Purple);

        var layer = new SymbolRepeaterLayer
        {
            Id = "rank",
            Rect = new NormalizedRect(0, 0, 1, 1),
            SymbolSetKey = "stars",
            SymbolKey = "rank",
            Count = 99,
            MaxCount = 4,
            Direction = RepeaterDirection.LeftToRight,
        };

        var result = Renderer.Render(Request(LayoutWith(layer), resources, []));
        using var decoded = SKBitmap.Decode(result.Content);

        var geometry = CardGeometry.YuGiOh(150);
        var cellWidth = geometry.TrimWidthPx / 4f;
        var midY = geometry.TrimHeightPx / 2;
        var lastCellX = (int)(cellWidth * 3.5f);

        Assert.True(decoded.GetPixel(lastCellX, midY).Alpha > 0, "anche l'ultima delle 4 posizioni deve essere riempita");
    }

    [Fact]
    public void UnSimboloMancanteProduceUnAvviso()
    {
        using var resources = new ColorSymbolResources();

        var layer = new SymbolRepeaterLayer
        {
            Id = "rank",
            Rect = new NormalizedRect(0, 0, 1, 1),
            SymbolSetKey = "stars",
            SymbolKey = "rank",
            Count = 1,
        };

        var result = Renderer.Render(Request(LayoutWith(layer), resources, []));

        Assert.Contains(result.Warnings, w => w.Code == "symbol.missing" && w.LayerId == "rank");
    }

    [Fact]
    public void UnConteggioAZeroNonDisegnaNullaNeAvvisa()
    {
        using var resources = new ColorSymbolResources();

        var layer = new SymbolRepeaterLayer
        {
            Id = "rank",
            Rect = new NormalizedRect(0, 0, 1, 1),
            SymbolSetKey = "stars",
            SymbolKey = "rank",
            Count = 0,
        };

        var result = Renderer.Render(Request(LayoutWith(layer), resources, []));

        Assert.Empty(result.Warnings);
        Assert.Empty(resources.RequestedSymbols);
    }

    [Fact]
    public void LeFrecceLinkAccesePrendonoIlSimboloOnELeAltreLoOff()
    {
        using var resources = new ColorSymbolResources();
        resources.Register("link-arrows", "on", SKColors.White);
        resources.Register("link-arrows", "off", SKColors.DimGray);

        var layer = new ToggleGroupLayer
        {
            Id = "arrows",
            Rect = new NormalizedRect(0, 0, 1, 1),
            SymbolSetKey = "link-arrows",
            FieldKey = "linkArrows",
            OnSymbolKey = "on",
            OffSymbolKey = "off",
            Items =
            [
                new ToggleItem { Key = "top", Rect = new NormalizedRect(0.4, 0.0, 0.2, 0.2) },
                new ToggleItem { Key = "bottom", Rect = new NormalizedRect(0.4, 0.8, 0.2, 0.2) },
            ],
        };
        var values = new Dictionary<string, CardValue> { ["linkArrows"] = CardValue.FromList(["top"]) };

        var result = Renderer.Render(Request(LayoutWith(layer), resources, values));
        using var decoded = SKBitmap.Decode(result.Content);

        var geometry = CardGeometry.YuGiOh(150);
        var topPixel = decoded.GetPixel(decoded.Width / 2, (int)(geometry.TrimHeightPx * 0.1));
        var bottomPixel = decoded.GetPixel(decoded.Width / 2, (int)(geometry.TrimHeightPx * 0.9));

        Assert.Equal(255, topPixel.Red);
        Assert.Equal(105, bottomPixel.Red);
    }

    [Fact]
    public void SenzaSimboloDiSpegnimentoLePosizioniInattiveRestanoTrasparenti()
    {
        using var resources = new ColorSymbolResources();
        resources.Register("link-arrows", "on", SKColors.White);

        var layer = new ToggleGroupLayer
        {
            Id = "arrows",
            Rect = new NormalizedRect(0, 0, 1, 1),
            SymbolSetKey = "link-arrows",
            FieldKey = "linkArrows",
            OnSymbolKey = "on",
            Items = [new ToggleItem { Key = "top", Rect = new NormalizedRect(0.4, 0.0, 0.2, 0.2) }],
        };

        var result = Renderer.Render(Request(LayoutWith(layer), resources, []));
        using var decoded = SKBitmap.Decode(result.Content);

        var pixel = decoded.GetPixel(decoded.Width / 2, 5);
        Assert.Equal(0, pixel.Alpha);
    }
}
