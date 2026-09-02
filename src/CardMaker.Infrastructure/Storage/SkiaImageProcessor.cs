using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace CardMaker.Infrastructure.Storage;

/// <summary>
/// Ricodifica ogni immagine in PNG con SkiaSharp: e' la difesa che elimina metadati e payload
/// nascosti nel file caricato, perche' viene ridisegnata da zero a partire dai soli pixel.
/// </summary>
public sealed class SkiaImageProcessor(IOptions<UploadLimits> limits) : IImageProcessor
{
    private readonly UploadLimits _limits = limits.Value;

    public NormalizedImage? Normalize(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var codec = SKCodec.Create(new MemoryStream(source, writable: false));
        if (codec is null)
        {
            return null;
        }

        var info = codec.Info;
        if (UploadValidator.ExceedsPixelBudget(info.Width, info.Height, _limits))
        {
            return null;
        }

        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
        {
            return null;
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        if (encoded is null)
        {
            return null;
        }

        return new NormalizedImage(encoded.ToArray(), bitmap.Width, bitmap.Height, "image/png");
    }
}

public sealed class SkiaFontProcessor : IFontProcessor
{
    public FontInfo? Probe(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var data = SKData.CreateCopy(source);
        using var typeface = SKTypeface.FromData(data);
        if (typeface is null)
        {
            return null;
        }

        var style = typeface.FontStyle;
        return new FontInfo(
            typeface.FamilyName ?? "Unknown",
            DescribeStyle(style),
            style.Weight,
            style.Slant != SKFontStyleSlant.Upright);
    }

    private static string DescribeStyle(SKFontStyle style)
    {
        var slant = style.Slant switch
        {
            SKFontStyleSlant.Italic => "Italic",
            SKFontStyleSlant.Oblique => "Oblique",
            _ => "Regular",
        };

        return style.Weight >= 600 ? $"Bold {slant}" : slant;
    }
}
