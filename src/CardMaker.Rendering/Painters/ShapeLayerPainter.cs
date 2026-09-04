using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal sealed class ShapeLayerPainter : ILayerPainter
{
    public bool CanPaint(LayerDefinition layer) =>
        layer is ShapeLayer;

    public void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context)
    {
        if (layer is ShapeLayer shape)
        {
            PaintShape(canvas, shape, dest, opacity);
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
                    [RenderDrawingUtilities.ParseColor(layer.GradientFrom, SKColors.Transparent), RenderDrawingUtilities.ParseColor(layer.GradientTo, SKColors.Transparent)],
                    SKShaderTileMode.Clamp);
            }
            else
            {
                fill.Color = RenderDrawingUtilities.ParseColor(layer.FillColor, SKColors.Transparent);
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
                Color = RenderDrawingUtilities.ParseColor(layer.BorderColor, SKColors.Black).WithAlpha(alpha),
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
}

