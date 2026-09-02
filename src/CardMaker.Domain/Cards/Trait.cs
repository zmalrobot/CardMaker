using CardMaker.Domain.Common;
using CardMaker.Domain.Games;

namespace CardMaker.Domain.Cards;

/// <summary>
/// Specializzazione che non cambia il frame ma influenza testo e regole, es. Tuner, Toon, Spirit.
/// </summary>
public class Trait : Entity
{
    public Guid GameId { get; set; }

    public Game Game { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    /// <summary>Raggruppamento logico, es. "ability" oppure "summon-method".</summary>
    public string Group { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<CardTypeTrait> CardTypes { get; set; } = [];
}

public class CardTypeTrait
{
    public Guid CardTypeId { get; set; }

    public CardType CardType { get; set; } = null!;

    public Guid TraitId { get; set; }

    public Trait Trait { get; set; } = null!;
}
