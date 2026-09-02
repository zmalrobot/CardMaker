using CardMaker.Contracts.Geometry;

namespace CardMaker.Contracts.Layout;

public sealed record CanvasDefinition
{
    public double WidthMm { get; init; } = 59;

    public double HeightMm { get; init; } = 86;

    public double CornerRadiusMm { get; init; } = 2;

    public double BleedMm { get; init; } = 2;

    public double SafeZoneMm { get; init; } = 3;

    public string? Background { get; init; }

    public CardGeometry ToGeometry(int dpi) => new()
    {
        WidthMm = WidthMm,
        HeightMm = HeightMm,
        BleedMm = BleedMm,
        CornerRadiusMm = CornerRadiusMm,
        SafeZoneMm = SafeZoneMm,
        Dpi = dpi,
    };

    public static CanvasDefinition FromGeometry(CardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        return new CanvasDefinition
        {
            WidthMm = geometry.WidthMm,
            HeightMm = geometry.HeightMm,
            BleedMm = geometry.BleedMm,
            CornerRadiusMm = geometry.CornerRadiusMm,
            SafeZoneMm = geometry.SafeZoneMm,
        };
    }
}

/// <summary>
/// Documento di layout: descrive per intero l'aspetto di una faccia di carta.
/// E' <b>dato</b>, non codice: e' cio' che rende il motore indipendente dal gioco (ADR-001).
/// </summary>
public sealed record CardLayout
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public CanvasDefinition Canvas { get; init; } = new();

    /// <summary>Stili tipografici riutilizzabili, referenziati per nome dai layer di testo.</summary>
    public IReadOnlyDictionary<string, TextStyle> TextStyles { get; init; } =
        new Dictionary<string, TextStyle>(StringComparer.Ordinal);

    public IReadOnlyList<ComputedField> Computed { get; init; } = [];

    public IReadOnlyList<LayerDefinition> Layers { get; init; } = [];

    public IEnumerable<LayerDefinition> EnumerateLayers()
    {
        foreach (var layer in Layers)
        {
            foreach (var descendant in Flatten(layer))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<LayerDefinition> Flatten(LayerDefinition layer)
    {
        yield return layer;
        if (layer is GroupLayer group)
        {
            foreach (var child in group.Children)
            {
                foreach (var descendant in Flatten(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
