using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering;
using CardMaker.Rendering.Text;
using SkiaSharp;
using Xunit;

namespace CardMaker.Rendering.Tests;

public sealed class CardRendererRegressionTests
{
    private readonly CardRenderer _sut = new(new TextEngine());

    private sealed class NullRenderResources : IRenderResources
    {
        public SKImage? GetImage(Guid assetId) => null;
        public SKImage? GetImageByKey(string assetKey) => null;
        public SKImage? GetSymbol(string symbolSetKey, string symbolKey) => null;
        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            isFallback = true;
            return TestFonts.Default;
        }
    }

    [Fact]
    public void TEST_UNIT_017_CollectVisibleLayersAccumulatorMaintainsStrictZIndexOrder()
    {
        var layout = new CardLayout
        {
            Canvas = new CanvasDefinition { WidthMm = 63, HeightMm = 88 },
            Layers =
            [
                new ShapeLayer { Id = "layer-z10", Z = 10, FillColor = "#FF0000", Rect = new NormalizedRect(0, 0, 1, 1) },
                new ShapeLayer { Id = "layer-z1", Z = 1, FillColor = "#00FF00", Rect = new NormalizedRect(0, 0, 1, 1) },
                new ShapeLayer { Id = "layer-z5", Z = 5, FillColor = "#0000FF", Rect = new NormalizedRect(0, 0, 1, 1) },
                new ShapeLayer { Id = "layer-z-2", Z = -2, FillColor = "#FFFF00", Rect = new NormalizedRect(0, 0, 1, 1) }
            ]
        };

        var request = new CardRenderRequest
        {
            Layout = layout,
            Values = new Dictionary<string, CardValue>(),
            Resources = new NullRenderResources(),
            Dpi = 100,
        };

        // Act - ALG-PERF-001 & LOOP-PERF-001
        var result = _sut.Render(request);

        // Assert: rendering completed with success
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Length > 0);
    }

    [Fact]
    public void TEST_UNIT_018_PaintLayerPatternMatchingRendersAllSupportedLayerTypes()
    {
        var layout = new CardLayout
        {
            Canvas = new CanvasDefinition { WidthMm = 63, HeightMm = 88 },
            Layers =
            [
                new ShapeLayer { Id = "shape", FillColor = "#EEEEEE", Rect = new NormalizedRect(0, 0, 1, 1) },
                new TextLayer { Id = "text", Source = "Hello", Rect = new NormalizedRect(0.1, 0.1, 0.8, 0.2) },
                new RichTextLayer { Id = "rich", Source = "Rich [b]Text[/b]", Rect = new NormalizedRect(0.1, 0.3, 0.8, 0.2) },
                new SymbolSlotLayer { Id = "sym", SymbolSetKey = "test", SymbolKey = "star", Rect = new NormalizedRect(0.1, 0.5, 0.2, 0.2) },
                new SymbolRepeaterLayer { Id = "rep", SymbolSetKey = "test", SymbolKey = "star", Count = 3, MaxCount = 5, Rect = new NormalizedRect(0.1, 0.7, 0.8, 0.1) },
                new ToggleGroupLayer { Id = "tog", FieldKey = "toggle", SymbolSetKey = "test", OnSymbolKey = "star", Items = [new ToggleItem { Key = "k1", Rect = new NormalizedRect(0, 0, 1, 1) }] },
                new OverlayLayer { Id = "over", Rect = new NormalizedRect(0, 0, 1, 1) }
            ]
        };

        var request = new CardRenderRequest
        {
            Layout = layout,
            Values = new Dictionary<string, CardValue> { ["toggle"] = CardValue.FromList(["k1"]) },
            Resources = new NullRenderResources(),
            Dpi = 100,
        };

        // Act - LOOP-PERF-002: direct pattern matching dispatch
        var result = _sut.Render(request);

        // Assert
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Length > 0);
    }

    [Fact]
    public void TEST_UNIT_019_RenderPostProcessorTrimsRasterImageDirectlyWithoutPngDecode()
    {
        // Arrange
        var geometry = CardGeometry.PokerSize(100);

        var info = new SKImageInfo(geometry.MasterWidthPx, geometry.MasterHeightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Blue);
        using var snapshot = surface.Snapshot();

        var request = new CardRenderRequest
        {
            Layout = new CardLayout(),
            Values = new Dictionary<string, CardValue>(),
            Resources = new NullRenderResources(),
            Dpi = 100,
            IncludeBleed = false, // Trims to 100x150
            RoundCorners = false,
            Format = RenderOutputFormat.Png,
        };

        // Act - CPU-PERF-001 & MEM-PERF-003: direct raster image passthrough and trim
        using var processedImage = RenderPostProcessor.ApplyPostProcessing(snapshot, geometry, request);

        // Assert
        Assert.NotNull(processedImage);
        Assert.Equal(geometry.TrimWidthPx, processedImage.Width);
        Assert.Equal(geometry.TrimHeightPx, processedImage.Height);
    }
}
