using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal sealed class ImageLayerPainter : ILayerPainter
{
    public bool CanPaint(LayerDefinition layer) =>
        layer is StaticImageLayer or ImageSlotLayer;

    public void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context)
    {
        switch (layer)
        {
            case StaticImageLayer staticImage:
                PaintStaticImage(canvas, staticImage, dest, opacity, context);
                break;
            case ImageSlotLayer imageSlot:
                PaintImageSlot(canvas, imageSlot, dest, opacity, context);
                break;
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

        using var paint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
        RenderDrawingUtilities.DrawImage(canvas, image, dest, layer.Fit, paint, 1.0, 0, 0);
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

        using var paint = RenderDrawingUtilities.CreateImagePaint(opacity, layer.BlendMode);
        RenderDrawingUtilities.DrawImage(canvas, image, dest, layer.Fit, paint, layer.Zoom, layer.OffsetX, layer.OffsetY, ResolveSlice(layer, image));
    }

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
}

