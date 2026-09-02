using CardMaker.Domain.Assets;

namespace CardMaker.Application.Assets;

public sealed record AssetUploadRequest
{
    public required string FileName { get; init; }

    public required AssetCategory Category { get; init; }

    public required string LicenseNote { get; init; }

    public string? SourceNote { get; init; }

    public Guid? GameId { get; init; }

    public string? UploadedByUserId { get; init; }

    public UploadKind Kind { get; init; } = UploadKind.Image;
}

public sealed record AssetUploadOutcome(bool Succeeded, Asset? Asset, string? ErrorCode)
{
    public static AssetUploadOutcome Fail(string errorCode) => new(false, null, errorCode);

    public static AssetUploadOutcome Ok(Asset asset) => new(true, asset, null);
}

/// <summary>
/// Porta verso la gestione degli asset. La UI dipende da questa interfaccia, non da Infrastructure.
/// </summary>
public interface IAssetCatalog
{
    Task<AssetUploadOutcome> UploadAsync(Stream content, AssetUploadRequest request, CancellationToken cancellationToken = default);

    Task<Asset?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> ListAsync(Guid? gameId = null, int take = 100, CancellationToken cancellationToken = default);

    Task<Stream?> OpenContentAsync(string sha256, CancellationToken cancellationToken = default);
}
