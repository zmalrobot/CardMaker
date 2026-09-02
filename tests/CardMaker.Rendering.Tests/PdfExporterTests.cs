using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

public class PdfExporterTests
{
    private static readonly CardRenderer Renderer = new(new TextEngine());
    private static readonly PdfExporter Exporter = new();

    private sealed class StubResources : IRenderResources
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

    private static CardLayout SimpleLayout(string fillColor) => new()
    {
        Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
        Layers = [new ShapeLayer { Id = "bg", Rect = new NormalizedRect(0, 0, 1, 1), FillColor = fillColor }],
    };

    [Fact]
    public void UnSoloFronteProduceUnPdfDiUnaPagina()
    {
        var front = Renderer.Render(new CardRenderRequest
        {
            Layout = SimpleLayout("#FF0000"),
            Values = new Dictionary<string, CardValue>(),
            Resources = new StubResources(),
            Dpi = 150,
        });

        var pdf = Exporter.Export(front);

        Assert.True(pdf.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void FronteERetroProduconoUnPdfDiDuePagine()
    {
        var front = Renderer.Render(new CardRenderRequest
        {
            Layout = SimpleLayout("#FF0000"),
            Values = new Dictionary<string, CardValue>(),
            Resources = new StubResources(),
            Dpi = 150,
        });
        var back = Renderer.Render(new CardRenderRequest
        {
            Layout = SimpleLayout("#0000FF"),
            Values = new Dictionary<string, CardValue>(),
            Resources = new StubResources(),
            Dpi = 150,
        });

        var pdfFrontOnly = Exporter.Export(front);
        var pdfBoth = Exporter.Export(front, back);

        Assert.True(pdfBoth.Length > pdfFrontOnly.Length);
    }
}
