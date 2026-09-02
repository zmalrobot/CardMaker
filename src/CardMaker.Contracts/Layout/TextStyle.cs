namespace CardMaker.Contracts.Layout;

public enum TextAlign
{
    Left = 0,
    Center = 1,
    Right = 2,
    Justify = 3,
}

public enum VerticalAlign
{
    Top = 0,
    Middle = 1,
    Bottom = 2,
}

public enum TextTransform
{
    None = 0,
    Upper = 1,
    Lower = 2,
    Title = 3,
}

public enum AutoFitMode
{
    /// <summary>Nessun adattamento: il testo puo' debordare e viene segnalato.</summary>
    None = 0,

    /// <summary>Riduce il corpo del font.</summary>
    Shrink = 1,

    /// <summary>Comprime orizzontalmente mantenendo il corpo: e' il comportamento Yu-Gi-Oh!.</summary>
    Condense = 2,

    /// <summary>Prima comprime, poi riduce il corpo se non basta.</summary>
    ShrinkAndCondense = 3,
}

public sealed record AutoFitSettings
{
    public AutoFitMode Mode { get; init; } = AutoFitMode.ShrinkAndCondense;

    public double MinSizePt { get; init; } = 6;

    /// <summary>Compressione orizzontale minima: 0.6 = 60% della larghezza naturale.</summary>
    public double MinScaleX { get; init; } = 0.7;

    public double MinLineHeight { get; init; } = 0.9;

    public static readonly AutoFitSettings None = new() { Mode = AutoFitMode.None };
}

public sealed record StrokeStyle
{
    public required string Color { get; init; }

    public double WidthPt { get; init; } = 1;
}

public sealed record ShadowStyle
{
    public required string Color { get; init; }

    public double OffsetXPt { get; init; }

    public double OffsetYPt { get; init; }

    public double BlurPt { get; init; }
}

/// <summary>
/// Stile tipografico completo di un testo. Ogni elemento della carta ne ha uno proprio:
/// il <see cref="Font"/> e' l'<b>alias di ruolo</b> risolto sui font caricati dall'admin.
/// </summary>
public sealed record TextStyle
{
    /// <summary>Alias di ruolo, es. "card-name", "effect-italic", "atk-def-value".</summary>
    public string? Font { get; init; }

    public double SizePt { get; init; } = 12;

    public string Color { get; init; } = "#000000";

    public TextAlign Align { get; init; } = TextAlign.Left;

    public VerticalAlign VerticalAlign { get; init; } = VerticalAlign.Top;

    public double LineHeight { get; init; } = 1.15;

    public double LetterSpacingPt { get; init; }

    /// <summary>Scala orizzontale iniziale; l'auto-fit puo' ridurla fino a MinScaleX.</summary>
    public double ScaleX { get; init; } = 1.0;

    public TextTransform Transform { get; init; } = TextTransform.None;

    public int MaxLines { get; init; } = 64;

    /// <summary>Margine interno laterale. Sulle carte serve spesso solo questo, non quello verticale.</summary>
    public double PaddingXPt { get; init; }

    public double PaddingYPt { get; init; }

    public AutoFitSettings AutoFit { get; init; } = new();

    public StrokeStyle? Stroke { get; init; }

    public ShadowStyle? Shadow { get; init; }

    /// <summary>Applica solo le proprieta' valorizzate in <paramref name="overrides"/>.</summary>
    public TextStyle Merge(TextStyleOverrides? overrides) => overrides is null
        ? this
        : this with
        {
            Font = overrides.Font ?? Font,
            SizePt = overrides.SizePt ?? SizePt,
            Color = overrides.Color ?? Color,
            Align = overrides.Align ?? Align,
            VerticalAlign = overrides.VerticalAlign ?? VerticalAlign,
            LineHeight = overrides.LineHeight ?? LineHeight,
            LetterSpacingPt = overrides.LetterSpacingPt ?? LetterSpacingPt,
            ScaleX = overrides.ScaleX ?? ScaleX,
            Transform = overrides.Transform ?? Transform,
            MaxLines = overrides.MaxLines ?? MaxLines,
            PaddingXPt = overrides.PaddingXPt ?? PaddingXPt,
            PaddingYPt = overrides.PaddingYPt ?? PaddingYPt,
            AutoFit = overrides.AutoFit ?? AutoFit,
            Stroke = overrides.Stroke ?? Stroke,
            Shadow = overrides.Shadow ?? Shadow,
        };
}

/// <summary>
/// Sovrascritture parziali di uno stile: e' il meccanismo con cui una rarita' cambia il colore del
/// nome senza duplicare il template.
/// </summary>
public sealed record TextStyleOverrides
{
    public string? Font { get; init; }

    public double? SizePt { get; init; }

    public string? Color { get; init; }

    public TextAlign? Align { get; init; }

    public VerticalAlign? VerticalAlign { get; init; }

    public double? LineHeight { get; init; }

    public double? LetterSpacingPt { get; init; }

    public double? ScaleX { get; init; }

    public TextTransform? Transform { get; init; }

    public int? MaxLines { get; init; }

    public double? PaddingXPt { get; init; }

    public double? PaddingYPt { get; init; }

    public AutoFitSettings? AutoFit { get; init; }

    public StrokeStyle? Stroke { get; init; }

    public ShadowStyle? Shadow { get; init; }
}
