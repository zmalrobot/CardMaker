using CardMaker.Contracts.Geometry;
using SkiaSharp;

namespace CardMaker.Rendering;

internal static class RenderPostProcessor
{
    /// <summary>Fase POST: ritaglio al trim, angoli arrotondati, appiattimento per il JPEG.</summary>
    public static SKImage ApplyPostProcessing(SKImage source, CardGeometry geometry, CardRenderRequest request)
    {
        if (request.IncludeBleed)
        {
            // Con l'abbondanza gli angoli non vanno arrotondati: quell'area serve proprio a essere tagliata.
            return source.ToRasterImage() ?? source;
        }

        var trim = new SKRectI(
            geometry.BleedPx,
            geometry.BleedPx,
            geometry.BleedPx + geometry.TrimWidthPx,
            geometry.BleedPx + geometry.TrimHeightPx);

        if (!request.RoundCorners || geometry.CornerRadiusPx <= 0)
        {
            return source.Subset(trim) ?? source.ToRasterImage() ?? source;
        }

        using var cropped = source.Subset(trim) ?? source;

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

    public static byte[] Encode(SKImage image, CardRenderRequest request)
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

    public static void DrawGuides(SKCanvas canvas, CardGeometry geometry)
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
}

