using System.Diagnostics;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Painters;
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
    private readonly ImageLayerPainter _imagePainter = new();
    private readonly SymbolLayerPainter _symbolPainter = new();
    private readonly ShapeLayerPainter _shapePainter = new();
    private readonly TextLayerPainter _textPainter = new(textEngine);
    private readonly ContainerLayerPainter _containerPainter = new();
    private readonly OverlayLayerPainter _overlayPainter = new();

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

        var context = new PaintContext(request, geometry, binder, evaluator, warnings, textEngine);

        foreach (var (layer, opacity) in CollectVisibleLayers(layout.Layers, evaluator, 1.0))
        {
            PaintLayer(canvas, layer, opacity, context);
        }

        if (request.ShowGuides)
        {
            RenderPostProcessor.DrawGuides(canvas, geometry);
        }

        using var rendered = surface.Snapshot();
        using var final = RenderPostProcessor.ApplyPostProcessing(rendered, geometry, request);

        var content = RenderPostProcessor.Encode(final, request);
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

    /// <summary>Appiattisce l'albero, propaga l'opacita' dei gruppi e ordina per z-index.</summary>
    private static List<(LayerDefinition Layer, double Opacity)> CollectVisibleLayers(
        IReadOnlyList<LayerDefinition> layers,
        ConditionEvaluator evaluator,
        double inheritedOpacity)
    {
        var result = new List<(LayerDefinition Layer, double Opacity)>();
        CollectVisibleLayersRecursive(layers, evaluator, inheritedOpacity, result);
        result.Sort(static (a, b) => a.Layer.Z.CompareTo(b.Layer.Z));
        return result;
    }

    private static void CollectVisibleLayersRecursive(
        IReadOnlyList<LayerDefinition> layers,
        ConditionEvaluator evaluator,
        double inheritedOpacity,
        List<(LayerDefinition Layer, double Opacity)> accumulator)
    {
        foreach (var layer in layers)
        {
            if (!evaluator.IsSatisfied(layer.VisibleWhen))
            {
                continue;
            }

            var opacity = inheritedOpacity * layer.Opacity;

            if (layer is GroupLayer group)
            {
                CollectVisibleLayersRecursive(group.Children, evaluator, opacity, accumulator);
            }
            else
            {
                accumulator.Add((layer, opacity));
            }
        }
    }

    private void PaintLayer(SKCanvas canvas, LayerDefinition layer, double opacity, PaintContext context)
    {
        var dest = RenderDrawingUtilities.ResolveRect(layer, context.Geometry);
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

            // LOOP-PERF-002: Direct type switch dispatch with zero array enumeration and no interface dispatch
            switch (layer)
            {
                case StaticImageLayer or ImageSlotLayer:
                    _imagePainter.Paint(canvas, layer, dest, opacity, context);
                    break;
                case SymbolSlotLayer or SymbolRepeaterLayer:
                    _symbolPainter.Paint(canvas, layer, dest, opacity, context);
                    break;
                case ShapeLayer:
                    _shapePainter.Paint(canvas, layer, dest, opacity, context);
                    break;
                case TextLayer or RichTextLayer:
                    _textPainter.Paint(canvas, layer, dest, opacity, context);
                    break;
                case ToggleGroupLayer:
                    _containerPainter.Paint(canvas, layer, dest, opacity, context);
                    break;
                case OverlayLayer:
                    _overlayPainter.Paint(canvas, layer, dest, opacity, context);
                    break;
            }
        }
        finally
        {
            canvas.RestoreToCount(restore);
        }
    }

    internal static SKColor ParseColor(string? value, SKColor fallback) =>
        RenderDrawingUtilities.ParseColor(value, fallback);
}

internal static class PaintExtensions
{
    public static void Let(this SKPaint paint, Action<SKPaint> action) => action(paint);
}
