using CardMaker.Contracts.Layout;

namespace CardMaker.Application.Rendering;

public sealed record PreviewWarning(string Code, string Message, string? LayerId);

public sealed record CardPreviewRequest
{
    public required string LayoutJson { get; init; }

    public required IReadOnlyDictionary<string, CardValue> Values { get; init; }

    public Guid? GameId { get; init; }

    public int Dpi { get; init; } = 150;

    public bool IncludeBleed { get; init; }

    public bool RoundCorners { get; init; } = true;

    public bool ShowGuides { get; init; }

    /// <summary>"png" oppure "jpg".</summary>
    public string Format { get; init; } = "png";
}

public sealed record CardPreviewResult(
    bool Succeeded,
    byte[]? Content,
    string? ContentType,
    int WidthPx,
    int HeightPx,
    IReadOnlyList<PreviewWarning> Warnings,
    double ElapsedMs,
    IReadOnlyList<string> Errors)
{
    public static CardPreviewResult Fail(IReadOnlyList<string> errors) =>
        new(false, null, null, 0, 0, [], 0, errors);
}

/// <summary>
/// Porta verso il motore di rendering. Non espone tipi SkiaSharp, cosi' la UI puo' usarla senza
/// dipendere ne' da Rendering ne' da Infrastructure.
/// </summary>
public interface ICardPreviewService
{
    Task<CardPreviewResult> RenderAsync(CardPreviewRequest request, CancellationToken cancellationToken = default);
}
