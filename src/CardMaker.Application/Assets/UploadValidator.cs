namespace CardMaker.Application.Assets;

public enum UploadKind
{
    Image,
    Font,
}

public sealed record UploadLimits
{
    public long MaxImageBytes { get; init; } = 20L * 1024 * 1024;

    public long MaxFontBytes { get; init; } = 8L * 1024 * 1024;

    /// <summary>Guardia contro le decompression bomb: 64 megapixel.</summary>
    public long MaxPixels { get; init; } = 64L * 1000 * 1000;

    public int MaxDimension { get; init; } = 12_000;
}

public sealed record UploadValidationResult(bool IsValid, string? ErrorCode = null, string? DetectedContentType = null)
{
    public static UploadValidationResult Ok(string contentType) => new(true, null, contentType);

    public static UploadValidationResult Fail(string errorCode) => new(false, errorCode);
}

/// <summary>
/// Validazione degli upload basata sui <b>magic bytes</b>, non sull'estensione dichiarata.
/// Logica pura e senza I/O, quindi interamente testabile.
/// </summary>
public static class UploadValidator
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();
    private static readonly byte[] Otto = "OTTO"u8.ToArray();
    private static readonly byte[] TrueType = [0x00, 0x01, 0x00, 0x00];
    private static readonly byte[] TrueTag = "true"u8.ToArray();
    private static readonly byte[] Woff2 = "wOF2"u8.ToArray();

    public static UploadValidationResult Validate(ReadOnlySpan<byte> content, UploadKind kind, UploadLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (content.Length == 0)
        {
            return UploadValidationResult.Fail("upload.empty");
        }

        if (LooksLikeSvgOrMarkup(content))
        {
            // Gli SVG sono un vettore di XSS/XXE: bloccati per policy (ADR non negoziabile).
            return UploadValidationResult.Fail("upload.svgNotAllowed");
        }

        return kind switch
        {
            UploadKind.Image => ValidateImage(content, limits),
            UploadKind.Font => ValidateFont(content, limits),
            _ => UploadValidationResult.Fail("upload.unknownKind"),
        };
    }

    public static bool ExceedsPixelBudget(int width, int height, UploadLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return width <= 0
            || height <= 0
            || width > limits.MaxDimension
            || height > limits.MaxDimension
            || (long)width * height > limits.MaxPixels;
    }

    private static UploadValidationResult ValidateImage(ReadOnlySpan<byte> content, UploadLimits limits)
    {
        if (content.Length > limits.MaxImageBytes)
        {
            return UploadValidationResult.Fail("upload.tooLarge");
        }

        if (content.StartsWith(Png))
        {
            return UploadValidationResult.Ok("image/png");
        }

        if (content.StartsWith(Jpeg))
        {
            return UploadValidationResult.Ok("image/jpeg");
        }

        if (content.Length >= 12 && content.StartsWith(Riff) && content[8..12].SequenceEqual(Webp))
        {
            return UploadValidationResult.Ok("image/webp");
        }

        return UploadValidationResult.Fail("upload.unsupportedImageFormat");
    }

    private static UploadValidationResult ValidateFont(ReadOnlySpan<byte> content, UploadLimits limits)
    {
        if (content.Length > limits.MaxFontBytes)
        {
            return UploadValidationResult.Fail("upload.tooLarge");
        }

        if (content.StartsWith(Otto))
        {
            return UploadValidationResult.Ok("font/otf");
        }

        if (content.StartsWith(TrueType) || content.StartsWith(TrueTag))
        {
            return UploadValidationResult.Ok("font/ttf");
        }

        if (content.StartsWith(Woff2))
        {
            return UploadValidationResult.Ok("font/woff2");
        }

        return UploadValidationResult.Fail("upload.unsupportedFontFormat");
    }

    private static bool LooksLikeSvgOrMarkup(ReadOnlySpan<byte> content)
    {
        var probe = content[..Math.Min(content.Length, 512)];
        Span<char> chars = stackalloc char[probe.Length];
        for (var i = 0; i < probe.Length; i++)
        {
            chars[i] = (char)probe[i];
        }

        var head = new string(chars).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || head.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase)
            || head.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }
}
