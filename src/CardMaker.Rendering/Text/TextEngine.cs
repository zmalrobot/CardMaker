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

        var rawParagraphs = text.Split('\n');
        var paragraphs = new string[rawParagraphs.Length][];
        for (var i = 0; i < rawParagraphs.Length; i++)
        {
            var p = rawParagraphs[i];
            paragraphs[i] = p.Length == 0 ? [] : p.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        using var font = new SKFont(typeface, basePx) { ScaleX = maxScaleX, Subpixel = true };
        var linesBuffer = new List<TextLine>();

        // 1. Corpo piu' grande che entra concedendo la compressione massima (ALG-PERF-003, CPU-PERF-003).
        var size = FindLargestFittingSize(paragraphs, font, style, minPx, basePx, minScaleX, lineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer);

        var usedLineHeight = lineHeight;
        if (size is null && minLineHeight < lineHeight)
        {
            // 2. Ultima risorsa prima di dichiarare l'overflow: stringere l'interlinea.
            size = FindLargestFittingSize(paragraphs, font, style, minPx, basePx, minScaleX, minLineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer);
            usedLineHeight = minLineHeight;
        }

        if (size is null)
        {
            // Anche in overflow il numero di righe va rispettato: altrimenti il testo esce dalla casella.
            var forced = Layout(paragraphs, font, style, minPx, minScaleX, minLineHeight, maxWidthPx, dpi, style.MaxLines)
                .Take(Math.Max(1, style.MaxLines))
                .ToList();

            return new FittedText(forced, minPx, minScaleX, minPx * minLineHeight, Overflowed: true)
            {
                GlyphHeightPx = GlyphHeight(typeface, minPx),
            };
        }

        // 3. Compressione minore che continua a entrare a quel corpo.
        var scaleX = FindLargestFittingScale(paragraphs, font, style, size.Value, minScaleX, maxScaleX, usedLineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer);

        var lines = Layout(paragraphs, font, style, size.Value, scaleX, usedLineHeight, maxWidthPx, dpi, style.MaxLines);
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
        string[][] paragraphs, SKFont font, TextStyle style,
        float minPx, float maxPx, float scaleX, float lineHeight,
        float maxWidthPx, float maxHeightPx, int dpi, List<TextLine> linesBuffer)
    {
        if (Fits(paragraphs, font, style, maxPx, scaleX, lineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer))
        {
            return maxPx;
        }

        if (!Fits(paragraphs, font, style, minPx, scaleX, lineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer))
        {
            return null;
        }

        var low = minPx;
        var high = maxPx;
        while (high - low > SizeTolerancePx)
        {
            var mid = (low + high) / 2f;
            if (Fits(paragraphs, font, style, mid, scaleX, lineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer))
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
        string[][] paragraphs, SKFont font, TextStyle style,
        float sizePx, float minScaleX, float maxScaleX, float lineHeight,
        float maxWidthPx, float maxHeightPx, int dpi, List<TextLine> linesBuffer)
    {
        if (Fits(paragraphs, font, style, sizePx, maxScaleX, lineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer))
        {
            return maxScaleX;
        }

        var low = minScaleX;
        var high = maxScaleX;
        while (high - low > ScaleTolerance)
        {
            var mid = (low + high) / 2f;
            if (Fits(paragraphs, font, style, sizePx, mid, lineHeight, maxWidthPx, maxHeightPx, dpi, linesBuffer))
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
        string[][] paragraphs, SKFont font, TextStyle style,
        float sizePx, float scaleX, float lineHeight,
        float maxWidthPx, float maxHeightPx, int dpi, List<TextLine> linesBuffer)
    {
        linesBuffer.Clear();
        font.Size = sizePx;
        font.ScaleX = scaleX;
        var letterSpacing = PointsToPixels(style.LetterSpacingPt, dpi);

        for (var i = 0; i < paragraphs.Length; i++)
        {
            WrapParagraph(paragraphs[i], font, letterSpacing, maxWidthPx, linesBuffer, style.MaxLines);
            if (linesBuffer.Count > style.MaxLines)
            {
                return false;
            }
        }

        if (linesBuffer.Count == 0)
        {
            return true;
        }

        var maxAllowed = maxWidthPx + 0.5f;
        // LINQ-PERF-002: Indexed loop instead of lines.Any(...)
        for (var i = 0; i < linesBuffer.Count; i++)
        {
            if (linesBuffer[i].WidthPx > maxAllowed)
            {
                return false;
            }
        }

        var totalHeight = ((linesBuffer.Count - 1) * sizePx * lineHeight) + sizePx;
        return totalHeight <= maxHeightPx + 0.5f;
    }

    private static List<TextLine> Layout(
        string[][] paragraphs, SKFont font, TextStyle style,
        float sizePx, float scaleX, float lineHeight,
        float maxWidthPx, int dpi, int maxLines)
    {
        font.Size = sizePx;
        font.ScaleX = scaleX;
        var letterSpacing = PointsToPixels(style.LetterSpacingPt, dpi);

        var lines = new List<TextLine>();
        for (var i = 0; i < paragraphs.Length; i++)
        {
            WrapParagraph(paragraphs[i], font, letterSpacing, maxWidthPx, lines, maxLines);
            if (lines.Count > maxLines)
            {
                break;
            }
        }

        return lines;
    }

    private static void WrapParagraph(
        string[] words, SKFont font, float letterSpacing, float maxWidthPx,
        List<TextLine> lines, int maxLines)
    {
        if (words.Length == 0)
        {
            lines.Add(new TextLine(string.Empty, 0));
            return;
        }

        var current = string.Empty;
        var currentWidth = 0f;

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var candidate = current.Length == 0 ? word : current + " " + word;
            var candidateWidth = Measure(candidate, font, letterSpacing);
            if (candidateWidth <= maxWidthPx)
            {
                current = candidate;
                currentWidth = candidateWidth;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(new TextLine(current, currentWidth));
                current = string.Empty;
                currentWidth = 0f;
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
                var sub = remaining[..cut];
                lines.Add(new TextLine(sub, Measure(sub, font, letterSpacing)));
                remaining = remaining[cut..];
                if (lines.Count > maxLines)
                {
                    return;
                }
            }

            current = remaining;
            currentWidth = Measure(current, font, letterSpacing);
        }

        if (current.Length > 0)
        {
            lines.Add(new TextLine(current, currentWidth));
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
