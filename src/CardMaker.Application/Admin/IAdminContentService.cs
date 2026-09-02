using CardMaker.Domain.Cards;

namespace CardMaker.Application.Admin;

public sealed record AdminGameDto(
    Guid Id,
    string Key,
    string NameIt,
    string NameEn,
    string DescriptionIt,
    string DescriptionEn,
    decimal WidthMm,
    decimal HeightMm,
    decimal BleedMm,
    decimal SafeZoneMm,
    decimal CornerRadiusMm,
    int DefaultDpi,
    bool IsPublished,
    int SortOrder,
    int CardTypesCount,
    int SymbolSetsCount,
    int OptionListsCount,
    int TraitsCount);

public sealed class SaveGameRequest
{
    public Guid? Id { get; set; }
    public required string Key { get; set; }
    public required string NameIt { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionIt { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public decimal WidthMm { get; set; } = 59m;
    public decimal HeightMm { get; set; } = 86m;
    public decimal BleedMm { get; set; } = 2m;
    public decimal SafeZoneMm { get; set; } = 3m;
    public decimal CornerRadiusMm { get; set; } = 2m;
    public int DefaultDpi { get; set; } = 600;
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed record AdminCardTypeDto(
    Guid Id,
    Guid GameId,
    string Key,
    string NameIt,
    string NameEn,
    int SortOrder,
    int FieldsCount,
    int TemplatesCount,
    int AllowedTraitsCount);

public sealed record AdminFieldDto(
    Guid Id,
    Guid CardTypeId,
    string Key,
    string LabelIt,
    string LabelEn,
    string HelpTextIt,
    string HelpTextEn,
    FieldKind Kind,
    bool IsRequired,
    string? DefaultValueJson,
    Guid? OptionListId,
    Guid? SymbolSetId,
    string? ValidationJson,
    string? ComputedExprJson,
    string? VisibleWhenJson,
    string? GroupName,
    int SortOrder);

public sealed class SaveFieldDefinitionRequest
{
    public Guid? Id { get; set; }
    public required string Key { get; set; }
    public required string LabelIt { get; set; }
    public string LabelEn { get; set; } = string.Empty;
    public string HelpTextIt { get; set; } = string.Empty;
    public string HelpTextEn { get; set; } = string.Empty;
    public FieldKind Kind { get; set; } = FieldKind.Text;
    public bool IsRequired { get; set; }
    public string? DefaultValueJson { get; set; }
    public Guid? OptionListId { get; set; }
    public Guid? SymbolSetId { get; set; }
    public string? ValidationJson { get; set; }
    public string? ComputedExprJson { get; set; }
    public string? VisibleWhenJson { get; set; }
    public string? GroupName { get; set; }
    public int SortOrder { get; set; }
}

public sealed record AdminCardTypeDetailDto(
    Guid Id,
    Guid GameId,
    string Key,
    string NameIt,
    string NameEn,
    int SortOrder,
    IReadOnlyList<AdminFieldDto> Fields,
    IReadOnlyList<Guid> AllowedTraitIds);

public sealed class SaveCardTypeRequest
{
    public Guid? Id { get; set; }
    public required Guid GameId { get; set; }
    public required string Key { get; set; }
    public required string NameIt { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public IReadOnlyList<Guid> AllowedTraitIds { get; set; } = [];
}

public sealed record AdminTraitDto(
    Guid Id,
    Guid GameId,
    string Key,
    string NameIt,
    string NameEn,
    string Group,
    int SortOrder);

public sealed class SaveTraitRequest
{
    public Guid? Id { get; set; }
    public required Guid GameId { get; set; }
    public required string Key { get; set; }
    public required string NameIt { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed record AdminSymbolDto(
    Guid Id,
    Guid SymbolSetId,
    string Key,
    string NameIt,
    string NameEn,
    Guid? AssetId,
    string? InlineToken,
    int SortOrder);

public sealed class SaveSymbolRequest
{
    public Guid? Id { get; set; }
    public required string Key { get; set; }
    public required string NameIt { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public Guid? AssetId { get; set; }
    public string? InlineToken { get; set; }
    public int SortOrder { get; set; }
}

public sealed record AdminSymbolSetDto(
    Guid Id,
    Guid GameId,
    string Key,
    string NameIt,
    string NameEn,
    IReadOnlyList<AdminSymbolDto> Symbols);

public sealed class SaveSymbolSetRequest
{
    public Guid? Id { get; set; }
    public required Guid GameId { get; set; }
    public required string Key { get; set; }
    public required string NameIt { get; set; }
    public string NameEn { get; set; } = string.Empty;
}

public sealed record AdminOptionItemDto(
    Guid Id,
    Guid OptionListId,
    string Key,
    string LabelIt,
    string LabelEn,
    int SortOrder,
    string? MetadataJson,
    bool IsActive,
    Guid? SymbolId);

public sealed class SaveOptionItemRequest
{
    public Guid? Id { get; set; }
    public required string Key { get; set; }
    public required string LabelIt { get; set; }
    public string LabelEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? SymbolId { get; set; }
}

public sealed record AdminOptionListDto(
    Guid Id,
    Guid GameId,
    string Key,
    string NameIt,
    string NameEn,
    IReadOnlyList<AdminOptionItemDto> Items);

public sealed class SaveOptionListRequest
{
    public Guid? Id { get; set; }
    public required Guid GameId { get; set; }
    public required string Key { get; set; }
    public required string NameIt { get; set; }
    public string NameEn { get; set; } = string.Empty;
}

public sealed record AssetUsageCheckResult(
    bool IsInUse,
    IReadOnlyList<string> UsageReasons);

public sealed record AssetReplaceResult(
    bool Succeeded,
    string? NewSha256,
    string? ErrorMessage);

public sealed record AuditLogEntryDto(
    Guid Id,
    string? UserId,
    string Action,
    string EntityName,
    string? EntityId,
    string? DetailsJson,
    string? IpAddress,
    DateTimeOffset CreatedAtUtc);

public interface IAdminContentService
{
    // Games
    Task<IReadOnlyList<AdminGameDto>> GetGamesAsync(CancellationToken cancellationToken = default);
    Task<AdminGameDto?> GetGameByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminGameDto> SaveGameAsync(SaveGameRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteGameAsync(Guid id, string? userId, CancellationToken cancellationToken = default);

    // CardTypes
    Task<IReadOnlyList<AdminCardTypeDto>> GetCardTypesAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<AdminCardTypeDetailDto?> GetCardTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminCardTypeDetailDto> SaveCardTypeAsync(SaveCardTypeRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteCardTypeAsync(Guid id, string? userId, CancellationToken cancellationToken = default);

    // FieldDefinitions (Schema Editor)
    Task<AdminFieldDto> SaveFieldDefinitionAsync(Guid cardTypeId, SaveFieldDefinitionRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFieldDefinitionAsync(Guid fieldId, string? userId, CancellationToken cancellationToken = default);
    Task ReorderFieldsAsync(Guid cardTypeId, IReadOnlyList<Guid> orderedFieldIds, string? userId, CancellationToken cancellationToken = default);

    // Traits
    Task<IReadOnlyList<AdminTraitDto>> GetTraitsAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<AdminTraitDto> SaveTraitAsync(SaveTraitRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteTraitAsync(Guid id, string? userId, CancellationToken cancellationToken = default);

    // Symbols
    Task<IReadOnlyList<AdminSymbolSetDto>> GetSymbolSetsAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<AdminSymbolSetDto> SaveSymbolSetAsync(SaveSymbolSetRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSymbolSetAsync(Guid id, string? userId, CancellationToken cancellationToken = default);
    Task<AdminSymbolDto> SaveSymbolAsync(Guid symbolSetId, SaveSymbolRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSymbolAsync(Guid symbolId, string? userId, CancellationToken cancellationToken = default);

    // OptionLists
    Task<IReadOnlyList<AdminOptionListDto>> GetOptionListsAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<AdminOptionListDto> SaveOptionListAsync(SaveOptionListRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteOptionListAsync(Guid id, string? userId, CancellationToken cancellationToken = default);
    Task<AdminOptionItemDto> SaveOptionItemAsync(Guid optionListId, SaveOptionItemRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteOptionItemAsync(Guid optionItemId, string? userId, CancellationToken cancellationToken = default);

    // Safe Asset Operations
    Task<AssetUsageCheckResult> CheckAssetUsageAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<bool> SafeDeleteAssetAsync(Guid assetId, string? userId, CancellationToken cancellationToken = default);
    Task<AssetReplaceResult> ReplaceAssetBlobAsync(Guid assetId, Stream newContent, string fileName, string? userId, CancellationToken cancellationToken = default);

    // Audit Logs
    Task<IReadOnlyList<AuditLogEntryDto>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default);
}

