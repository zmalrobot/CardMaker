using CardMaker.Contracts.Geometry;
using CardMaker.Rendering.Placeholders;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

public class PlaceholderFrameGeneratorTests
{
    private static readonly PlaceholderFrameGenerator Generator = new();

    [Fact]
    public void ProduceUnPngDelleDimensioniDelMasterCanvas()
    {
        var geometry = CardGeometry.YuGiOh();
        var spec = PlaceholderFrameSpec.YuGiOhSet().First(s => s.Key == "monster-effect");

        var png = Generator.Generate(spec, geometry);

        using var decoded = SKBitmap.Decode(png);
        Assert.Equal(geometry.MasterWidthPx, decoded.Width);
        Assert.Equal(geometry.MasterHeightPx, decoded.Height);
    }

    [Fact]
    public void LaFinestraDellArtworkEComplementamenteTrasparente()
    {
        var geometry = CardGeometry.YuGiOh();
        var spec = PlaceholderFrameSpec.YuGiOhSet().First(s => s.Key == "monster-effect");
        var regions = PlaceholderFrameGenerator.GetRegions(spec.Layout);

        var png = Generator.Generate(spec, geometry);

        using var decoded = SKBitmap.Decode(png);
        var (x, y, width, height) = geometry.ToMasterPixels(regions.ArtWindow);

        // L'artwork viene disegnato SOTTO il frame: se la finestra non fosse trasparente non si vedrebbe.
        var center = decoded.GetPixel((int)(x + width / 2), (int)(y + height / 2));
        Assert.Equal(0, center.Alpha);
    }

    [Fact]
    public void IlBordoEsternoEOpacoFinoAlLimiteDellAbbondanza()
    {
        var geometry = CardGeometry.YuGiOh();
        var spec = PlaceholderFrameSpec.YuGiOhSet().First(s => s.Key == "spell");

        var png = Generator.Generate(spec, geometry);

        using var decoded = SKBitmap.Decode(png);
        Assert.Equal(255, decoded.GetPixel(0, 0).Alpha);
        Assert.Equal(255, decoded.GetPixel(decoded.Width - 1, decoded.Height - 1).Alpha);
    }

    [Fact]
    public void TuttiISegnapostoDelSetVengonoGenerati()
    {
        var geometry = CardGeometry.YuGiOh(150);

        foreach (var spec in PlaceholderFrameSpec.YuGiOhSet())
        {
            var png = Generator.Generate(spec, geometry);
            Assert.NotEmpty(png);
        }
    }

    [Fact]
    public void LeRegioniRestanoDentroLaCarta()
    {
        foreach (var layout in Enum.GetValues<PlaceholderLayout>())
        {
            if (layout == PlaceholderLayout.Back)
            {
                continue;
            }

            var regions = PlaceholderFrameGenerator.GetRegions(layout);
            foreach (var rect in new[]
                     {
                         regions.ArtWindow, regions.NameBox, regions.AttributeBox,
                         regions.LevelStrip, regions.TypeLineBox, regions.EffectBox, regions.AtkBox,
                     })
            {
                Assert.InRange(rect.X, 0d, 1d);
                Assert.InRange(rect.Y, 0d, 1d);
                Assert.InRange(rect.Right, 0d, 1d);
                Assert.InRange(rect.Bottom, 0d, 1d);
            }
        }
    }
}
