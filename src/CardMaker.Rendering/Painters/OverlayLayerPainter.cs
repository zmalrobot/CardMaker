using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal sealed class OverlayLayerPainter : ILayerPainter
{
    public bool CanPaint(LayerDefinition layer) =>
        layer is OverlayLayer;

    public void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context)
    {
        if (layer is OverlayLayer overlay)
        {
            PaintOverlay(canvas, overlay, dest, opacity, context);
        }
    }

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

        using var layerPaint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
        var restore = canvas.SaveLayer(layerPaint);
        try
        {
            using var normalPaint = new SKPaint { IsAntialias = true };
            RenderDrawingUtilities.DrawImage(canvas, image, dest, layer.Fit, normalPaint, 1.0, 0, 0);

            if (mask is not null)
            {
                using var maskPaint = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.DstIn };
                RenderDrawingUtilities.DrawImage(canvas, mask, dest, ImageFit.Stretch, maskPaint, 1.0, 0, 0);
            }
        }
        finally
        {
            canvas.RestoreToCount(restore);
        }
    }
}

