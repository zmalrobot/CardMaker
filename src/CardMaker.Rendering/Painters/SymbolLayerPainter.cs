using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal sealed class SymbolLayerPainter : ILayerPainter
{
    public bool CanPaint(LayerDefinition layer) =>
        layer is SymbolSlotLayer or SymbolRepeaterLayer;

    public void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context)
    {
        switch (layer)
        {
            case SymbolSlotLayer symbol:
                PaintSymbol(canvas, symbol, dest, opacity, context);
                break;
            case SymbolRepeaterLayer repeater:
                PaintSymbolRepeater(canvas, repeater, dest, opacity, context);
                break;
        }
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

        using var paint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
        RenderDrawingUtilities.DrawImage(canvas, image, dest, layer.Fit, paint, 1.0, 0, 0);
    }

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

        using var paint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
        for (var i = 0; i < count; i++)
        {
            var cellIndex = layer.Direction == RepeaterDirection.RightToLeft ? maxCount - 1 - i : i;
            var cellMidX = dest.Left + (cellWidth * (cellIndex + 0.5f));
            var cellRect = new SKRect(
                cellMidX - (size / 2f), dest.MidY - (size / 2f),
                cellMidX + (size / 2f), dest.MidY + (size / 2f));
            RenderDrawingUtilities.DrawImage(canvas, image, cellRect, ImageFit.Contain, paint, 1.0, 0, 0);
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
}

