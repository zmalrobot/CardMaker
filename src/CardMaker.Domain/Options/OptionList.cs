using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Symbols;

namespace CardMaker.Domain.Options;

/// <summary>
/// Enumerazione gestita dall'admin senza toccare il codice: razze, rarita', edizioni, attributi.
/// </summary>
public class OptionList : Entity
{
    public Guid GameId { get; set; }

    public Game Game { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    public ICollection<OptionItem> Items { get; set; } = [];
}

public class OptionItem : Entity
{
    public Guid OptionListId { get; set; }

    public OptionList OptionList { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Label { get; set; } = new();

    public Guid? SymbolId { get; set; }

    public Symbol? Symbol { get; set; }

    /// <summary>JSON libero, es. il colore del nome associato a una rarita'.</summary>
    public string? MetadataJson { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
