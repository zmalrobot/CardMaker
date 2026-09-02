using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

/// <summary>
/// Golden image test con tolleranza pixel (F2). Copre le combinazioni di layer piu' a rischio di
/// regressione silenziosa: gradiente, ripetitore, toggle, overlay mascherato, testo misto.
/// I riferimenti vivono in <c>GoldenImages/</c> e si rigenerano con <c>UPDATE_GOLDEN=1</c>.
/// </summary>
public class GoldenImageTests
{
    private static readonly CardRenderer Renderer = new(new TextEngine());

    private sealed class GoldenResources : IRenderResources
    {
        private readonly Dictionary<string, SKColor> _symbolColors = new(StringComparer.Ordinal);

        public void MapSymbol(string setKey, string key, SKColor color) => _symbolColors[$"{setKey}/{key}"] = color;

        public SKImage? GetImage(Guid assetId) => null;

        public SKImage? GetImageByKey(string assetKey) => null;

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey)
        {
            if (!_symbolColors.TryGetValue($"{symbolSetKey}/{symbolKey}", out var color))
            {
                return null;
            }

            var info = new SKImageInfo(32, 32);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(color);
            return surface.Snapshot();
        }

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            isFallback = true;
            return TestFonts.Default;
        }
    }

    private static CardRenderRequest Request(CardLayout layout, IRenderResources resources, Dictionary<string, CardValue>? values = null) => new()
    {
        Layout = layout,
        Values = values ?? [],
        Resources = resources,
        Dpi = 150,
    };

    [Fact]
    public void GradienteEFormeArrotondate()
    {
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new ShapeLayer
                {
                    Id = "bg", Rect = new NormalizedRect(0, 0, 1, 1),
                    GradientFrom = "#1D5FD8", GradientTo = "#29A9E8", GradientAngleDeg = 45,
                },
                new ShapeLayer
                {
                    Id = "badge", Rect = new NormalizedRect(0.2, 0.2, 0.6, 0.3),
                    Shape = ShapeKind.RoundedRect, CornerRadius = 0.2, FillColor = "#FFFFFF",
                    BorderColor = "#101010", BorderWidthMm = 0.6,
                },
            ],
        };

        var result = Renderer.Render(Request(layout, new GoldenResources()));

        GoldenImageAssert.Matches(result.Content, "gradient-and-shapes");
    }

    [Fact]
    public void StelleLivelloEFrecceLink()
    {
        var resources = new GoldenResources();
        resources.MapSymbol("stars", "level", SKColors.Gold);
        resources.MapSymbol("link-arrows", "on", SKColors.White);
        resources.MapSymbol("link-arrows", "off", SKColors.DimGray);

        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new SymbolRepeaterLayer
                {
                    Id = "level", Rect = new NormalizedRect(0.1, 0.05, 0.8, 0.08),
                    SymbolSetKey = "stars", SymbolKey = "level", FieldKey = "level",
                    MaxCount = 8, Direction = RepeaterDirection.RightToLeft,
                },
                new ToggleGroupLayer
                {
                    Id = "arrows", Rect = new NormalizedRect(0.3, 0.6, 0.4, 0.3),
                    SymbolSetKey = "link-arrows", FieldKey = "linkArrows",
                    OnSymbolKey = "on", OffSymbolKey = "off",
                    Items =
                    [
                        new ToggleItem { Key = "top", Rect = new NormalizedRect(0.4, 0.0, 0.2, 0.2) },
                        new ToggleItem { Key = "left", Rect = new NormalizedRect(0.0, 0.4, 0.2, 0.2) },
                        new ToggleItem { Key = "right", Rect = new NormalizedRect(0.8, 0.4, 0.2, 0.2) },
                        new ToggleItem { Key = "bottom", Rect = new NormalizedRect(0.4, 0.8, 0.2, 0.2) },
                    ],
                },
            ],
        };
        var values = new Dictionary<string, CardValue>
        {
            ["level"] = CardValue.FromNumber(5),
            ["linkArrows"] = CardValue.FromList(["top", "right"]),
        };

        var result = Renderer.Render(Request(layout, resources, values));

        GoldenImageAssert.Matches(result.Content, "stars-and-link-arrows");
    }

    [Fact]
    public void TestoMistoConEtichettaDiSezione()
    {
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            TextStyles = new Dictionary<string, TextStyle>(StringComparer.Ordinal)
            {
                ["body"] = new() { SizePt = 9, Color = "#101010", LineHeight = 1.2 },
            },
            Layers =
            [
                new ShapeLayer { Id = "bg", Rect = new NormalizedRect(0, 0, 1, 1), FillColor = "#FDF6E3" },
                new RichTextLayer
                {
                    Id = "effect", Rect = new NormalizedRect(0.08, 0.1, 0.84, 0.5),
                    Style = "body",
                    Source = "[EFFETTO] Questo testo e' **in grassetto** e *in corsivo* a tratti.\n- Punto uno\n- Punto due",
                },
            ],
        };

        var result = Renderer.Render(Request(layout, new GoldenResources()));

        GoldenImageAssert.Matches(result.Content, "rich-text-with-section-label");
    }
}
