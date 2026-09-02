using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

public class TextEngineTests
{
    private const int Dpi = 600;
    private static readonly TextEngine Engine = new();

    private static TextStyle Style(AutoFitMode mode, double sizePt = 12, double minSizePt = 4, double minScaleX = 0.5) => new()
    {
        SizePt = sizePt,
        MaxLines = 32,
        AutoFit = new AutoFitSettings { Mode = mode, MinSizePt = minSizePt, MinScaleX = minScaleX },
    };

    [Fact]
    public void UnTestoCheGiaEntraNonVieneToccato()
    {
        var style = Style(AutoFitMode.ShrinkAndCondense);

        var fitted = Engine.Fit("ok", TestFonts.Default, style, 2000, 500, Dpi);

        Assert.Equal(TextEngine.PointsToPixels(style.SizePt, Dpi), fitted.SizePx, 0.5);
        Assert.Equal(1f, fitted.ScaleX, 0.01);
        Assert.False(fitted.Overflowed);
    }

    [Fact]
    public void ConCondenseIlCorpoRestaEIlTestoSiComprime()
    {
        var style = Style(AutoFitMode.Condense) with { MaxLines = 1 };
        var basePx = TextEngine.PointsToPixels(style.SizePt, Dpi);

        var fitted = Engine.Fit("Drago Bianco Occhi Blu Leggendario", TestFonts.Default, style, 900, 400, Dpi);

        Assert.Equal(basePx, fitted.SizePx, 0.5);
        Assert.True(fitted.ScaleX < 1f, "il testo avrebbe dovuto comprimersi");
        Assert.True(fitted.ScaleX >= 0.5f);
        Assert.Single(fitted.Lines);
    }

    [Fact]
    public void ConShrinkIlCorpoSiRiduceELaLarghezzaRestaNaturale()
    {
        var style = Style(AutoFitMode.Shrink) with { MaxLines = 1 };
        var basePx = TextEngine.PointsToPixels(style.SizePt, Dpi);

        var fitted = Engine.Fit("Drago Bianco Occhi Blu Leggendario", TestFonts.Default, style, 900, 400, Dpi);

        Assert.True(fitted.SizePx < basePx, "il corpo avrebbe dovuto ridursi");
        Assert.Equal(1f, fitted.ScaleX, 0.01);
    }

    [Fact]
    public void NonVieneApplicataCompressioneSuperflua()
    {
        const string Text = "Nome di carta piuttosto lungo";
        const float MaxWidth = 1200f;
        var style = Style(AutoFitMode.ShrinkAndCondense) with { MaxLines = 1 };

        var fitted = Engine.Fit(Text, TestFonts.Default, style, MaxWidth, 400, Dpi);

        Assert.False(fitted.Overflowed);
        Assert.Single(fitted.Lines);
        Assert.True(fitted.Lines[0].WidthPx <= MaxWidth + 0.5f);

        // Allentando di poco la compressione il testo non deve piu' entrare:
        // significa che il motore ha usato la scala piu' grande possibile.
        using var looser = new SKFont(TestFonts.Default, fitted.SizePx) { ScaleX = fitted.ScaleX + 0.05f };
        Assert.True(looser.MeasureText(Text) > MaxWidth);
    }

    [Fact]
    public void InOverflowIlNumeroMassimoDiRigheVieneRispettato()
    {
        var style = Style(AutoFitMode.None, sizePt: 12) with { MaxLines = 1 };

        var fitted = Engine.Fit(new string('W', 200), TestFonts.Default, style, 300, 100, Dpi);

        Assert.True(fitted.Overflowed);
        Assert.Single(fitted.Lines);
    }

    [Fact]
    public void IlTestoLungoVieneMandatoACapo()
    {
        var style = Style(AutoFitMode.None, sizePt: 8);

        var fitted = Engine.Fit(
            "Se questa carta viene Evocata Normalmente puoi prendere il controllo di un mostro scoperto controllato dal tuo avversario.",
            TestFonts.Default, style, 1200, 4000, Dpi);

        Assert.True(fitted.Lines.Count > 1);
        Assert.All(fitted.Lines, line => Assert.True(line.WidthPx <= 1200.5f));
    }

    [Fact]
    public void GliACapoEsplicitiSonoRispettati()
    {
        var style = Style(AutoFitMode.None, sizePt: 8);

        var fitted = Engine.Fit("Riga uno\nRiga due\nRiga tre", TestFonts.Default, style, 3000, 4000, Dpi);

        Assert.Equal(3, fitted.Lines.Count);
    }

    [Fact]
    public void UnaParolaPiuLargaDellaCasellaVieneSpezzataSenzaCicliInfiniti()
    {
        var style = Style(AutoFitMode.None, sizePt: 12);

        var fitted = Engine.Fit(new string('M', 200), TestFonts.Default, style, 300, 40000, Dpi);

        Assert.True(fitted.Lines.Count > 1);
        Assert.All(fitted.Lines, line => Assert.True(line.WidthPx <= 300.5f));
    }

    [Fact]
    public void SeIlTestoNonEntraNemmenoAlMinimoVieneSegnalatoLOverflow()
    {
        var style = Style(AutoFitMode.ShrinkAndCondense, sizePt: 12, minSizePt: 10, minScaleX: 0.95) with { MaxLines = 1 };

        var fitted = Engine.Fit(new string('W', 120), TestFonts.Default, style, 200, 100, Dpi);

        Assert.True(fitted.Overflowed);
    }

    [Fact]
    public void IlTestoVuotoNonProduceRighe()
    {
        var fitted = Engine.Fit(null, TestFonts.Default, Style(AutoFitMode.None), 1000, 1000, Dpi);

        Assert.Empty(fitted.Lines);
    }

    [Theory]
    [InlineData(TextTransform.Upper, "DRAGO")]
    [InlineData(TextTransform.Lower, "drago")]
    [InlineData(TextTransform.None, "Drago")]
    public void LaTrasformazioneDelTestoVieneApplicata(TextTransform transform, string expected)
    {
        var style = Style(AutoFitMode.None) with { Transform = transform };

        var fitted = Engine.Fit("Drago", TestFonts.Default, style, 4000, 1000, Dpi);

        Assert.Equal(expected, fitted.Lines[0].Text);
    }

    [Fact]
    public void ADpiDiversiIlTestoOccupaLaStessaFrazioneDiCarta()
    {
        var style = Style(AutoFitMode.None, sizePt: 10);
        const string Text = "Drago Bianco";

        var low = Engine.Fit(Text, TestFonts.Default, style, 500, 500, 150);
        var high = Engine.Fit(Text, TestFonts.Default, style, 2000, 2000, 600);

        // Quadruplicando il DPI e la casella, la proporzione del testo non deve cambiare.
        Assert.Equal(low.Lines[0].WidthPx / 500f, high.Lines[0].WidthPx / 2000f, 0.02);
    }
}
