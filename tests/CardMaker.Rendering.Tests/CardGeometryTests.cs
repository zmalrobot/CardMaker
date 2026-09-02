using CardMaker.Contracts.Geometry;

namespace CardMaker.Rendering.Tests;

/// <summary>
/// Le misure sono il contratto con chi produce la grafica: se cambiano, gli asset non combaciano piu'.
/// </summary>
public class CardGeometryTests
{
    [Fact]
    public void YuGiOhA600DpiRispettaLaSpecificaAsset()
    {
        var geometry = CardGeometry.YuGiOh();

        Assert.Equal(1394, geometry.TrimWidthPx);
        Assert.Equal(2031, geometry.TrimHeightPx);
        Assert.Equal(47, geometry.BleedPx);
        Assert.Equal(1488, geometry.MasterWidthPx);
        Assert.Equal(2125, geometry.MasterHeightPx);
        Assert.Equal(47, geometry.CornerRadiusPx);
        Assert.Equal(71, geometry.SafeZonePx);
    }

    [Fact]
    public void IlTrimRestaCentratoNelMasterCanvas()
    {
        var geometry = CardGeometry.YuGiOh();

        Assert.Equal(geometry.MasterWidthPx, geometry.TrimWidthPx + (2 * geometry.BleedPx));
        Assert.Equal(geometry.MasterHeightPx, geometry.TrimHeightPx + (2 * geometry.BleedPx));
    }

    [Fact]
    public void FormatoPokerA600Dpi()
    {
        var geometry = CardGeometry.PokerSize();

        Assert.Equal(1488, geometry.TrimWidthPx);
        Assert.Equal(2079, geometry.TrimHeightPx);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(150)]
    [InlineData(300)]
    [InlineData(600)]
    public void LeCoordinateNormalizzateScalanoConIlDpi(int dpi)
    {
        var geometry = CardGeometry.YuGiOh(dpi);
        var rect = new NormalizedRect(0.25, 0.5, 0.5, 0.25);

        var (x, y, width, height) = geometry.ToMasterPixels(rect);

        Assert.Equal(geometry.BleedPx + (geometry.TrimWidthPx * 0.25f), x, 0.01);
        Assert.Equal(geometry.BleedPx + (geometry.TrimHeightPx * 0.5f), y, 0.01);
        Assert.Equal(geometry.TrimWidthPx * 0.5f, width, 0.01);
        Assert.Equal(geometry.TrimHeightPx * 0.25f, height, 0.01);
    }
}
