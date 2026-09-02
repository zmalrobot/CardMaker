using System.Globalization;
using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Text;

public sealed record TextLine(string Text, float WidthPx);

public sealed record FittedText(
    IReadOnlyList<TextLine> Lines,
    float SizePx,
    float ScaleX,
    float LineHeightPx,
    bool Overflowed)
{
    /// <summary>Altezza reale dei glifi (ascent + descent), usata per posizionare la baseline.</summary>
    public float GlyphHeightPx { get; init; }

    /// <summary>
    /// Altezza occupata. Il budget di una riga e' il <b>quadratone</b> (il corpo del font), non
    /// ascent+descent: e' cosi' che ragiona chi disegna un template, e ascendenti e discendenti
    /// possono sbordare di poco come sulle carte vere. L'interlinea vale solo <b>fra</b> le righe.
    /// </summary>
    public float TotalHeightPx => Lines.Count == 0
        ? 0
        : ((Lines.Count - 1) * LineHeightPx) + SizePx;

    public static readonly FittedText Empty = new([], 0, 1, 0, false);
}

/// <summary>
/// Fase MEASURE per i testi: manda a capo e adatta il testo alla casella.
/// <para>
/// Strategia: si cerca il <b>corpo piu' grande</b> che entra concedendo la massima compressione,
/// poi la <b>compressione minore</b> che continua a entrare a quel corpo. Il risultato e' il testo
/// piu' grande e meno deformato possibile, che e' esattamente cio' che fanno le carte vere.
/// </para>
/// </summary>
public sealed class TextEngine
{
    private const float SizeTolerancePx = 0.25f;
    private const float ScaleTolerance = 0.01f;

    public FittedText Fit(
        string? text,
        SKTypeface typeface,
        TextStyle style,
        float maxWidthPx,
        float maxHeightPx,
        int dpi)
    {
        ArgumentNullException.ThrowIfNull(typeface);
        ArgumentNullException.ThrowIfNull(style);

        text = Transform(text, style.Transform);
        if (string.IsNullOrEmpty(text) || maxWidthPx <= 0 || maxHeightPx <= 0)
        {
            return FittedText.Empty;
        }

        var basePx = PointsToPixels(style.SizePt, dpi);
        var autoFit = style.AutoFit ?? AutoFitSettings.None;
        var canShrink = autoFit.Mode is AutoFitMode.Shrink or AutoFitMode.ShrinkAndCondense;
        var canCondense = autoFit.Mode is AutoFitMode.Condense or AutoFitMode.ShrinkAndCondense;

        var minPx = canShrink ? Math.Min(basePx, PointsToPixels(autoFit.MinSizePt, dpi)) : basePx;
        var maxScaleX = (float)style.ScaleX;
        var minScaleX = canCondense ? Math.Min(maxScaleX, (float)autoFit.MinScaleX) : maxScaleX;
        var lineHeight = (float)style.LineHeight;
        var minLineHeight = canShrink ? Math.Min(lineHeight, (float)autoFit.MinLineHeight) : lineHeight;

        // 1. Corpo piu' grande che entra concedendo la compressione massima.
        var size = FindLargestFittingSize(text, typeface, style, minPx, basePx, minScaleX, lineHeight, maxWidthPx, maxHeightPx, dpi);

        var usedLineHeight = lineHeight;
        if (size is null && minLineHeight < lineHeight)
        {
            // 2. Ultima risorsa prima di dichiarare l'overflow: stringere l'interlinea.
            size = FindLargestFittingSize(text, typeface, style, minPx, basePx, minScaleX, minLineHeight, maxWidthPx, maxHeightPx, dpi);
            usedLineHeight = minLineHeight;
        }

        if (size is null)
        {
            // Anche in overflow il numero di righe va rispettato: altrimenti il testo esce dalla casella.
            var forced = Layout(text, typeface, style, minPx, minScaleX, minLineHeight, maxWidthPx, dpi, style.MaxLines)
                .Take(Math.Max(1, style.MaxLines))
                .ToList();

            return new FittedText(forced, minPx, minScaleX, minPx * minLineHeight, Overflowed: true)
            {
                GlyphHeightPx = GlyphHeight(typeface, minPx),
            };
        }

        // 3. Compressione minore che continua a entrare a quel corpo.
        var scaleX = FindLargestFittingScale(text, typeface, style, size.Value, minScaleX, maxScaleX, usedLineHeight, maxWidthPx, maxHeightPx, dpi);

        var lines = Layout(text, typeface, style, size.Value, scaleX, usedLineHeight, maxWidthPx, dpi, style.MaxLines);
        return new FittedText(lines, size.Value, scaleX, size.Value * usedLineHeight, Overflowed: false)
        {
            GlyphHeightPx = GlyphHeight(typeface, size.Value),
        };
    }

    internal static float GlyphHeight(SKTypeface typeface, float sizePx)
    {
        using var font = new SKFont(typeface, sizePx);
        var metrics = font.Metrics;
        return metrics.Descent - metrics.Ascent;
    }

    public static float PointsToPixels(double points, int dpi) => (float)(points / 72.0 * dpi);

    private float? FindLargestFittingSize(
        string text, SKTypeface typeface, TextStyle style,
        float minPx, float maxPx, float scaleX, float lineHeight,
        float maxWidthPx, float maxHeightPx, int dpi)
    {
        if (Fits(text, typeface, style, maxPx, scaleX, lineHeight, maxWidthPx, maxHeightPx, dpi))
        {
            return maxPx;
        }

        if (!Fits(text, typeface, style, minPx, scaleX, lineHeight, maxWidthPx, maxHeightPx, dpi))
        {
            return null;
        }

        var low = minPx;
        var high = maxPx;
        while (high - low > SizeTolerancePx)
        {
            var mid = (low + high) / 2f;
            if (Fits(text, typeface, style, mid, scaleX, lineHeight, maxWidthPx, maxHeightPx, dpi))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private float FindLargestFittingScale(
        string text, SKTypeface typeface, TextStyle style,
        float sizePx, float minScaleX, float maxScaleX, float lineHeight,
        float maxWidthPx, float maxHeightPx, int dpi)
    {
        if (Fits(text, typeface, style, sizePx, maxScaleX, lineHeight, maxWidthPx, maxHeightPx, dpi))
        {
            return maxScaleX;
        }

        var low = minScaleX;
        var high = maxScaleX;
        while (high - low > ScaleTolerance)
        {
            var mid = (low + high) / 2f;
            if (Fits(text, typeface, style, sizePx, mid, lineHeight, maxWidthPx, maxHeightPx, dpi))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private bool Fits(
        string text, SKTypeface typeface, TextStyle style,
        float sizePx, float scaleX, float lineHeight,
        float maxWidthPx, float maxHeightPx, int dpi)
    {
        var lines = Layout(text, typeface, style, sizePx, scaleX, lineHeight, maxWidthPx, dpi, style.MaxLines);
        if (lines.Count == 0)
        {
            return true;
        }

        if (lines.Count > style.MaxLines)
        {
            return false;
        }

        if (lines.Any(l => l.WidthPx > maxWidthPx + 0.5f))
        {
            return false;
        }

        var totalHeight = ((lines.Count - 1) * sizePx * lineHeight) + sizePx;
        return totalHeight <= maxHeightPx + 0.5f;
    }

    private List<TextLine> Layout(
        string text, SKTypeface typeface, TextStyle style,
        float sizePx, float scaleX, float lineHeight,
        float maxWidthPx, int dpi, int maxLines)
    {
        using var font = new SKFont(typeface, sizePx) { ScaleX = scaleX, Subpixel = true };
        var letterSpacing = PointsToPixels(style.LetterSpacingPt, dpi);

        var lines = new List<TextLine>();
        foreach (var paragraph in text.Split('\n'))
        {
            WrapParagraph(paragraph, font, letterSpacing, maxWidthPx, lines, maxLines);
            if (lines.Count > maxLines)
            {
                break;
            }
        }

        return lines;
    }

    private static void WrapParagraph(
        string paragraph, SKFont font, float letterSpacing, float maxWidthPx,
        List<TextLine> lines, int maxLines)
    {
        if (paragraph.Length == 0)
        {
            lines.Add(new TextLine(string.Empty, 0));
            return;
        }

        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            lines.Add(new TextLine(string.Empty, 0));
            return;
        }

        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (Measure(candidate, font, letterSpacing) <= maxWidthPx)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(new TextLine(current, Measure(current, font, letterSpacing)));
                current = string.Empty;
                if (lines.Count > maxLines)
                {
                    return;
                }
            }

            // Parola singola piu' larga della casella: spezzata a forza, altrimenti il wrap non termina.
            var remaining = word;
            while (Measure(remaining, font, letterSpacing) > maxWidthPx && remaining.Length > 1)
            {
                var cut = FindBreakPoint(remaining, font, letterSpacing, maxWidthPx);
                lines.Add(new TextLine(remaining[..cut], Measure(remaining[..cut], font, letterSpacing)));
                remaining = remaining[cut..];
                if (lines.Count > maxLines)
                {
                    return;
                }
            }

            current = remaining;
        }

        if (current.Length > 0)
        {
            lines.Add(new TextLine(current, Measure(current, font, letterSpacing)));
        }
    }

    private static int FindBreakPoint(string word, SKFont font, float letterSpacing, float maxWidthPx)
    {
        var low = 1;
        var high = word.Length - 1;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (Measure(word[..mid], font, letterSpacing) <= maxWidthPx)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    internal static float Measure(string text, SKFont font, float letterSpacing)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var width = font.MeasureText(text);
        if (letterSpacing != 0)
        {
            width += letterSpacing * (text.Length - 1);
        }

        return width;
    }

    private static string? Transform(string? text, TextTransform transform)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return transform switch
        {
            TextTransform.Upper => text.ToUpperInvariant(),
            TextTransform.Lower => text.ToLowerInvariant(),
            TextTransform.Title => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()),
            _ => text,
        };
    }
}
