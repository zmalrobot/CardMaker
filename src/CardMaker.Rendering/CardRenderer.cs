using System.Diagnostics;
using System.Globalization;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Pipeline;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Rendering;

/// <summary>
/// Motore di rendering unico e data-driven (ADR-001).
/// Pipeline: RESOLVE -> BIND -> EVALUATE -> MEASURE -> PAINT -> POST.
/// Anteprima ed export usano questo stesso codice: cambia solo il DPI (ADR-003).
/// </summary>
public sealed class CardRenderer(TextEngine textEngine)
{
    private static readonly TextStyle DefaultTextStyle = new();

    public CardRenderResult Render(CardRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<RenderWarning>();

        var layout = request.Layout;
        var geometry = layout.Canvas.ToGeometry(request.Dpi);

        var binder = new ValueBinder(request.Values, layout.Computed);
        var evaluator = new ConditionEvaluator(binder);

        var info = new SKImageInfo(geometry.MasterWidthPx, geometry.MasterHeightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(ParseColor(layout.Canvas.Background, SKColors.Transparent));

        var context = new PaintContext(request, geometry, binder, evaluator, warnings);

        foreach (var (layer, opacity) in CollectVisibleLayers(layout.Layers, evaluator, 1.0))
        {
            PaintLayer(canvas, layer, opacity, context);
        }

        if (request.ShowGuides)
        {
            DrawGuides(canvas, geometry);
        }

        using var rendered = surface.Snapshot();
        using var final = ApplyPostProcessing(rendered, geometry, request);

        var content = Encode(final, request);
        stopwatch.Stop();

        return new CardRenderResult(
            content,
            request.Format == RenderOutputFormat.Png ? "image/png" : "image/jpeg",
            final.Width,
            final.Height,
            warnings,
            stopwatch.Elapsed)
        {
            Geometry = geometry,
            Dpi = request.Dpi,
        };
    }

    private sealed record PaintContext(
        CardRenderRequest Request,
        CardGeometry Geometry,
        ValueBinder Binder,
        ConditionEvaluator Evaluator,
        List<RenderWarning> Warnings)
    {
        public IRenderResources Resources => Request.Resources;

        public void Warn(string code, string message, string? layerId = null) =>
            Warnings.Add(new RenderWarning(code, message, layerId));
    }

    /// <summary>Appiattisce l'albero, propaga l'opacita' dei gruppi e ordina per z-index.</summary>
    private static List<(LayerDefinition Layer, double Opacity)> CollectVisibleLayers(
        IReadOnlyList<LayerDefinition> layers,
        ConditionEvaluator evaluator,
        double inheritedOpacity)
    {
        var result = new List<(LayerDefinition, double)>();

        foreach (var layer in layers)
        {
            if (!evaluator.IsSatisfied(layer.VisibleWhen))
            {
                continue;
            }

            var opacity = inheritedOpacity * layer.Opacity;

            if (layer is GroupLayer group)
            {
                result.AddRange(CollectVisibleLayers(group.Children, evaluator, opacity));
            }
            else
            {
                result.Add((layer, opacity));
            }
        }

        return [.. result.OrderBy(item => item.Item1.Z)];
    }

    private void PaintLayer(SKCanvas canvas, LayerDefinition layer, double opacity, PaintContext context)
    {
        var dest = ResolveRect(layer, context.Geometry);
        if (dest.Width <= 0 || dest.Height <= 0)
        {
            return;
        }

        var restore = canvas.Save();
        try
        {
            if (layer.RotationDeg != 0)
            {
                canvas.RotateDegrees((float)layer.RotationDeg, dest.MidX, dest.MidY);
            }

            switch (layer)
            {
                case StaticImageLayer staticImage:
                    PaintStaticImage(canvas, staticImage, dest, opacity, context);
                    break;
                case ImageSlotLayer imageSlot:
                    PaintImageSlot(canvas, imageSlot, dest, opacity, context);
                    break;
                case SymbolSlotLayer symbol:
                    PaintSymbol(canvas, symbol, dest, opacity, context);
                    break;
                case ShapeLayer shape:
                    PaintShape(canvas, shape, dest, opacity);
                    break;
                case TextLayer text:
                    PaintText(canvas, text, dest, opacity, context);
                    break;
                case SymbolRepeaterLayer repeater:
                    PaintSymbolRepeater(canvas, repeater, dest, opacity, context);
                    break;
                case ToggleGroupLayer toggle:
                    PaintToggleGroup(canvas, toggle, dest, opacity, context);
                    break;
                case RichTextLayer richText:
                    PaintRichText(canvas, richText, dest, opacity, context);
                    break;
                case OverlayLayer overlay:
                    PaintOverlay(canvas, overlay, dest, opacity, context);
                    break;
            }
        }
        finally
        {
            canvas.RestoreToCount(restore);
        }
    }

    private static void PaintStaticImage(
        SKCanvas canvas, StaticImageLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var image = layer.AssetId is { } id
            ? context.Resources.GetImage(id)
            : layer.AssetKey is { } key ? context.Resources.GetImageByKey(key) : null;

        if (image is null)
        {
            context.Warn("asset.missing", $"Asset non trovato per il layer '{layer.Name}'.", layer.Id);
            return;
        }

        using var paint = CreateImagePaint(opacity, layer.BlendMode);
        DrawImage(canvas, image, dest, layer.Fit, paint, 1.0, 0, 0);
    }

    /// <summary>
    /// Il blend mode e l'opacita' vanno applicati quando lo strato isolato si fonde con lo sfondo
    /// (in <c>SaveLayer</c>), non mentre si disegna dentro: cosi' la maschera modella l'alpha
    /// dell'overlay <b>prima</b> che il blend (es. multiply per il foil) veda lo sfondo sottostante.
    /// </summary>
    private static void PaintOverlay(SKCanvas canvas, OverlayLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var image = layer.AssetId is { } id
            ? context.Resources.GetImage(id)
            : layer.AssetKey is { } key ? context.Resources.GetImageByKey(key) : null;

        if (image is null)
        {
            context.Warn("asset.missing", $"Asset non trovato per il layer '{layer.Name}'.", layer.Id);
            return;
        }

        SKImage? mask = layer.MaskAssetId is { } maskId
            ? context.Resources.GetImage(maskId)
            : layer.MaskAssetKey is { } maskKey ? context.Resources.GetImageByKey(maskKey) : null;

        if (mask is null && (layer.MaskAssetId is not null || layer.MaskAssetKey is not null))
        {
            context.Warn("asset.missing", $"Maschera non trovata per il layer '{layer.Name}'.", layer.Id);
        }

        using var layerPaint = CreateImagePaint(opacity, layer.BlendMode);
        var restore = canvas.SaveLayer(layerPaint);
        try
        {
            using var normalPaint = new SKPaint { IsAntialias = true };
            DrawImage(canvas, image, dest, layer.Fit, normalPaint, 1.0, 0, 0);

            if (mask is not null)
            {
                using var maskPaint = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.DstIn };
                DrawImage(canvas, mask, dest, ImageFit.Stretch, maskPaint, 1.0, 0, 0);
            }
        }
        finally
        {
            canvas.RestoreToCount(restore);
        }
    }

    private static void PaintImageSlot(
        SKCanvas canvas, ImageSlotLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var value = context.Binder.Get(layer.FieldKey);
        SKImage? image = null;

        if (value is not null && Guid.TryParse(value.AsText(), out var assetId))
        {
            image = context.Resources.GetImage(assetId);
        }

        if (image is null && layer.PlaceholderAssetId is { } placeholder)
        {
            image = context.Resources.GetImage(placeholder);
        }

        if (image is null)
        {
            context.Warn("artwork.missing", $"Nessuna immagine per il campo '{layer.FieldKey}'.", layer.Id);
            return;
        }

        if (layer.MinSourceWidth > 0 && (image.Width < layer.MinSourceWidth || image.Height < layer.MinSourceHeight))
        {
            context.Warn(
                "artwork.lowResolution",
                $"Immagine {image.Width}x{image.Height} sotto la risoluzione consigliata "
                + $"{layer.MinSourceWidth}x{layer.MinSourceHeight}: l'export a {context.Request.Dpi} DPI risultera' sgranato.",
                layer.Id);
        }

        using var paint = CreateImagePaint(opacity, layer.BlendMode);
        DrawImage(canvas, image, dest, layer.Fit, paint, layer.Zoom, layer.OffsetX, layer.OffsetY, ResolveSlice(layer, image));
    }

    /// <summary>Ritaglia l'immagine sorgente in fette uguali (Maximum Monster Rush): 1 fetta = nessun crop.</summary>
    private static SKRect? ResolveSlice(ImageSlotLayer layer, SKImage image)
    {
        if (layer.SliceCount <= 1)
        {
            return null;
        }

        var index = Math.Clamp(layer.SliceIndex, 0, layer.SliceCount - 1);
        return layer.SliceAxis == SliceAxis.Horizontal
            ? new SKRect(image.Width * index / (float)layer.SliceCount, 0, image.Width * (index + 1) / (float)layer.SliceCount, image.Height)
            : new SKRect(0, image.Height * index / (float)layer.SliceCount, image.Width, image.Height * (index + 1) / (float)layer.SliceCount);
    }

    private static void PaintSymbol(
        SKCanvas canvas, SymbolSlotLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var key = layer.FieldKey is { } field
            ? context.Binder.Get(field)?.AsText()
            : layer.SymbolKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var image = context.Resources.GetSymbol(layer.SymbolSetKey, key);
        if (image is null)
        {
            context.Warn("symbol.missing", $"Simbolo '{layer.SymbolSetKey}/{key}' non trovato.", layer.Id);
            return;
        }

        using var paint = CreateImagePaint(opacity, layer.BlendMode);
        DrawImage(canvas, image, dest, layer.Fit, paint, 1.0, 0, 0);
    }

    /// <summary>Griglia a passo fisso: le posizioni non sono mai meno di <see cref="SymbolRepeaterLayer.MaxCount"/>.</summary>
    private static void PaintSymbolRepeater(
        SKCanvas canvas, SymbolRepeaterLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var maxCount = Math.Max(1, layer.MaxCount);
        var count = Math.Clamp(ResolveRepeaterCount(layer, context.Binder), 0, maxCount);
        if (count == 0)
        {
            return;
        }

        var image = context.Resources.GetSymbol(layer.SymbolSetKey, layer.SymbolKey);
        if (image is null)
        {
            context.Warn("symbol.missing", $"Simbolo '{layer.SymbolSetKey}/{layer.SymbolKey}' non trovato.", layer.Id);
            return;
        }

        var cellWidth = dest.Width / maxCount;
        var size = Math.Min(cellWidth * (1 - (float)layer.GapFraction), dest.Height);

        using var paint = CreateImagePaint(opacity, layer.BlendMode);
        for (var i = 0; i < count; i++)
        {
            var cellIndex = layer.Direction == RepeaterDirection.RightToLeft ? maxCount - 1 - i : i;
            var cellMidX = dest.Left + (cellWidth * (cellIndex + 0.5f));
            var cellRect = new SKRect(
                cellMidX - (size / 2f), dest.MidY - (size / 2f),
                cellMidX + (size / 2f), dest.MidY + (size / 2f));
            DrawImage(canvas, image, cellRect, ImageFit.Contain, paint, 1.0, 0, 0);
        }
    }

    private static int ResolveRepeaterCount(SymbolRepeaterLayer layer, ValueBinder binder)
    {
        if (layer.FieldKey is { } key)
        {
            return (int)(binder.Get(key)?.AsNumber() ?? 0);
        }

        return layer.Count;
    }

    /// <summary>Posizioni fisse on/off: usato per le frecce Link.</summary>
    private static void PaintToggleGroup(
        SKCanvas canvas, ToggleGroupLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var active = context.Binder.Get(layer.FieldKey)?.AsList() ?? [];
        var activeSet = new HashSet<string>(active, StringComparer.Ordinal);

        using var paint = CreateImagePaint(opacity, layer.BlendMode);
        foreach (var item in layer.Items)
        {
            var isOn = activeSet.Contains(item.Key);
            var symbolKey = isOn ? layer.OnSymbolKey : layer.OffSymbolKey;
            if (symbolKey is null)
            {
                continue;
            }

            var image = context.Resources.GetSymbol(layer.SymbolSetKey, symbolKey);
            if (image is null)
            {
                context.Warn("symbol.missing", $"Simbolo '{layer.SymbolSetKey}/{symbolKey}' non trovato.", layer.Id);
                continue;
            }

            var itemRect = new SKRect(
                dest.Left + (float)(item.Rect.X * dest.Width),
                dest.Top + (float)(item.Rect.Y * dest.Height),
                dest.Left + (float)(item.Rect.Right * dest.Width),
                dest.Top + (float)(item.Rect.Bottom * dest.Height));
            DrawImage(canvas, image, itemRect, ImageFit.Contain, paint, 1.0, 0, 0);
        }
    }

    private static void PaintShape(SKCanvas canvas, ShapeLayer layer, SKRect dest, double opacity)
    {
        var alpha = (byte)Math.Clamp(opacity * 255, 0, 255);

        if (layer.FillColor is not null || layer.GradientFrom is not null)
        {
            using var fill = new SKPaint { IsAntialias = true };

            if (layer.GradientFrom is not null && layer.GradientTo is not null)
            {
                var radians = layer.GradientAngleDeg * Math.PI / 180.0;
                var dx = (float)Math.Cos(radians) * dest.Width / 2f;
                var dy = (float)Math.Sin(radians) * dest.Height / 2f;
                fill.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(dest.MidX - dx, dest.MidY - dy),
                    new SKPoint(dest.MidX + dx, dest.MidY + dy),
                    [ParseColor(layer.GradientFrom, SKColors.Transparent), ParseColor(layer.GradientTo, SKColors.Transparent)],
                    SKShaderTileMode.Clamp);
            }
            else
            {
                fill.Color = ParseColor(layer.FillColor, SKColors.Transparent);
            }

            fill.Color = fill.Color.WithAlpha(alpha);
            DrawShape(canvas, layer, dest, fill);
        }

        if (layer.BorderColor is not null && layer.BorderWidthMm > 0)
        {
            using var border = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = ParseColor(layer.BorderColor, SKColors.Black).WithAlpha(alpha),
                StrokeWidth = (float)(layer.BorderWidthMm / CardGeometry.MillimetersPerInch * 600),
            };
            DrawShape(canvas, layer, dest, border);
        }
    }

    private static void DrawShape(SKCanvas canvas, ShapeLayer layer, SKRect dest, SKPaint paint)
    {
        switch (layer.Shape)
        {
            case ShapeKind.Ellipse:
                canvas.DrawOval(dest, paint);
                break;
            case ShapeKind.RoundedRect:
                var radius = (float)(layer.CornerRadius * Math.Min(dest.Width, dest.Height));
                canvas.DrawRoundRect(dest, radius, radius, paint);
                break;
            default:
                canvas.DrawRect(dest, paint);
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

    /// <summary>
    /// Nessun auto-fit: dimensione fissa con word-wrap semplice (ADR-023). Ogni paragrafo del
    /// markup va a capo per conto proprio, cosi' le sezioni etichettate Rush restano leggibili.
    /// </summary>
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

        using var textPaint = new SKPaint { Color = ParseColor(style.Color, SKColors.Black).WithAlpha(alpha), IsAntialias = true };
        using var labelPaint = new SKPaint
        {
            Color = ParseColor(layer.SectionLabelColor ?? style.Color, SKColors.Black).WithAlpha(alpha),
            IsAntialias = true,
        };
        using var imagePaint = CreateImagePaint(opacity, layer.BlendMode);
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
                        DrawImage(canvas, image, symbolRect, ImageFit.Contain, imagePaint, 1.0, 0, 0);
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
            Color = ParseColor(style.Color, SKColors.Black).WithAlpha(alpha),
            IsAntialias = true,
        };

        if (style.Shadow is { } shadow)
        {
            paint.ImageFilter = SKImageFilter.CreateDropShadow(
                TextEngine.PointsToPixels(shadow.OffsetXPt, dpi),
                TextEngine.PointsToPixels(shadow.OffsetYPt, dpi),
                TextEngine.PointsToPixels(shadow.BlurPt, dpi),
                TextEngine.PointsToPixels(shadow.BlurPt, dpi),
                ParseColor(shadow.Color, SKColors.Black));
        }

        SKPaint? strokePaint = null;
        if (style.Stroke is { } stroke)
        {
            strokePaint = new SKPaint
            {
                Color = ParseColor(stroke.Color, SKColors.Black).WithAlpha(alpha),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = TextEngine.PointsToPixels(stroke.WidthPt, dpi),
                StrokeJoin = SKStrokeJoin.Round,
            };
        }

        var totalHeight = fitted.TotalHeightPx;
        var startY = style.VerticalAlign switch
        {
            VerticalAlign.Middle => box.Top + ((box.Height - totalHeight) / 2f),
            VerticalAlign.Bottom => box.Bottom - totalHeight,
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

                // Il glifo viene centrato nel quadratone: ascendenti e discendenti sbordano in modo simmetrico.
                var baseline = startY + (i * fitted.LineHeightPx)
                    + ((fitted.SizePx - glyphHeight) / 2f) - metrics.Ascent;

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

    private static SKRect ResolveRect(LayerDefinition layer, CardGeometry geometry)
    {
        var (x, y, width, height) = geometry.ToMasterPixels(layer.Rect);

        var left = layer.Anchor switch
        {
            LayerAnchor.Top or LayerAnchor.Center or LayerAnchor.Bottom => x - (width / 2f),
            LayerAnchor.TopRight or LayerAnchor.Right or LayerAnchor.BottomRight => x - width,
            _ => x,
        };

        var top = layer.Anchor switch
        {
            LayerAnchor.Left or LayerAnchor.Center or LayerAnchor.Right => y - (height / 2f),
            LayerAnchor.BottomLeft or LayerAnchor.Bottom or LayerAnchor.BottomRight => y - height,
            _ => y,
        };

        return new SKRect(left, top, left + width, top + height);
    }

    private static SKPaint CreateImagePaint(double opacity, string? blendMode) => new()
    {
        Color = SKColors.White.WithAlpha((byte)Math.Clamp(opacity * 255, 0, 255)),
        IsAntialias = true,
        BlendMode = ParseBlendMode(blendMode),
    };

    private static void DrawImage(
        SKCanvas canvas, SKImage image, SKRect dest, ImageFit fit, SKPaint paint,
        double zoom, double offsetX, double offsetY, SKRect? sourceRect = null)
    {
        var source = sourceRect ?? new SKRect(0, 0, image.Width, image.Height);
        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

        if (fit == ImageFit.Stretch && zoom == 1.0 && offsetX == 0 && offsetY == 0)
        {
            canvas.DrawImage(image, source, dest, sampling, paint);
            return;
        }

        var scaleX = dest.Width / source.Width;
        var scaleY = dest.Height / source.Height;
        var scale = fit switch
        {
            ImageFit.Contain => Math.Min(scaleX, scaleY),
            ImageFit.Stretch => 1f,
            _ => Math.Max(scaleX, scaleY),
        } * (float)zoom;

        var drawWidth = fit == ImageFit.Stretch ? dest.Width * (float)zoom : source.Width * scale;
        var drawHeight = fit == ImageFit.Stretch ? dest.Height * (float)zoom : source.Height * scale;

        var left = dest.MidX - (drawWidth / 2f) + (float)(offsetX * dest.Width);
        var top = dest.MidY - (drawHeight / 2f) + (float)(offsetY * dest.Height);

        var restore = canvas.Save();
        canvas.ClipRect(dest, antialias: true);
        canvas.DrawImage(image, source, new SKRect(left, top, left + drawWidth, top + drawHeight), sampling, paint);
        canvas.RestoreToCount(restore);
    }

    /// <summary>Fase POST: ritaglio al trim, angoli arrotondati, appiattimento per il JPEG.</summary>
    private static SKImage ApplyPostProcessing(SKImage source, CardGeometry geometry, CardRenderRequest request)
    {
        if (request.IncludeBleed)
        {
            // Con l'abbondanza gli angoli non vanno arrotondati: quell'area serve proprio a essere tagliata.
            return SKImage.FromEncodedData(source.Encode(SKEncodedImageFormat.Png, 100)) ?? source;
        }

        var trim = new SKRectI(
            geometry.BleedPx,
            geometry.BleedPx,
            geometry.BleedPx + geometry.TrimWidthPx,
            geometry.BleedPx + geometry.TrimHeightPx);

        using var cropped = source.Subset(trim) ?? source;

        if (!request.RoundCorners || geometry.CornerRadiusPx <= 0)
        {
            return SKImage.FromEncodedData(cropped.Encode(SKEncodedImageFormat.Png, 100)) ?? cropped;
        }

        var info = new SKImageInfo(geometry.TrimWidthPx, geometry.TrimHeightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rounded = new SKRoundRect(
            new SKRect(0, 0, geometry.TrimWidthPx, geometry.TrimHeightPx),
            geometry.CornerRadiusPx,
            geometry.CornerRadiusPx);

        canvas.ClipRoundRect(rounded, antialias: true);
        canvas.DrawImage(cropped, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));

        return surface.Snapshot();
    }

    private static byte[] Encode(SKImage image, CardRenderRequest request)
    {
        if (request.Format == RenderOutputFormat.Png)
        {
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();
        }

        // Il JPEG non ha trasparenza: gli angoli arrotondati vanno appoggiati su un fondo bianco.
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));

        using var flattened = surface.Snapshot();
        using var jpeg = flattened.Encode(SKEncodedImageFormat.Jpeg, Math.Clamp(request.JpegQuality, 1, 100));
        return jpeg.ToArray();
    }

    private static void DrawGuides(SKCanvas canvas, CardGeometry geometry)
    {
        using var trim = new SKPaint
        {
            Color = SKColors.Magenta.WithAlpha(0xB0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1, geometry.TrimWidthPx * 0.002f),
            PathEffect = SKPathEffect.CreateDash([18f, 12f], 0),
        };
        canvas.DrawRect(geometry.BleedPx, geometry.BleedPx, geometry.TrimWidthPx, geometry.TrimHeightPx, trim);

        using var safe = new SKPaint
        {
            Color = SKColors.Cyan.WithAlpha(0xB0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1, geometry.TrimWidthPx * 0.002f),
            PathEffect = SKPathEffect.CreateDash([10f, 10f], 0),
        };
        var offset = geometry.BleedPx + geometry.SafeZonePx;
        canvas.DrawRect(
            offset,
            offset,
            geometry.TrimWidthPx - (2 * geometry.SafeZonePx),
            geometry.TrimHeightPx - (2 * geometry.SafeZonePx),
            safe);
    }

    internal static SKColor ParseColor(string? value, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return SKColor.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static SKBlendMode ParseBlendMode(string? value) => value?.ToLowerInvariant() switch
    {
        "multiply" => SKBlendMode.Multiply,
        "screen" => SKBlendMode.Screen,
        "overlay" => SKBlendMode.Overlay,
        "softlight" => SKBlendMode.SoftLight,
        "hardlight" => SKBlendMode.HardLight,
        "colordodge" => SKBlendMode.ColorDodge,
        "colorburn" => SKBlendMode.ColorBurn,
        "lighten" => SKBlendMode.Lighten,
        "darken" => SKBlendMode.Darken,
        "difference" => SKBlendMode.Difference,
        _ => SKBlendMode.SrcOver,
    };
}

internal static class PaintExtensions
{
    public static void Let(this SKPaint paint, Action<SKPaint> action) => action(paint);
}
