using CardMaker.Domain.Common;
using CardMaker.Domain.Games;

namespace CardMaker.Domain.Assets;

public enum AssetCategory
{
    Other = 0,
    Frame = 1,
    Symbol = 2,
    Foil = 3,
    Overlay = 4,
    CardBack = 5,
    Mask = 6,
    Font = 7,
    Artwork = 8,
    Placeholder = 9,
}

/// <summary>
/// Metadati di un file binario. Il contenuto vive sul filesystem, indirizzato dal suo SHA-256.
/// </summary>
public class Asset : Entity
{
    /// <summary>SHA-256 esadecimale minuscolo: e' anche il nome del file su disco.</summary>
    public string Sha256 { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long ByteSize { get; set; }

    public int PixelWidth { get; set; }

    public int PixelHeight { get; set; }

    public AssetCategory Category { get; set; } = AssetCategory.Other;

    public string? Tags { get; set; }

    /// <summary>Obbligatorio per processo: documenta il diritto d'uso dell'asset (ADR-010).</summary>
    public string LicenseNote { get; set; } = string.Empty;

    public string? SourceNote { get; set; }

    public string? UploadedByUserId { get; set; }

    public Guid? GameId { get; set; }

    public Game? Game { get; set; }
}
