using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Text;
using SkiaSharp;
using Xunit;

namespace CardMaker.Rendering.Tests;

public sealed class TextEngineRegressionTests
{
    private const int Dpi = 300;
    private static readonly TextEngine Engine = new();

    private static TextStyle CreateStyle(double sizePt = 12, AutoFitMode mode = AutoFitMode.None, int maxLines = 10) => new()
    {
        SizePt = sizePt,
        MaxLines = maxLines,
        AutoFit = new AutoFitSettings { Mode = mode, MinSizePt = 6, MinScaleX = 0.6 },
    };

    [Fact]
    public void TEST_UNIT_013_TokenizedParagraphsProducesExactLinesAndWordWrapping()
    {
        var style = CreateStyle(sizePt: 10, maxLines: 10);
        const string text = "Prima riga del testo.\nSeconda riga del testo con molte più parole per verificare il corretto a capo.\nTerza riga breve.";

        // Act - ALG-PERF-003 & STR-PERF-003: single-pass tokenization and word wrap
        var fitted = Engine.Fit(text, TestFonts.Default, style, maxWidthPx: 500, maxHeightPx: 300, dpi: Dpi);

        // Assert
        Assert.False(fitted.Overflowed);
        Assert.True(fitted.Lines.Count >= 3);
        Assert.Equal("Prima riga del testo.", fitted.Lines[0].Text);
        Assert.Equal("Terza riga breve.", fitted.Lines[^1].Text);
    }

    [Fact]
    public void TEST_UNIT_014_ExtremelyLongWordTriggersFindBreakPointCleanly()
    {
        var style = CreateStyle(sizePt: 12, maxLines: 4);
        var longWord = new string('A', 80);

        // Act
        var fitted = Engine.Fit(longWord, TestFonts.Default, style, maxWidthPx: 200, maxHeightPx: 200, dpi: Dpi);

        // Assert: unbroken word was forcibly split across lines without infinite loop
        Assert.True(fitted.Lines.Count > 1);
        Assert.All(fitted.Lines, l => Assert.True(l.WidthPx <= 200.5f));
    }

    [Fact]
    public void TEST_UNIT_015_IndexedLoopInFitsAccuratelyDetectsWidthOverflow()
    {
        // Style without autofit, very narrow box
        var style = CreateStyle(sizePt: 16, mode: AutoFitMode.None, maxLines: 1);
        const string text = "Testo che non entra assolutamente in trenta pixel";

        // Act - LINQ-PERF-002: Fits loop
        var fitted = Engine.Fit(text, TestFonts.Default, style, maxWidthPx: 30, maxHeightPx: 100, dpi: Dpi);

        // Assert
        Assert.True(fitted.Overflowed);
    }

    [Fact]
    public void TEST_UNIT_016_ReusedSKFontConvergesToOptimalSizeAndScale()
    {
        // Style with shrink and condense
        var style = CreateStyle(sizePt: 14, mode: AutoFitMode.ShrinkAndCondense, maxLines: 1);
        const string text = "Titolo Carta Molto Lungo Da Adattare";

        // Act - CPU-PERF-003: reusable SKFont binary search
        var fitted = Engine.Fit(text, TestFonts.Default, style, maxWidthPx: 300, maxHeightPx: 50, dpi: Dpi);

        // Assert
        Assert.False(fitted.Overflowed);
        Assert.Single(fitted.Lines);
        Assert.True(fitted.Lines[0].WidthPx <= 300.5f);
        Assert.True(fitted.SizePx <= TextEngine.PointsToPixels(14, Dpi));
    }
}
