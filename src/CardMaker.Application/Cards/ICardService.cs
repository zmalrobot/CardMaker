using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Templates;

namespace CardMaker.Application.Cards;

public sealed record CardSummaryDto(
    Guid Id,
    string Title,
    string GameKey,
    string GameName,
    string CardTypeKey,
    string CardTypeName,
    Guid? ThumbnailAssetId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record TraitOptionDto(
    Guid Id,
    string Key,
    string Name,
    string Group);

public sealed record OptionItemDto(
    string Key,
    string Label);

public sealed record SymbolOptionDto(
    string Key,
    string Name,
    Guid? AssetId,
    string? InlineToken);

public sealed record FieldDefinitionDto(
    Guid Id,
    string Key,
    string Label,
    string HelpText,
    FieldKind Kind,
    bool IsRequired,
    string? DefaultValueJson,
    string? GroupName,
    int SortOrder,
    string? VisibleWhenJson,
    IReadOnlyList<OptionItemDto> Options,
    IReadOnlyList<SymbolOptionDto> Symbols);

public sealed record CardTypeDetailDto(
    Guid Id,
    Guid GameId,
    string Key,
    string Name,
    IReadOnlyList<FieldDefinitionDto> Fields,
    IReadOnlyList<TraitOptionDto> AllowedTraits,
    IReadOnlyList<Template> Templates);

public sealed record CardDetailDto(
    Guid Id,
    string Title,
    Guid GameId,
    string GameKey,
    Guid CardTypeId,
    string CardTypeKey,
    Guid TemplateVersionId,
    Guid? BackTemplateVersionId,
    IReadOnlyDictionary<string, CardValue> Values,
    IReadOnlyList<string> SelectedTraits,
    Guid? ThumbnailAssetId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveCardRequest
{
    public required string Title { get; init; }
    public required Guid GameId { get; init; }
    public required Guid CardTypeId { get; init; }
    public required Guid TemplateVersionId { get; init; }
    public Guid? BackTemplateVersionId { get; init; }
    public required IReadOnlyDictionary<string, CardValue> Values { get; init; }
    public required IReadOnlyList<string> SelectedTraits { get; init; }
    public Guid? ThumbnailAssetId { get; init; }
}

public interface ICardService
{
    Task<IReadOnlyList<CardSummaryDto>> GetUserCardsAsync(string userId, CancellationToken cancellationToken = default);

    Task<CardDetailDto?> GetCardAsync(Guid cardId, string userId, CancellationToken cancellationToken = default);

    Task<CardDetailDto> CreateCardAsync(SaveCardRequest request, string userId, CancellationToken cancellationToken = default);

    Task<CardDetailDto> UpdateCardAsync(Guid cardId, SaveCardRequest request, string userId, CancellationToken cancellationToken = default);

    Task<CardDetailDto> DuplicateCardAsync(Guid cardId, string userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteCardAsync(Guid cardId, string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CardTypeDetailDto>> GetGameCardTypesAsync(string gameKey, CancellationToken cancellationToken = default);
}
