using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal sealed class ContainerLayerPainter : ILayerPainter
{
    public bool CanPaint(LayerDefinition layer) =>
        layer is ToggleGroupLayer;

    public void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context)
    {
        if (layer is ToggleGroupLayer toggle)
        {
            PaintToggleGroup(canvas, toggle, dest, opacity, context);
        }
    }

    private static void PaintToggleGroup(
        SKCanvas canvas, ToggleGroupLayer layer, SKRect dest, double opacity, PaintContext context)
    {
        var active = context.Binder.Get(layer.FieldKey)?.AsList() ?? [];
        var activeSet = new HashSet<string>(active, StringComparer.Ordinal);

        using var paint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
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
            RenderDrawingUtilities.DrawImage(canvas, image, itemRect, ImageFit.Contain, paint, 1.0, 0, 0);
        }
    }
}

