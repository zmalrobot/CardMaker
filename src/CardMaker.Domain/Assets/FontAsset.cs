using CardMaker.Domain.Common;
using CardMaker.Domain.Games;

namespace CardMaker.Domain.Assets;

/// <summary>
/// Font caricato dall'admin. L'applicazione non distribuisce alcun font proprietario (ADR-010).
/// </summary>
public class FontAsset : Entity
{
    public Guid AssetId { get; set; }

    public Asset Asset { get; set; } = null!;

    public Guid? GameId { get; set; }

    public Game? Game { get; set; }

    /// <summary>Alias usato nei layout, es. "card-name", "effect".</summary>
    public string Alias { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string StyleName { get; set; } = string.Empty;

    public int Weight { get; set; } = 400;

    public bool IsItalic { get; set; }

    public string LicenseNote { get; set; } = string.Empty;
}
