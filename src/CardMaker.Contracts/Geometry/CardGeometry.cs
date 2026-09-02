namespace CardMaker.Contracts.Geometry;

/// <summary>
/// Converte il formato fisico di una carta nelle misure in pixel a un dato DPI.
/// Yu-Gi-Oh! a 600 DPI: master 1488x2126, trim 1394x2032 con offset 47,47.
/// </summary>
public sealed record CardGeometry
{
    public const double MillimetersPerInch = 25.4;

    public required double WidthMm { get; init; }

    public required double HeightMm { get; init; }

    public double BleedMm { get; init; } = 2;

    public double CornerRadiusMm { get; init; } = 2;

    public double SafeZoneMm { get; init; } = 3;

    public int Dpi { get; init; } = 600;

    public static CardGeometry YuGiOh(int dpi = 600) => new()
    {
        WidthMm = 59,
        HeightMm = 86,
        BleedMm = 2,
        CornerRadiusMm = 2,
        SafeZoneMm = 3,
        Dpi = dpi,
    };

    public static CardGeometry PokerSize(int dpi = 600) => new()
    {
        WidthMm = 63,
        HeightMm = 88,
        BleedMm = 2,
        CornerRadiusMm = 3.18,
        SafeZoneMm = 3,
        Dpi = dpi,
    };

    public int TrimWidthPx => ToPixels(WidthMm);

    public int TrimHeightPx => ToPixels(HeightMm);

    public int BleedPx => ToPixels(BleedMm);

    public int MasterWidthPx => TrimWidthPx + (2 * BleedPx);

    public int MasterHeightPx => TrimHeightPx + (2 * BleedPx);

    public int CornerRadiusPx => ToPixels(CornerRadiusMm);

    public int SafeZonePx => ToPixels(SafeZoneMm);

    public CardGeometry AtDpi(int dpi) => this with { Dpi = dpi };

    public double ScaleFrom(CardGeometry other) => (double)Dpi / other.Dpi;

    /// <summary>Converte un rettangolo normalizzato (relativo al trim) in pixel sul master canvas.</summary>
    public (float X, float Y, float Width, float Height) ToMasterPixels(NormalizedRect rect) => (
        BleedPx + (float)(rect.X * TrimWidthPx),
        BleedPx + (float)(rect.Y * TrimHeightPx),
        (float)(rect.Width * TrimWidthPx),
        (float)(rect.Height * TrimHeightPx));

    public int ToPixels(double millimeters) =>
        (int)Math.Round(millimeters / MillimetersPerInch * Dpi, MidpointRounding.AwayFromZero);

    public float PointsToPixels(double points) => (float)(points / 72.0 * Dpi);
}
