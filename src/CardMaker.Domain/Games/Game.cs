using CardMaker.Domain.Assets;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Common;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;

namespace CardMaker.Domain.Games;

/// <summary>
/// Un gioco di carte. Definisce il formato fisico e raccoglie tutti i contenuti caricati dall'admin.
/// </summary>
public class Game : Entity
{
    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    public LocalizedText Description { get; set; } = new();

    // Formato fisico (mm). Yu-Gi-Oh! 59 x 86; Pokemon e Magic 63 x 88.
    public decimal WidthMm { get; set; } = 59m;

    public decimal HeightMm { get; set; } = 86m;

    public decimal CornerRadiusMm { get; set; } = 2m;

    public decimal BleedMm { get; set; } = 2m;

    public decimal SafeZoneMm { get; set; } = 3m;

    public int DefaultDpi { get; set; } = 600;

    public string DefaultCulture { get; set; } = LocalizedText.DefaultCulture;

    public Guid? CardBackTemplateId { get; set; }

    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }

    public ICollection<CardType> CardTypes { get; set; } = [];

    public ICollection<SymbolSet> SymbolSets { get; set; } = [];

    public ICollection<OptionList> OptionLists { get; set; } = [];

    public ICollection<Trait> Traits { get; set; } = [];

    public ICollection<Asset> Assets { get; set; } = [];

    public ICollection<FontAsset> Fonts { get; set; } = [];
}
