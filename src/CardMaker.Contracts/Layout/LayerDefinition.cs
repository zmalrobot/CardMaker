using System.Text.Json.Serialization;
using CardMaker.Contracts.Geometry;

namespace CardMaker.Contracts.Layout;

public enum ImageFit
{
    Cover = 0,
    Contain = 1,
    Stretch = 2,
}

public enum LayerAnchor
{
    TopLeft = 0,
    Top = 1,
    TopRight = 2,
    Left = 3,
    Center = 4,
    Right = 5,
    BottomLeft = 6,
    Bottom = 7,
    BottomRight = 8,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StaticImageLayer), LayerTypes.StaticImage)]
[JsonDerivedType(typeof(ImageSlotLayer), LayerTypes.ImageSlot)]
[JsonDerivedType(typeof(TextLayer), LayerTypes.Text)]
[JsonDerivedType(typeof(SymbolSlotLayer), LayerTypes.SymbolSlot)]
[JsonDerivedType(typeof(ShapeLayer), LayerTypes.Shape)]
[JsonDerivedType(typeof(GroupLayer), LayerTypes.Group)]
[JsonDerivedType(typeof(SymbolRepeaterLayer), LayerTypes.SymbolRepeater)]
[JsonDerivedType(typeof(ToggleGroupLayer), LayerTypes.ToggleGroup)]
[JsonDerivedType(typeof(RichTextLayer), LayerTypes.RichText)]
[JsonDerivedType(typeof(OverlayLayer), LayerTypes.Overlay)]
public abstract record LayerDefinition
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString("N");

    public string Name { get; init; } = string.Empty;

    public int Z { get; init; }

    /// <summary>Coordinate normalizzate 0..1 rispetto al trim della carta (ADR-008).</summary>
    public NormalizedRect Rect { get; init; }

    /// <summary>Se true, il layer si estende sull'intero Master Canvas inclusa l'abbondanza (frame, retro, overlay a piena pagina).</summary>
    public bool FullBleed { get; init; }

    public LayerAnchor Anchor { get; init; } = LayerAnchor.TopLeft;

    public double RotationDeg { get; init; }

    public double Opacity { get; init; } = 1.0;

    public string? BlendMode { get; init; }

    public Condition? VisibleWhen { get; init; }

    public bool Locked { get; init; }
}

public static class LayerTypes
{
    public const string StaticImage = "staticImage";
    public const string ImageSlot = "imageSlot";
    public const string Text = "text";
    public const string SymbolSlot = "symbolSlot";
    public const string Shape = "shape";
    public const string Group = "group";
    public const string SymbolRepeater = "symbolRepeater";
    public const string ToggleGroup = "toggleGroup";
    public const string RichText = "richText";
    public const string Overlay = "overlay";
}

/// <summary>Asset fisso: frame, bordo, ologramma, overlay.</summary>
public sealed record StaticImageLayer : LayerDefinition
{
    public Guid? AssetId { get; init; }

    /// <summary>In alternativa a <see cref="AssetId"/>: chiave logica risolta dall'host.</summary>
    public string? AssetKey { get; init; }

    public ImageFit Fit { get; init; } = ImageFit.Stretch;
}

/// <summary>Immagine caricata dall'utente, con inquadratura regolabile.</summary>
public sealed record ImageSlotLayer : LayerDefinition
{
    public required string FieldKey { get; init; }

    public ImageFit Fit { get; init; } = ImageFit.Cover;

    /// <summary>Zoom applicato dall'utente: 1 = adatta alla finestra.</summary>
    public double Zoom { get; init; } = 1.0;

    /// <summary>Scostamento del ritaglio, in frazioni della finestra (-1..1).</summary>
    public double OffsetX { get; init; }

    public double OffsetY { get; init; }

    public Guid? PlaceholderAssetId { get; init; }

    /// <summary>Risoluzione minima consigliata: sotto questa soglia il render emette un avviso.</summary>
    public int MinSourceWidth { get; init; }

    public int MinSourceHeight { get; init; }

    /// <summary>Numero di fette in cui l'immagine sorgente e' divisa (Maximum Monster Rush). 1 = nessun crop.</summary>
    public int SliceCount { get; init; } = 1;

    /// <summary>Indice della fetta da mostrare, 0-based.</summary>
    public int SliceIndex { get; init; }

    public SliceAxis SliceAxis { get; init; } = SliceAxis.Horizontal;
}

public enum SliceAxis
{
    /// <summary>Le fette sono colonne verticali affiancate (sinistra/centro/destra).</summary>
    Horizontal = 0,

    /// <summary>Le fette sono bande orizzontali impilate.</summary>
    Vertical = 1,
}

public sealed record TextLayer : LayerDefinition
{
    /// <summary>Testo letterale o binding, es. "{{name}}" oppure "ATK/{{atk}}".</summary>
    public required string Source { get; init; }

    /// <summary>Nome di uno stile definito in <see cref="CardLayout.TextStyles"/>.</summary>
    public string? Style { get; init; }

    public TextStyleOverrides? StyleOverrides { get; init; }

    /// <summary>Se il testo risolto e' vuoto il layer viene saltato senza avvisi.</summary>
    public bool HideWhenEmpty { get; init; } = true;
}

public sealed record SymbolSlotLayer : LayerDefinition
{
    public required string SymbolSetKey { get; init; }

    /// <summary>Chiave fissa del simbolo; ignorata se e' valorizzato <see cref="FieldKey"/>.</summary>
    public string? SymbolKey { get; init; }

    /// <summary>Campo da cui leggere la chiave del simbolo, es. l'attributo scelto dall'utente.</summary>
    public string? FieldKey { get; init; }

    public ImageFit Fit { get; init; } = ImageFit.Contain;
}

public enum ShapeKind
{
    Rectangle = 0,
    RoundedRect = 1,
    Ellipse = 2,
}

public sealed record ShapeLayer : LayerDefinition
{
    public ShapeKind Shape { get; init; } = ShapeKind.Rectangle;

    public double CornerRadius { get; init; }

    public string? FillColor { get; init; }

    public string? GradientFrom { get; init; }

    public string? GradientTo { get; init; }

    public double GradientAngleDeg { get; init; } = 90;

    public string? BorderColor { get; init; }

    public double BorderWidthMm { get; init; }
}

/// <summary>Contenitore: sposta, nasconde o rende trasparenti piu' layer insieme.</summary>
public sealed record GroupLayer : LayerDefinition
{
    public IReadOnlyList<LayerDefinition> Children { get; init; } = [];
}

public enum RepeaterDirection
{
    /// <summary>Riempie da sinistra: usato per il Rank dei Rush/Xyz.</summary>
    LeftToRight = 0,

    /// <summary>Riempie da destra: usato per il Livello dei mostri normali.</summary>
    RightToLeft = 1,
}

/// <summary>Ripete un simbolo N volte su una griglia a passo fisso (stelle Livello/Rank).</summary>
public sealed record SymbolRepeaterLayer : LayerDefinition
{
    public required string SymbolSetKey { get; init; }

    public required string SymbolKey { get; init; }

    /// <summary>Campo numerico da cui leggere il conteggio; ignorato se <see cref="Count"/> e' valorizzato.</summary>
    public string? FieldKey { get; init; }

    /// <summary>Conteggio letterale, usato quando non c'e' <see cref="FieldKey"/>.</summary>
    public int Count { get; init; }

    /// <summary>Numero di posizioni della griglia, sempre le stesse a prescindere dal conteggio (max 12).</summary>
    public int MaxCount { get; init; } = 12;

    public RepeaterDirection Direction { get; init; } = RepeaterDirection.LeftToRight;

    /// <summary>Spazio fra le celle, come frazione della larghezza della cella.</summary>
    public double GapFraction { get; init; } = 0.1;
}

/// <summary>Una delle posizioni di un <see cref="ToggleGroupLayer"/>, con rettangolo relativo al gruppo.</summary>
public sealed record ToggleItem
{
    /// <summary>Chiave che deve comparire nella lista del campo per accendere questa posizione.</summary>
    public required string Key { get; init; }

    /// <summary>Coordinate normalizzate 0..1 relative al rettangolo del layer, non alla carta.</summary>
    public NormalizedRect Rect { get; init; }
}

/// <summary>Gruppo di posizioni on/off in punti fissi (frecce Link).</summary>
public sealed record ToggleGroupLayer : LayerDefinition
{
    public required string SymbolSetKey { get; init; }

    /// <summary>Campo lista con le chiavi delle posizioni accese.</summary>
    public required string FieldKey { get; init; }

    public required string OnSymbolKey { get; init; }

    /// <summary>Se assente, le posizioni spente non vengono disegnate.</summary>
    public string? OffSymbolKey { get; init; }

    public IReadOnlyList<ToggleItem> Items { get; init; } = [];
}

/// <summary>
/// Testo con formattazione mista: grassetto/corsivo, simboli inline allineati al rigo,
/// etichette di sezione ("[EFFETTO]") e punti elenco. Nessun auto-fit: dimensione fissa
/// con eventuale avviso di overflow (ADR-023).
/// </summary>
public sealed record RichTextLayer : LayerDefinition
{
    /// <summary>Testo con markup: "{{binding}}", **grassetto**, *corsivo*, {sym:set.chiave}, righe "[LABEL] ..." o "- ...".</summary>
    public required string Source { get; init; }

    public string? Style { get; init; }

    public TextStyleOverrides? StyleOverrides { get; init; }

    /// <summary>Alias di ruolo per i run in grassetto; se assente si usa lo stesso font del corpo.</summary>
    public string? BoldFont { get; init; }

    public string? ItalicFont { get; init; }

    public string? BoldItalicFont { get; init; }

    /// <summary>Set di simboli usato dai token "{sym:chiave}" senza prefisso esplicito.</summary>
    public string? DefaultSymbolSetKey { get; init; }

    /// <summary>Colore delle etichette di sezione "[LABEL]"; se assente usa il colore dello stile.</summary>
    public string? SectionLabelColor { get; init; }

    public bool HideWhenEmpty { get; init; } = true;
}

/// <summary>Immagine con blend mode ed eventuale maschera: base per foil e rarita' (ADR-023).</summary>
public sealed record OverlayLayer : LayerDefinition
{
    public Guid? AssetId { get; init; }

    public string? AssetKey { get; init; }

    public Guid? MaskAssetId { get; init; }

    public string? MaskAssetKey { get; init; }

    public ImageFit Fit { get; init; } = ImageFit.Stretch;
}

