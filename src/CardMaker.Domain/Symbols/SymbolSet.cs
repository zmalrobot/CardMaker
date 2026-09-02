using CardMaker.Domain.Assets;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;

namespace CardMaker.Domain.Symbols;

/// <summary>
/// Insieme omogeneo di simboli, es. "attributes", "spell-properties", "link-arrows".
/// </summary>
public class SymbolSet : Entity
{
    public Guid GameId { get; set; }

    public Game Game { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    public int SortOrder { get; set; }

    public ICollection<Symbol> Symbols { get; set; } = [];
}

public class Symbol : Entity
{
    public Guid SymbolSetId { get; set; }

    public SymbolSet SymbolSet { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    public Guid? AssetId { get; set; }

    public Asset? Asset { get; set; }

    /// <summary>Token utilizzabile nel rich text, es. "{sym:attributes.dark}".</summary>
    public string? InlineToken { get; set; }

    public int SortOrder { get; set; }
}
