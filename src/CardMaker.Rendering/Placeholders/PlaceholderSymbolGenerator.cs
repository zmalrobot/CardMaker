using SkiaSharp;

namespace CardMaker.Rendering.Placeholders;

public sealed class PlaceholderSymbolGenerator
{
    public byte[] Generate(string setKey, string symbolKey, int size = 256)
    {
        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        switch (setKey.ToLowerInvariant())
        {
            case "attributes":
                DrawAttribute(canvas, symbolKey, size);
                break;
            case "stars":
                DrawStar(canvas, symbolKey, size);
                break;
            case "link-arrows":
                DrawLinkArrow(canvas, symbolKey, size);
                break;
            case "spell-properties":
                DrawProperty(canvas, symbolKey, size, new SKColor(0x00, 0x80, 0x80), new SKColor(0x20, 0xB2, 0xAA));
                break;
            case "trap-properties":
                DrawProperty(canvas, symbolKey, size, new SKColor(0xA0, 0x15, 0x60), new SKColor(0xFF, 0x69, 0xB4));
                break;
            default:
                DrawGenericBadge(canvas, symbolKey, size, new SKColor(0x50, 0x50, 0x50));
                break;
        }

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static void DrawAttribute(SKCanvas canvas, string key, int size)
    {
        var (fill, stroke, label) = key.ToLowerInvariant() switch
        {
            "light" => (new SKColor(0xE5, 0xB5, 0x1A), new SKColor(0xFF, 0xF8, 0xDC), "LUCE"),
            "dark" => (new SKColor(0x3B, 0x18, 0x5F), new SKColor(0x93, 0x70, 0xDB), "OSCU"),
            "water" => (new SKColor(0x1B, 0x6C, 0xB8), new SKColor(0x87, 0xCE, 0xFA), "ACQU"),
            "fire" => (new SKColor(0xC8, 0x2A, 0x1E), new SKColor(0xFF, 0x8C, 0x00), "FUOC"),
            "earth" => (new SKColor(0x7D, 0x4D, 0x1E), new SKColor(0xD2, 0xB4, 0x8C), "TERR"),
            "wind" => (new SKColor(0x27, 0x8A, 0x47), new SKColor(0x98, 0xFB, 0x98), "VENT"),
            "divine" => (new SKColor(0xD4, 0x88, 0x15), new SKColor(0xFF, 0xD7, 0x00), "DIVI"),
            _ => (new SKColor(0x60, 0x60, 0x60), new SKColor(0xAA, 0xAA, 0xAA), key[..Math.Min(4, key.Length)].ToUpperInvariant()),
        };

        var radius = (size - 16) / 2f;
        var center = size / 2f;

        using var fillPaint = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(center, center, radius, fillPaint);

        using var strokePaint = new SKPaint
        {
            Color = stroke,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = size * 0.04f,
        };
        canvas.DrawCircle(center, center, radius, strokePaint);

        using var font = new SKFont(SKTypeface.Default, size * 0.30f);
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        var fontMetrics = font.Metrics;
        var textY = center - (fontMetrics.Ascent + fontMetrics.Descent) / 2f;
        canvas.DrawText(label, center, textY, SKTextAlign.Center, font, textPaint);
    }

    private static void DrawProperty(SKCanvas canvas, string key, int size, SKColor fill, SKColor stroke)
    {
        var radius = (size - 16) / 2f;
        var center = size / 2f;

        using var fillPaint = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(center, center, radius, fillPaint);

        using var strokePaint = new SKPaint
        {
            Color = stroke,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = size * 0.04f,
        };
        canvas.DrawCircle(center, center, radius, strokePaint);

        var symbolText = key.ToLowerInvariant() switch
        {
            "quick-play" => "⚡",
            "continuous" => "∞",
            "equip" => "⚔",
            "field" => "✛",
            "ritual" => "🔥",
            "counter" => "↩",
            _ => "●",
        };

        using var font = new SKFont(SKTypeface.Default, size * 0.40f);
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        var fontMetrics = font.Metrics;
        var textY = center - (fontMetrics.Ascent + fontMetrics.Descent) / 2f;
        canvas.DrawText(symbolText, center, textY, SKTextAlign.Center, font, textPaint);
    }

    private static void DrawStar(SKCanvas canvas, string key, int size)
    {
        var isRank = string.Equals(key, "rank", StringComparison.OrdinalIgnoreCase);
        var fillColor = isRank ? new SKColor(0x22, 0x22, 0x22) : new SKColor(0xFF, 0xB8, 0x00);
        var strokeColor = isRank ? new SKColor(0xFF, 0xD7, 0x00) : new SKColor(0xD8, 0x60, 0x00);

        var center = size / 2f;
        var outerRadius = size * 0.45f;
        var innerRadius = outerRadius * 0.42f;

        var builder = new SKPathBuilder();
        for (var i = 0; i < 10; i++)
        {
            var r = (i % 2 == 0) ? outerRadius : innerRadius;
            var angle = (float)(i * Math.PI / 5.0 - Math.PI / 2.0);
            var x = center + r * (float)Math.Cos(angle);
            var y = center + r * (float)Math.Sin(angle);

            if (i == 0)
            {
                builder.MoveTo(x, y);
            }
            else
            {
                builder.LineTo(x, y);
            }
        }
        builder.Close();
        using var path = builder.Detach();

        using var fillPaint = new SKPaint { Color = fillColor, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawPath(path, fillPaint);

        using var strokePaint = new SKPaint
        {
            Color = strokeColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = size * 0.05f,
        };
        canvas.DrawPath(path, strokePaint);
    }

    private static void DrawLinkArrow(SKCanvas canvas, string key, int size)
    {
        var isOn = string.Equals(key, "on", StringComparison.OrdinalIgnoreCase);
        var fillColor = isOn ? new SKColor(0xEE, 0x25, 0x25) : new SKColor(0x40, 0x40, 0x40, 0x90);
        var strokeColor = isOn ? new SKColor(0xFF, 0x88, 0x88) : new SKColor(0x60, 0x60, 0x60, 0x90);

        var center = size / 2f;
        var builder = new SKPathBuilder();
        builder.MoveTo(center, size * 0.12f);
        builder.LineTo(size * 0.82f, size * 0.82f);
        builder.LineTo(center, size * 0.65f);
        builder.LineTo(size * 0.18f, size * 0.82f);
        builder.Close();
        using var path = builder.Detach();

        using var fillPaint = new SKPaint { Color = fillColor, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawPath(path, fillPaint);

        using var strokePaint = new SKPaint
        {
            Color = strokeColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = size * 0.04f,
        };
        canvas.DrawPath(path, strokePaint);
    }

    private static void DrawGenericBadge(SKCanvas canvas, string label, int size, SKColor color)
    {
        var center = size / 2f;
        using var fillPaint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(size * 0.1f, size * 0.1f, size * 0.8f, size * 0.8f, size * 0.15f, size * 0.15f, fillPaint);

        using var font = new SKFont(SKTypeface.Default, size * 0.28f);
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var fontMetrics = font.Metrics;
        var textY = center - (fontMetrics.Ascent + fontMetrics.Descent) / 2f;
        canvas.DrawText(label[..Math.Min(4, label.Length)].ToUpperInvariant(), center, textY, SKTextAlign.Center, font, textPaint);
    }
}
