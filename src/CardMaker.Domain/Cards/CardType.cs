using CardMaker.Domain.Assets;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Templates;

namespace CardMaker.Domain.Cards;

/// <summary>
/// Un tipo di carta di un gioco, es. "monster-xyz", "spell", "rush-maximum-left".
/// </summary>
public class CardType : Entity
{
    public Guid GameId { get; set; }

    public Game Game { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    public LocalizedText Description { get; set; } = new();

    public Guid? IconAssetId { get; set; }

    public Asset? IconAsset { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<FieldDefinition> Fields { get; set; } = [];

    public ICollection<Template> Templates { get; set; } = [];

    public ICollection<CardTypeTrait> AllowedTraits { get; set; } = [];
}
