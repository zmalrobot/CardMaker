using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

/// <summary>
/// Un testo che non entra deve comunque essere disegnato: l'admin deve vedere il problema,
/// non ritrovarsi la carta muta.
/// </summary>
public class TextOverflowRenderingTests
{
    private sealed class WhiteResources : IRenderResources
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

    private static int CountDarkPixels(byte[] png, NormalizedRect region, CardGeometry geometry)
    {
        using var bitmap = SKBitmap.Decode(png);
        var left = (int)(region.X * geometry.TrimWidthPx);
        var top = (int)(region.Y * geometry.TrimHeightPx);
        var right = (int)(region.Right * geometry.TrimWidthPx);
        var bottom = (int)(region.Bottom * geometry.TrimHeightPx);

        var dark = 0;
        for (var y = Math.Max(0, top); y < Math.Min(bitmap.Height, bottom); y++)
        {
            for (var x = Math.Max(0, left); x < Math.Min(bitmap.Width, right); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > 128 && pixel.Red < 120 && pixel.Green < 120 && pixel.Blue < 120)
                {
                    dark++;
                }
            }
        }

        return dark;
    }

    private static (byte[] Png, IReadOnlyList<RenderWarning> Warnings) RenderName(string name)
    {
        var values = DemoLayouts.SampleValues();
        values["name"] = CardValue.FromText(name);

        var layout = DemoLayouts.YuGiOhMonster();
        var background = new CardLayout
        {
            Canvas = layout.Canvas,
            TextStyles = layout.TextStyles,
            Computed = layout.Computed,
            // Fondo bianco opaco per poter contare i pixel scuri del testo.
            Layers = [new ShapeLayer { Id = "bg", Z = -100, Rect = new NormalizedRect(0, 0, 1, 1), FillColor = "#FFFFFF" }, .. layout.Layers],
        };

        var result = new CardRenderer(new TextEngine()).Render(new CardRenderRequest
        {
            Layout = background,
            Values = values,
            Resources = new WhiteResources(),
            Dpi = 300,
        });

        return (result.Content, result.Warnings);
    }

    private static readonly NormalizedRect NameBox = new(0.070, 0.038, 0.720, 0.058);

    [Fact]
    public void UnNomeCheEntraVieneDisegnato()
    {
        var (png, warnings) = RenderName("Drago Bianco");

        Assert.DoesNotContain(warnings, w => w.Code == "text.overflow" && w.LayerId == "name");
        Assert.True(CountDarkPixels(png, NameBox, CardGeometry.YuGiOh(300)) > 100);
    }

    [Fact]
    public void UnNomeCheNonEntraVieneComunqueDisegnato()
    {
        var (png, warnings) = RenderName("Drago Bianco Occhi Blu Leggendario Supremo Definitivo Assoluto");

        Assert.Contains(warnings, w => w.Code == "text.overflow" && w.LayerId == "name");
        Assert.True(
            CountDarkPixels(png, NameBox, CardGeometry.YuGiOh(300)) > 100,
            "il testo in overflow deve restare visibile, altrimenti la carta sembra vuota");
    }
}
