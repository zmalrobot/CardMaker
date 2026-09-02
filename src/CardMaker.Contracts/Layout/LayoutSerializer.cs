using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardMaker.Contracts.Layout;

public sealed record LayoutIssue(string Code, string Message, string? LayerId = null);

public sealed record LayoutValidationResult(bool IsValid, IReadOnlyList<LayoutIssue> Issues)
{
    public static readonly LayoutValidationResult Valid = new(true, []);
}

public static class LayoutSerializer
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = false,
    };

    public static string Serialize(CardLayout layout) => JsonSerializer.Serialize(layout, Options);

    public static CardLayout? Deserialize(string json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<CardLayout>(json, Options);

    /// <summary>Il modello C# e' lo schema: qui si verificano solo i vincoli che i tipi non esprimono.</summary>
    public static LayoutValidationResult Validate(CardLayout? layout)
    {
        var issues = new List<LayoutIssue>();

        if (layout is null)
        {
            return new LayoutValidationResult(false, [new LayoutIssue("layout.missing", "Layout assente o non deserializzabile.")]);
        }

        if (layout.SchemaVersion is < 1 or > CardLayout.CurrentSchemaVersion)
        {
            issues.Add(new LayoutIssue("layout.unsupportedSchemaVersion",
                $"Versione di schema {layout.SchemaVersion} non supportata."));
        }

        if (layout.Canvas.WidthMm <= 0 || layout.Canvas.HeightMm <= 0)
        {
            issues.Add(new LayoutIssue("canvas.invalidSize", "Le dimensioni della carta devono essere positive."));
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var layer in layout.EnumerateLayers())
        {
            if (!seenIds.Add(layer.Id))
            {
                issues.Add(new LayoutIssue("layer.duplicateId", $"Id di layer duplicato: {layer.Id}.", layer.Id));
            }

            if (layer.Rect.Width <= 0 || layer.Rect.Height <= 0)
            {
                issues.Add(new LayoutIssue("layer.emptyRect", "Il rettangolo del layer ha area nulla.", layer.Id));
            }

            if (layer.Opacity is < 0 or > 1)
            {
                issues.Add(new LayoutIssue("layer.invalidOpacity", "L'opacita' deve essere fra 0 e 1.", layer.Id));
            }

            ValidateCondition(layer.VisibleWhen, layer.Id, issues);

            if (layer is TextLayer text)
            {
                if (text.Style is not null && !layout.TextStyles.ContainsKey(text.Style))
                {
                    issues.Add(new LayoutIssue("layer.unknownTextStyle",
                        $"Stile di testo '{text.Style}' non definito.", layer.Id));
                }

                if (string.IsNullOrEmpty(text.Source))
                {
                    issues.Add(new LayoutIssue("layer.emptySource", "Il layer di testo non ha sorgente.", layer.Id));
                }
            }

            if (layer is SymbolSlotLayer symbol
                && string.IsNullOrWhiteSpace(symbol.SymbolKey)
                && string.IsNullOrWhiteSpace(symbol.FieldKey))
            {
                issues.Add(new LayoutIssue("layer.symbolWithoutSource",
                    "Il simbolo non ha ne' chiave fissa ne' campo di origine.", layer.Id));
            }

            if (layer is SymbolRepeaterLayer repeater && repeater.MaxCount <= 0)
            {
                issues.Add(new LayoutIssue("layer.invalidMaxCount",
                    "Il numero massimo di posizioni deve essere positivo.", layer.Id));
            }

            if (layer is ToggleGroupLayer toggle && toggle.Items.Count == 0)
            {
                issues.Add(new LayoutIssue("layer.toggleGroupWithoutItems",
                    "Il gruppo di stato non ha posizioni definite.", layer.Id));
            }

            if (layer is RichTextLayer richText)
            {
                if (richText.Style is not null && !layout.TextStyles.ContainsKey(richText.Style))
                {
                    issues.Add(new LayoutIssue("layer.unknownTextStyle",
                        $"Stile di testo '{richText.Style}' non definito.", layer.Id));
                }

                if (string.IsNullOrEmpty(richText.Source))
                {
                    issues.Add(new LayoutIssue("layer.emptySource", "Il layer di testo non ha sorgente.", layer.Id));
                }
            }

            if (layer is OverlayLayer overlay && overlay.AssetId is null && string.IsNullOrWhiteSpace(overlay.AssetKey))
            {
                issues.Add(new LayoutIssue("layer.overlayWithoutSource",
                    "L'overlay non ha ne' AssetId ne' AssetKey.", layer.Id));
            }

            if (layer is ImageSlotLayer slot && slot.SliceCount > 1 && (slot.SliceIndex < 0 || slot.SliceIndex >= slot.SliceCount))
            {
                issues.Add(new LayoutIssue("layer.invalidSliceIndex",
                    "L'indice della fetta e' fuori dall'intervallo di SliceCount.", layer.Id));
            }
        }

        foreach (var computed in layout.Computed)
        {
            if (computed.Expr.Op is not (ComputedOps.Join or ComputedOps.Concat or ComputedOps.Count))
            {
                issues.Add(new LayoutIssue("computed.unknownOp",
                    $"Operatore calcolato sconosciuto: {computed.Expr.Op}."));
            }
        }

        return new LayoutValidationResult(issues.Count == 0, issues);
    }

    private static void ValidateCondition(Condition? condition, string layerId, List<LayoutIssue> issues)
    {
        if (condition is null)
        {
            return;
        }

        if (!ConditionOps.All.Contains(condition.Op, StringComparer.Ordinal))
        {
            issues.Add(new LayoutIssue("condition.unknownOp",
                $"Operatore di condizione sconosciuto: {condition.Op}.", layerId));
        }

        foreach (var arg in condition.Args ?? [])
        {
            ValidateCondition(arg, layerId, issues);
        }
    }
}
