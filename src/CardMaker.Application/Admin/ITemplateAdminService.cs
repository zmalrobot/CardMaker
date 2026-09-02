using System.Text.RegularExpressions;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Templates;

namespace CardMaker.Application.Admin;

public sealed record TemplateSummaryDto(
    Guid Id,
    Guid CardTypeId,
    string Key,
    string Name,
    bool IsDefault,
    string? SelectionRuleJson,
    int SortOrder,
    int VersionsCount,
    int? LatestPublishedVersionNumber,
    int LatestVersionNumber);

public sealed record TemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    string LayoutJson,
    string? ChangeNote,
    bool IsPublished,
    DateTimeOffset? PublishedAtUtc,
    string? CreatedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record TemplateDetailDto(
    Guid Id,
    Guid CardTypeId,
    string CardTypeName,
    string GameKey,
    string Key,
    string Name,
    bool IsDefault,
    string? SelectionRuleJson,
    int SortOrder,
    IReadOnlyList<TemplateVersionDto> Versions,
    TemplateVersionDto? CurrentVersion);

public sealed class SaveTemplateRequest
{
    public Guid? Id { get; set; }
    public required Guid CardTypeId { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public bool IsDefault { get; set; }
    public string? SelectionRuleJson { get; set; }
    public int SortOrder { get; set; }
}

public sealed record LayoutValidationIssue(
    string Severity, // "error" | "warning" | "info"
    string Code,
    string Message,
    string? LayerName);

public sealed record LayoutValidationReport(
    bool IsValid,
    IReadOnlyList<LayoutValidationIssue> Issues);

public interface ITemplateAdminService
{
    Task<IReadOnlyList<TemplateSummaryDto>> GetTemplatesByCardTypeAsync(Guid cardTypeId, CancellationToken cancellationToken = default);
    Task<TemplateDetailDto?> GetTemplateDetailAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<TemplateDetailDto> SaveTemplateAsync(SaveTemplateRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteTemplateAsync(Guid templateId, string? userId, CancellationToken cancellationToken = default);

    Task<TemplateVersionDto> CreateVersionAsync(Guid templateId, string layoutJson, string? changeNote, string? userId, CancellationToken cancellationToken = default);
    Task<TemplateVersionDto> PublishVersionAsync(Guid versionId, string? userId, CancellationToken cancellationToken = default);

    Task<LayoutValidationReport> ValidateLayoutAsync(Guid cardTypeId, CardLayout layout, CancellationToken cancellationToken = default);
}

