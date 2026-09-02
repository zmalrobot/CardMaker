using SkiaSharp;

namespace CardMaker.Rendering;

/// <summary>
/// Compone in un unico PDF le pagine gia' renderizzate (fronte, opzionalmente retro).
/// Riusa i PNG prodotti da <see cref="CardRenderer"/>: stesso motore, nessun secondo percorso di
/// disegno (ADR-003 esteso al PDF).
/// </summary>
public sealed class PdfExporter
{
    public byte[] Export(CardRenderResult front, CardRenderResult? back = null)
    {
        ArgumentNullException.ThrowIfNull(front);

        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            DrawPage(document, front);
            if (back is not null)
            {
                DrawPage(document, back);
            }
        }

        return stream.ToArray();
    }

    private static void DrawPage(SKDocument document, CardRenderResult page)
    {
        var widthPt = page.WidthPx * 72f / page.Dpi;
        var heightPt = page.HeightPx * 72f / page.Dpi;

        using var bitmap = SKBitmap.Decode(page.Content);
        using var image = SKImage.FromBitmap(bitmap);

        var canvas = document.BeginPage(widthPt, heightPt);
        canvas.DrawImage(image, new SKRect(0, 0, widthPt, heightPt), new SKSamplingOptions(SKCubicResampler.Mitchell));
        document.EndPage();
    }
}
