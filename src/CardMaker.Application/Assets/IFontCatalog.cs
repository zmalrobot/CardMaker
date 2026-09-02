using CardMaker.Domain.Assets;

namespace CardMaker.Application.Assets;

public sealed record FontRegistrationRequest
{
    public required string FileName { get; init; }

    /// <summary>Alias di ruolo, es. "card-name", "effect-italic", "atk-def-value".</summary>
    public required string Alias { get; init; }

    public required string LicenseNote { get; init; }

    public Guid? GameId { get; init; }

    public string? UploadedByUserId { get; init; }
}

public sealed record FontRegistrationOutcome(bool Succeeded, FontAsset? Font, string? ErrorCode)
{
    public static FontRegistrationOutcome Fail(string errorCode) => new(false, null, errorCode);

    public static FontRegistrationOutcome Ok(FontAsset font) => new(true, font, null);
}

/// <summary>
/// Gestisce i font caricati dall'admin, indicizzati per <b>alias di ruolo</b>.
/// Ogni elemento testuale di una carta ha il proprio ruolo, quindi il proprio font sostituibile.
/// </summary>
public interface IFontCatalog
{
    Task<FontRegistrationOutcome> RegisterAsync(
        Stream content,
        FontRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FontAsset>> ListAsync(Guid? gameId = null, CancellationToken cancellationToken = default);

    Task<FontAsset?> FindByAliasAsync(Guid? gameId, string roleAlias, CancellationToken cancellationToken = default);

    Task<byte[]?> GetBytesAsync(Guid fontAssetId, CancellationToken cancellationToken = default);

    Task<byte[]?> GetBytesByAliasAsync(Guid? gameId, string roleAlias, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(Guid fontAssetId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Alias suggeriti nell'interfaccia admin. Sono solo un aiuto alla compilazione: l'alias e' testo
/// libero e il motore non conosce alcun ruolo predefinito.
/// </summary>
public static class FontRoleSuggestions
{
    public static readonly string[] YuGiOh =
    [
        "card-name",
        "spell-trap-label",
        "type-line",
        "effect",
        "effect-italic",
        "effect-bold",
        "pendulum-effect",
        "pendulum-scale",
        "atk-def-label",
        "atk-def-value",
        "link-rating",
        "set-code",
        "edition",
        "passcode",
        "copyright",
        "rush-card-name",
        "rush-section-label",
        "rush-effect",
        "rush-type-line",
        "rush-maximum-atk",
    ];
}
