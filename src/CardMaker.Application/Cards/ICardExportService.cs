using CardMaker.Domain.Cards;
using CardMaker.Domain.Templates;

namespace CardMaker.Application.Cards;

public sealed record CardExportOptions
{
    public RenderFormat Format { get; init; } = RenderFormat.Png;
    public int Dpi { get; init; } = 600;
    public bool IncludeBleed { get; init; }
    public bool RoundCorners { get; init; } = true;
    public CardFace Face { get; init; } = CardFace.Front;
    public bool BothFaces { get; init; }
}

public sealed record CardExportResult(
    bool Succeeded,
    byte[]? Content,
    string? ContentType,
    string? FileName,
    string? ErrorMessage);

public interface ICardExportService
{
    Task<CardExportResult> ExportCardAsync(
        Guid cardId,
        string userId,
        CardExportOptions options,
        CancellationToken cancellationToken = default);
}
