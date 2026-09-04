using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering;

internal static class RenderDrawingUtilities
{
    public static SKColor ParseColor(string? value, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return SKColor.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public static SKBlendMode ParseBlendMode(string? value) => value?.ToLowerInvariant() switch
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

    public static SKPaint CreateImagePaint(double opacity, string? blendMode) => new()
    {
        Color = SKColors.White.WithAlpha((byte)Math.Clamp(opacity * 255, 0, 255)),
        IsAntialias = true,
        BlendMode = ParseBlendMode(blendMode),
    };

    public static void DrawImage(
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

    public static SKRect ResolveRect(LayerDefinition layer, CardGeometry geometry)
    {
        if (layer.FullBleed ||
            (layer.Rect.X == 0 && layer.Rect.Y == 0 && layer.Rect.Width == 1 && layer.Rect.Height == 1 &&
             (layer.Id is "frame" or "back" ||
              string.Equals(layer.Name, "Frame", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(layer.Name, "Retro", StringComparison.OrdinalIgnoreCase))))
        {
            return new SKRect(0, 0, geometry.MasterWidthPx, geometry.MasterHeightPx);
        }

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
}

