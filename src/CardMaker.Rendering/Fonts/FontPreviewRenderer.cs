using SkiaSharp;

namespace CardMaker.Rendering.Fonts;

/// <summary>
/// Disegna un campione di testo con un font caricato. Serve all'admin per verificare, prima di
/// usarlo in un template, che il file sia valido e copra gli accenti italiani e le cifre.
/// </summary>
public sealed class FontPreviewRenderer
{
    public const string DefaultSample = "Drago Bianco Occhi Blu \u2014 ATK/3000 DEF/2500 \u2014 \u00e0\u00e8\u00e9\u00ec\u00f2\u00f9";

    public byte[] Render(byte[] fontBytes, string? sample = null, int width = 900, float sizePx = 46f)
    {
        using var typeface = FontRegistry.FromBytes(fontBytes) ?? FontRegistry.Fallback;
        return Render(typeface, sample, width, sizePx);
    }

    public byte[] Render(SKTypeface typeface, string? sample = null, int width = 900, float sizePx = 46f)
    {
        ArgumentNullException.ThrowIfNull(typeface);
        var text = string.IsNullOrWhiteSpace(sample) ? DefaultSample : sample;

        using var font = new SKFont(typeface, sizePx);
        using var paint = new SKPaint { Color = new SKColor(0x20, 0x20, 0x20), IsAntialias = true };

        var metrics = font.Metrics;
        var lineHeight = metrics.Descent - metrics.Ascent;
        var height = (int)Math.Ceiling(lineHeight * 2.4f);

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var baseline = -metrics.Ascent + (lineHeight * 0.2f);
        canvas.DrawText(text, 16, baseline, SKTextAlign.Left, font, paint);

        // Seconda riga compressa: mostra come reagisce il font allo scaling orizzontale (stile Yu-Gi-Oh!).
        using var condensed = new SKFont(typeface, sizePx) { ScaleX = 0.6f };
        canvas.DrawText(text, 16, baseline + lineHeight * 1.15f, SKTextAlign.Left, condensed, paint);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
        return encoded.ToArray();
    }
}
