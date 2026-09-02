using CardMaker.Domain.Common;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;

namespace CardMaker.Domain.Cards;

public enum FieldKind
{
    Text = 0,
    MultilineText = 1,
    RichText = 2,
    Integer = 3,
    Decimal = 4,
    Boolean = 5,
    Enum = 6,
    MultiEnum = 7,
    Image = 8,
    Color = 9,
    SymbolRef = 10,
    ToggleSet = 11,
    Computed = 12,
}

/// <summary>
/// Definisce un campo dello schema di un tipo di carta. Da qui viene generato il form dell'utente
/// e da qui derivano i binding {{key}} usati nei layout.
/// </summary>
public class FieldDefinition : Entity
{
    public Guid CardTypeId { get; set; }

    public CardType CardType { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Label { get; set; } = new();

    public LocalizedText HelpText { get; set; } = new();

    public FieldKind Kind { get; set; } = FieldKind.Text;

    public bool IsRequired { get; set; }

    public string? DefaultValueJson { get; set; }

    public Guid? OptionListId { get; set; }

    public OptionList? OptionList { get; set; }

    public Guid? SymbolSetId { get; set; }

    public SymbolSet? SymbolSet { get; set; }

    /// <summary>JSON: { min, max, maxLength, pattern }.</summary>
    public string? ValidationJson { get; set; }

    /// <summary>JSON: espressione dichiarativa per i campi calcolati (type line, LINK-n).</summary>
    public string? ComputedExprJson { get; set; }

    /// <summary>JSON: AST condizionale che decide se il campo compare nel form.</summary>
    public string? VisibleWhenJson { get; set; }

    public string? GroupName { get; set; }

    public int SortOrder { get; set; }
}
