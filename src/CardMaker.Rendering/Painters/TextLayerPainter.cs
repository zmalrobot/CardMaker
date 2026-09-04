using System.Globalization;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal sealed class TextLayerPainter(TextEngine textEngine) : ILayerPainter
{
    private static readonly TextStyle DefaultTextStyle = new();

    public bool CanPaint(LayerDefinition layer) =>
        layer is TextLayer or RichTextLayer;

    public void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context)
    {
        switch (layer)
        {
            case TextLayer text:
                PaintText(canvas, text, dest, opacity, context);
                break;
            case RichTextLayer richText:
                PaintRichText(canvas, richText, dest, opacity, context);
                break;
        }
    }

    private void PaintText(SKCanvas canvas, TextLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var style = ResolveStyle(context.Request.Layout, layer);
        var content = context.Binder.Bind(layer.Source);

        if (string.IsNullOrWhiteSpace(content))
        {
            if (!layer.HideWhenEmpty)
            {
                context.Warn("text.empty", $"Testo vuoto per il layer '{layer.Name}'.", layer.Id);
            }

            return;
        }

        var typeface = context.Resources.ResolveFont(style.Font, out var isFallback);
        if (isFallback && !string.IsNullOrWhiteSpace(style.Font))
        {
            context.Warn("font.fallback",
                $"Nessun font assegnato al ruolo '{style.Font}': usato il font di ripiego.", layer.Id);
        }

        var paddingX = TextEngine.PointsToPixels(style.PaddingXPt, context.Request.Dpi);
        var paddingY = TextEngine.PointsToPixels(style.PaddingYPt, context.Request.Dpi);
        var box = new SKRect(dest.Left + paddingX, dest.Top + paddingY, dest.Right - paddingX, dest.Bottom - paddingY);
        if (box.Width <= 0 || box.Height <= 0)
        {
            return;
        }

        var fitted = textEngine.Fit(content, typeface, style, box.Width, box.Height, context.Request.Dpi);
        if (fitted.Lines.Count == 0)
        {
            return;
        }

        if (fitted.Overflowed)
        {
            context.Warn("text.overflow",
                $"Il testo del layer '{layer.Name}' non entra nella casella nemmeno al minimo consentito.", layer.Id);
        }

        DrawFittedText(canvas, fitted, style, typeface, box, opacity, context.Request.Dpi);
    }

    private sealed record MeasuredWord(RichRunKind Kind, string Text, string? SymbolSetKey, string? SymbolKey, float WidthPx);

    private void PaintRichText(SKCanvas canvas, RichTextLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var style = ResolveStyle(context.Request.Layout, layer.Style, layer.StyleOverrides);
        var content = context.Binder.Bind(layer.Source);

        if (string.IsNullOrWhiteSpace(content))
        {
            if (!layer.HideWhenEmpty)
            {
                context.Warn("text.empty", $"Testo vuoto per il layer '{layer.Name}'.", layer.Id);
            }

            return;
        }

        var regularFace = context.Resources.ResolveFont(style.Font, out var isFallback);
        if (isFallback && !string.IsNullOrWhiteSpace(style.Font))
        {
            context.Warn("font.fallback",
                $"Nessun font assegnato al ruolo '{style.Font}': usato il font di ripiego.", layer.Id);
        }

        var boldFace = layer.BoldFont is { } b ? context.Resources.ResolveFont(b, out _) : regularFace;
        var italicFace = layer.ItalicFont is { } it ? context.Resources.ResolveFont(it, out _) : regularFace;
        var boldItalicFace = layer.BoldItalicFont is { } bi ? context.Resources.ResolveFont(bi, out _) : boldFace;

        var paddingX = TextEngine.PointsToPixels(style.PaddingXPt, context.Request.Dpi);
        var paddingY = TextEngine.PointsToPixels(style.PaddingYPt, context.Request.Dpi);
        var box = new SKRect(dest.Left + paddingX, dest.Top + paddingY, dest.Right - paddingX, dest.Bottom - paddingY);
        if (box.Width <= 0 || box.Height <= 0)
        {
            return;
        }

        var sizePx = TextEngine.PointsToPixels(style.SizePt, context.Request.Dpi);
        var lineHeightPx = sizePx * (float)style.LineHeight;

        using var regularFont = new SKFont(regularFace, sizePx);
        using var boldFont = new SKFont(boldFace, sizePx);
        using var italicFont = new SKFont(italicFace, sizePx);
        using var boldItalicFont = new SKFont(boldItalicFace, sizePx);

        SKFont FontFor(RichRunKind kind) => kind switch
        {
            RichRunKind.Bold or RichRunKind.SectionLabel => boldFont,
            RichRunKind.Italic => italicFont,
            RichRunKind.BoldItalic => boldItalicFont,
            _ => regularFont,
        };

        var paragraphs = RichTextParser.Parse(content, layer.DefaultSymbolSetKey);
        var lines = new List<List<MeasuredWord>>();

        foreach (var paragraph in paragraphs)
        {
            var words = new List<MeasuredWord>();
            if (paragraph.IsBullet)
            {
                words.Add(new MeasuredWord(RichRunKind.Text, "\u2022", null, null, TextEngine.Measure("\u2022", regularFont, 0)));
            }

            foreach (var run in paragraph.Runs)
            {
                if (run.Kind == RichRunKind.Symbol)
                {
                    words.Add(new MeasuredWord(run.Kind, string.Empty, run.SymbolSetKey, run.SymbolKey, sizePx));
                    continue;
                }

                foreach (var part in run.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    words.Add(new MeasuredWord(run.Kind, part, null, null, TextEngine.Measure(part, FontFor(run.Kind), 0)));
                }
            }

            var wrapped = WrapWords(words, box.Width, regularFont);
            lines.AddRange(wrapped.Count == 0 ? [[]] : wrapped);
        }

        var maxLines = Math.Max(1, (int)(box.Height / lineHeightPx));
        if (lines.Count > maxLines)
        {
            context.Warn("text.overflow",
                $"Il testo del layer '{layer.Name}' non entra nella casella nemmeno al minimo consentito.", layer.Id);
        }

        DrawRichLines(canvas, [.. lines.Take(maxLines)], box, style, layer, FontFor, regularFont, sizePx, lineHeightPx, opacity, context);
    }

    private static float SpaceWidth(SKFont font) => TextEngine.Measure(" ", font, 0);

    private static List<List<MeasuredWord>> WrapWords(List<MeasuredWord> words, float maxWidth, SKFont regularFont)
    {
        var lines = new List<List<MeasuredWord>>();
        if (words.Count == 0)
        {
            return lines;
        }

        var space = SpaceWidth(regularFont);
        var current = new List<MeasuredWord>();
        var currentWidth = 0f;

        foreach (var word in words)
        {
            var extra = current.Count == 0 ? 0 : space;
            if (current.Count > 0 && currentWidth + extra + word.WidthPx > maxWidth)
            {
                lines.Add(current);
                current = [];
                currentWidth = 0;
                extra = 0;
            }

            current.Add(word);
            currentWidth += extra + word.WidthPx;
        }

        lines.Add(current);
        return lines;
    }

    private static void DrawRichLines(
        SKCanvas canvas, List<List<MeasuredWord>> lines, SKRect box, TextStyle style, RichTextLayer layer,
        Func<RichRunKind, SKFont> fontFor, SKFont regularFont, float sizePx, float lineHeightPx,
        double opacity, PaintContext context)
    {
        var metrics = regularFont.Metrics;
        var glyphHeight = metrics.Descent - metrics.Ascent;
        var alpha = (byte)Math.Clamp(opacity * 255, 0, 255);

        using var textPaint = new SKPaint { Color = RenderDrawingUtilities.ParseColor(style.Color, SKColors.Black).WithAlpha(alpha), IsAntialias = true };
        using var labelPaint = new SKPaint
        {
            Color = RenderDrawingUtilities.ParseColor(layer.SectionLabelColor ?? style.Color, SKColors.Black).WithAlpha(alpha),
            IsAntialias = true,
        };
        using var imagePaint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
        var space = SpaceWidth(regularFont);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Count == 0)
            {
                continue;
            }

            var totalWidth = line.Sum(w => w.WidthPx) + (space * (line.Count - 1));
            var x = style.Align switch
            {
                TextAlign.Center => box.Left + ((box.Width - totalWidth) / 2f),
                TextAlign.Right => box.Right - totalWidth,
                _ => box.Left,
            };
            var lineTop = box.Top + (i * lineHeightPx);
            var baseline = lineTop + ((sizePx - glyphHeight) / 2f) - metrics.Ascent;

            foreach (var word in line)
            {
                if (word.Kind == RichRunKind.Symbol)
                {
                    var image = word.SymbolSetKey is not null && word.SymbolKey is not null
                        ? context.Resources.GetSymbol(word.SymbolSetKey, word.SymbolKey)
                        : null;

                    if (image is null)
                    {
                        if (word.SymbolSetKey is not null && word.SymbolKey is not null)
                        {
                            context.Warn("symbol.missing", $"Simbolo '{word.SymbolSetKey}/{word.SymbolKey}' non trovato.", layer.Id);
                        }
                    }
                    else
                    {
                        var symbolRect = new SKRect(x, lineTop, x + word.WidthPx, lineTop + sizePx);
                        RenderDrawingUtilities.DrawImage(canvas, image, symbolRect, ImageFit.Contain, imagePaint, 1.0, 0, 0);
                    }

                    x += word.WidthPx + space;
                    continue;
                }

                var paint = word.Kind == RichRunKind.SectionLabel ? labelPaint : textPaint;
                canvas.DrawText(word.Text, x, baseline, SKTextAlign.Left, fontFor(word.Kind), paint);
                x += word.WidthPx + space;
            }
        }
    }

    private static void DrawFittedText(
        SKCanvas canvas, FittedText fitted, TextStyle style, SKTypeface typeface,
        SKRect box, double opacity, int dpi)
    {
        using var font = new SKFont(typeface, fitted.SizePx) { ScaleX = fitted.ScaleX, Subpixel = true };
        var metrics = font.Metrics;
        var glyphHeight = metrics.Descent - metrics.Ascent;
        var letterSpacing = TextEngine.PointsToPixels(style.LetterSpacingPt, dpi);

        var alpha = (byte)Math.Clamp(opacity * 255, 0, 255);
        using var paint = new SKPaint
        {
            Color = RenderDrawingUtilities.ParseColor(style.Color, SKColors.Black).WithAlpha(alpha),
            IsAntialias = true,
        };

        if (style.Shadow is { } shadow)
        {
            paint.ImageFilter = SKImageFilter.CreateDropShadow(
                TextEngine.PointsToPixels(shadow.OffsetXPt, dpi),
                TextEngine.PointsToPixels(shadow.OffsetYPt, dpi),
                TextEngine.PointsToPixels(shadow.BlurPt, dpi),
                TextEngine.PointsToPixels(shadow.BlurPt, dpi),
                RenderDrawingUtilities.ParseColor(shadow.Color, SKColors.Black));
        }

        SKPaint? strokePaint = null;
        if (style.Stroke is { } stroke)
        {
            strokePaint = new SKPaint
            {
                Color = RenderDrawingUtilities.ParseColor(stroke.Color, SKColors.Black).WithAlpha(alpha),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = TextEngine.PointsToPixels(stroke.WidthPt, dpi),
                StrokeJoin = SKStrokeJoin.Round,
            };
        }

        var visualHeight = ((fitted.Lines.Count - 1) * fitted.LineHeightPx) + glyphHeight;
        var startY = style.VerticalAlign switch
        {
            VerticalAlign.Middle => box.Top + ((box.Height - visualHeight) / 2f),
            VerticalAlign.Bottom => box.Bottom - visualHeight,
            _ => box.Top,
        };

        try
        {
            for (var i = 0; i < fitted.Lines.Count; i++)
            {
                var line = fitted.Lines[i];
                if (line.Text.Length == 0)
                {
                    continue;
                }

                var baseline = startY + (i * fitted.LineHeightPx) - metrics.Ascent;

                var isLastLine = i == fitted.Lines.Count - 1;
                DrawLine(canvas, line, style, font, paint, strokePaint, box, baseline, letterSpacing, isLastLine);
            }
        }
        finally
        {
            strokePaint?.Dispose();
        }
    }

    private static void DrawLine(
        SKCanvas canvas, TextLine line, TextStyle style, SKFont font,
        SKPaint paint, SKPaint? strokePaint, SKRect box, float baseline,
        float letterSpacing, bool isLastLine)
    {
        if (style.Align == TextAlign.Justify && !isLastLine && line.Text.Contains(' ', StringComparison.Ordinal))
        {
            DrawJustified(canvas, line, font, paint, strokePaint, box, baseline, letterSpacing);
            return;
        }

        var x = style.Align switch
        {
            TextAlign.Center => box.Left + ((box.Width - line.WidthPx) / 2f),
            TextAlign.Right => box.Right - line.WidthPx,
            _ => box.Left,
        };

        if (letterSpacing != 0)
        {
            DrawSpaced(canvas, line.Text, font, paint, strokePaint, x, baseline, letterSpacing);
            return;
        }

        strokePaint?.Let(sp => canvas.DrawText(line.Text, x, baseline, SKTextAlign.Left, font, sp));
        canvas.DrawText(line.Text, x, baseline, SKTextAlign.Left, font, paint);
    }

    private static void DrawJustified(
        SKCanvas canvas, TextLine line, SKFont font, SKPaint paint, SKPaint? strokePaint,
        SKRect box, float baseline, float letterSpacing)
    {
        var words = line.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            strokePaint?.Let(sp => canvas.DrawText(line.Text, box.Left, baseline, SKTextAlign.Left, font, sp));
            canvas.DrawText(line.Text, box.Left, baseline, SKTextAlign.Left, font, paint);
            return;
        }

        var wordsWidth = words.Sum(w => TextEngine.Measure(w, font, letterSpacing));
        var gap = (box.Width - wordsWidth) / (words.Length - 1);

        var x = box.Left;
        foreach (var word in words)
        {
            strokePaint?.Let(sp => canvas.DrawText(word, x, baseline, SKTextAlign.Left, font, sp));
            canvas.DrawText(word, x, baseline, SKTextAlign.Left, font, paint);
            x += TextEngine.Measure(word, font, letterSpacing) + gap;
        }
    }

    private static void DrawSpaced(
        SKCanvas canvas, string text, SKFont font, SKPaint paint, SKPaint? strokePaint,
        float x, float baseline, float letterSpacing)
    {
        foreach (var character in text)
        {
            var glyph = character.ToString(CultureInfo.InvariantCulture);
            strokePaint?.Let(sp => canvas.DrawText(glyph, x, baseline, SKTextAlign.Left, font, sp));
            canvas.DrawText(glyph, x, baseline, SKTextAlign.Left, font, paint);
            x += font.MeasureText(glyph) + letterSpacing;
        }
    }

    private static TextStyle ResolveStyle(CardLayout layout, TextLayer layer) =>
        ResolveStyle(layout, layer.Style, layer.StyleOverrides);

    private static TextStyle ResolveStyle(CardLayout layout, string? styleName, TextStyleOverrides? overrides)
    {
        var baseStyle = styleName is not null && layout.TextStyles.TryGetValue(styleName, out var found)
            ? found
            : DefaultTextStyle;

        return baseStyle.Merge(overrides);
    }
}

