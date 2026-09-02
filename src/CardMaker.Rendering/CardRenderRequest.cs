using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering;

public enum RenderOutputFormat
{
    Png = 0,
    Jpeg = 1,
}

public sealed record RenderWarning(string Code, string Message, string? LayerId = null);

/// <summary>
/// Risorse necessarie al render, fornite dall'host: il motore non conosce database ne' filesystem.
/// </summary>
public interface IRenderResources
{
    SKImage? GetImage(Guid assetId);

    SKImage? GetImageByKey(string assetKey);

    SKImage? GetSymbol(string symbolSetKey, string symbolKey);

    SKTypeface ResolveFont(string? roleAlias, out bool isFallback);
}

public sealed record CardRenderRequest
{
    public required CardLayout Layout { get; init; }

    public required IReadOnlyDictionary<string, CardValue> Values { get; init; }

    public required IRenderResources Resources { get; init; }

    public int Dpi { get; init; } = 600;

    /// <summary>Se falso l'immagine viene ritagliata al trim e gli angoli vengono arrotondati.</summary>
    public bool IncludeBleed { get; init; }

    public bool RoundCorners { get; init; } = true;

    public RenderOutputFormat Format { get; init; } = RenderOutputFormat.Png;

    public int JpegQuality { get; init; } = 92;

    /// <summary>Disegna le guide di trim e safe zone: utile solo nell'editor.</summary>
    public bool ShowGuides { get; init; }
}

public sealed record CardRenderResult(
    byte[] Content,
    string ContentType,
    int WidthPx,
    int HeightPx,
    IReadOnlyList<RenderWarning> Warnings,
    TimeSpan Duration)
{
    public CardGeometry? Geometry { get; init; }

    /// <summary>DPI usato per il render: serve a convertire i pixel in punti nell'export PDF.</summary>
    public int Dpi { get; init; } = 600;
}
