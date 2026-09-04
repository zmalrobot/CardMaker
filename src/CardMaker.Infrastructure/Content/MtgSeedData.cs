using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;
using CardMaker.Domain.Templates;

namespace CardMaker.Infrastructure.Content;

public static class MtgSeedData
{
    public const string GameKey = "mtg";

    public static SeedGraph Build()
    {
        var game = new Game
        {
            Key = GameKey,
            Name = LocalizedText.From("Magic: The Gathering", "Magic: The Gathering"),
            Description = LocalizedText.From(
                "Magic: The Gathering (formato Poker 63×88 mm).",
                "Magic: The Gathering (Poker format 63×88 mm)."),
            WidthMm = 63,
            HeightMm = 88,
            BleedMm = 2,
            CornerRadiusMm = 3,
            SafeZoneMm = 3,
            DefaultDpi = 600,
        };

        var manaSet = BuildManaSymbolSet(game);
        var raritySet = BuildRaritySymbolSet(game);
        var symbolSets = new List<SymbolSet> { manaSet, raritySet };

        var colors = BuildColorsOptionList(game);
        var rarities = BuildRaritiesOptionList(game);
        var optionLists = new List<OptionList> { colors, rarities };

        var traits = BuildTraits(game);

        var creatureLayout = DemoLayouts.MtgCreature();
        var spellLayout = BuildMtgSpellLayout();

        var cardTypes = new List<CardType>
        {
            BuildCreatureCardType(game, "mtg-creature", "Creatura", "Creature", raritySet, traits, creatureLayout),
            BuildSpellCardType(game, "mtg-instant", "Istantaneo", "Instant", raritySet, traits, spellLayout),
            BuildSpellCardType(game, "mtg-sorcery", "Stregoneria", "Sorcery", raritySet, traits, spellLayout),
            BuildSpellCardType(game, "mtg-enchantment", "Incantesimo", "Enchantment", raritySet, traits, spellLayout),
            BuildSpellCardType(game, "mtg-artifact", "Artefatto", "Artifact", raritySet, traits, spellLayout),
            BuildPlaneswalkerCardType(game, "mtg-planeswalker", "Planeswalker", "Planeswalker", raritySet, traits, spellLayout),
            BuildSpellCardType(game, "mtg-land", "Terra", "Land", raritySet, traits, spellLayout),
        };

        return new SeedGraph(game, cardTypes, traits, optionLists, symbolSets);
    }

    public static CardLayout BuildMtgCreatureLayout() => DemoLayouts.MtgCreature();

    public static CardLayout BuildMtgSpellLayout()
    {
        var layout = DemoLayouts.MtgCreature();
        var layers = layout.Layers.Where(l => l.Id != "pt").ToList();
        return layout with { Layers = layers };
    }

    private static SymbolSet BuildManaSymbolSet(Game game)
    {
        var set = new SymbolSet
        {
            GameId = game.Id,
            Game = game,
            Key = "mtg-mana",
            Name = LocalizedText.From("Simboli di Mana", "Mana Symbols"),
        };

        var symbols = new (string Key, string It, string En)[]
        {
            ("w", "Bianco", "White"),
            ("u", "Blu", "Blue"),
            ("b", "Nero", "Black"),
            ("r", "Rosso", "Red"),
            ("g", "Verde", "Green"),
            ("c", "Incolore", "Colorless"),
            ("0", "0", "0"),
            ("1", "1", "1"),
            ("2", "2", "2"),
            ("3", "3", "3"),
            ("4", "4", "4"),
            ("5", "5", "5"),
            ("6", "6", "6"),
            ("7", "7", "7"),
            ("8", "8", "8"),
            ("9", "9", "9"),
            ("x", "X", "X"),
            ("tap", "TAP", "TAP"),
        };

        var list = new List<Symbol>();
        for (var i = 0; i < symbols.Length; i++)
        {
            var (k, it, en) = symbols[i];
            list.Add(new Symbol
            {
                SymbolSetId = set.Id,
                SymbolSet = set,
                Key = k,
                Name = LocalizedText.From(it, en),
                InlineToken = $"{{mana:{k}}}",
                SortOrder = i,
            });
        }
        set.Symbols = list;
        return set;
    }

    private static SymbolSet BuildRaritySymbolSet(Game game)
    {
        var set = new SymbolSet
        {
            GameId = game.Id,
            Game = game,
            Key = "mtg-rarity",
            Name = LocalizedText.From("Simboli di Rarità", "Rarity Symbols"),
        };

        var symbols = new (string Key, string It, string En)[]
        {
            ("common", "Comune", "Common"),
            ("uncommon", "Non comune", "Uncommon"),
            ("rare", "Rara", "Rare"),
            ("mythic", "Rara Mitica", "Mythic Rare"),
        };

        var list = new List<Symbol>();
        for (var i = 0; i < symbols.Length; i++)
        {
            var (k, it, en) = symbols[i];
            list.Add(new Symbol
            {
                SymbolSetId = set.Id,
                SymbolSet = set,
                Key = k,
                Name = LocalizedText.From(it, en),
                InlineToken = $"{{rarity:{k}}}",
                SortOrder = i,
            });
        }
        set.Symbols = list;
        return set;
    }

    private static OptionList BuildColorsOptionList(Game game)
    {
        var list = new OptionList
        {
            GameId = game.Id,
            Game = game,
            Key = "mtg-colors",
            Name = LocalizedText.From("Colori di Magic", "Magic Colors"),
        };

        var items = new (string Key, string It, string En)[]
        {
            ("white", "Bianco", "White"),
            ("blue", "Blu", "Blue"),
            ("black", "Nero", "Black"),
            ("red", "Rosso", "Red"),
            ("green", "Verde", "Green"),
            ("colorless", "Incolore", "Colorless"),
            ("multicolor", "Multicolore", "Multicolor"),
        };

        list.Items = [.. items.Select((item, i) => new OptionItem
        {
            OptionListId = list.Id,
            OptionList = list,
            Key = item.Key,
            Label = LocalizedText.From(item.It, item.En),
            SortOrder = i,
        })];

        return list;
    }

    private static OptionList BuildRaritiesOptionList(Game game)
    {
        var list = new OptionList
        {
            GameId = game.Id,
            Game = game,
            Key = "mtg-rarities",
            Name = LocalizedText.From("Rarità Magic", "Magic Rarities"),
        };

        var items = new (string Key, string It, string En)[]
        {
            ("common", "Comune", "Common"),
            ("uncommon", "Non comune", "Uncommon"),
            ("rare", "Rara", "Rare"),
            ("mythic", "Rara Mitica", "Mythic Rare"),
        };

        list.Items = [.. items.Select((item, i) => new OptionItem
        {
            OptionListId = list.Id,
            OptionList = list,
            Key = item.Key,
            Label = LocalizedText.From(item.It, item.En),
            SortOrder = i,
        })];

        return list;
    }

    private static List<Trait> BuildTraits(Game game)
    {
        var traits = new (string Key, string It, string En)[]
        {
            ("legendary", "Leggendario", "Legendary"),
            ("snow", "Neve", "Snow"),
            ("token", "Pedina", "Token"),
            ("basic", "Base", "Basic"),
            ("saga", "Saga", "Saga"),
            ("vehicle", "Veicolo", "Vehicle"),
        };

        return [.. traits.Select((t, i) => new Trait
        {
            GameId = game.Id,
            Game = game,
            Key = t.Key,
            Name = LocalizedText.From(t.It, t.En),
            SortOrder = i,
        })];
    }

    private static CardType BuildCreatureCardType(
        Game game, string key, string labelIt, string labelEn,
        SymbolSet raritySet, IReadOnlyList<Trait> traits, CardLayout layout)
    {
        var ct = new CardType
        {
            GameId = game.Id,
            Game = game,
            Key = key,
            Name = LocalizedText.From(labelIt, labelEn),
        };

        var fields = new List<FieldDefinition>();
        var order = 0;
        fields.Add(TextField(ct, "name", "Nome", "Name", order++, isRequired: true));
        fields.Add(TextField(ct, "manaCost", "Costo di Mana", "Mana Cost", order++));
        fields.Add(ImageField(ct, "artwork", "Illustrazione", "Artwork", order++));
        fields.Add(TextField(ct, "typeLine", "Riga del Tipo", "Type Line", order++, isRequired: true));
        fields.Add(RichTextField(ct, "rulesText", "Testo delle Regole", "Rules Text", order++));
        fields.Add(RichTextField(ct, "flavorText", "Testo di Colore", "Flavor Text", order++));
        fields.Add(TextField(ct, "power", "Forza", "Power", order++));
        fields.Add(TextField(ct, "toughness", "Costituzione", "Toughness", order++));
        fields.Add(SymbolField(ct, "setRarity", "Rarità", "Rarity", raritySet, order++));
        fields.Add(TextField(ct, "illustrator", "Illustratore", "Illustrator", order++));
        fields.Add(TextField(ct, "collectorNumber", "Numero Collettore", "Collector Number", order++));
        fields.Add(TextField(ct, "copyright", "Copyright", "Copyright", order++));

        ct.Fields = fields;
        ct.AllowedTraits = [.. traits.Select(t => new CardTypeTrait { CardTypeId = ct.Id, CardType = ct, TraitId = t.Id, Trait = t })];
        ct.Templates = [SingleTemplate(ct, key, labelIt, labelEn, CardFace.Front, layout)];
        return ct;
    }

    private static CardType BuildSpellCardType(
        Game game, string key, string labelIt, string labelEn,
        SymbolSet raritySet, IReadOnlyList<Trait> traits, CardLayout layout)
    {
        var ct = new CardType
        {
            GameId = game.Id,
            Game = game,
            Key = key,
            Name = LocalizedText.From(labelIt, labelEn),
        };

        var fields = new List<FieldDefinition>();
        var order = 0;
        fields.Add(TextField(ct, "name", "Nome", "Name", order++, isRequired: true));
        fields.Add(TextField(ct, "manaCost", "Costo di Mana", "Mana Cost", order++));
        fields.Add(ImageField(ct, "artwork", "Illustrazione", "Artwork", order++));
        fields.Add(TextField(ct, "typeLine", "Riga del Tipo", "Type Line", order++, isRequired: true));
        fields.Add(RichTextField(ct, "rulesText", "Testo delle Regole", "Rules Text", order++));
        fields.Add(RichTextField(ct, "flavorText", "Testo di Colore", "Flavor Text", order++));
        fields.Add(SymbolField(ct, "setRarity", "Rarità", "Rarity", raritySet, order++));
        fields.Add(TextField(ct, "illustrator", "Illustratore", "Illustrator", order++));
        fields.Add(TextField(ct, "collectorNumber", "Numero Collettore", "Collector Number", order++));
        fields.Add(TextField(ct, "copyright", "Copyright", "Copyright", order++));

        ct.Fields = fields;
        ct.AllowedTraits = [.. traits.Select(t => new CardTypeTrait { CardTypeId = ct.Id, CardType = ct, TraitId = t.Id, Trait = t })];
        ct.Templates = [SingleTemplate(ct, key, labelIt, labelEn, CardFace.Front, layout)];
        return ct;
    }

    private static CardType BuildPlaneswalkerCardType(
        Game game, string key, string labelIt, string labelEn,
        SymbolSet raritySet, IReadOnlyList<Trait> traits, CardLayout layout)
    {
        var ct = new CardType
        {
            GameId = game.Id,
            Game = game,
            Key = key,
            Name = LocalizedText.From(labelIt, labelEn),
        };

        var fields = new List<FieldDefinition>();
        var order = 0;
        fields.Add(TextField(ct, "name", "Nome", "Name", order++, isRequired: true));
        fields.Add(TextField(ct, "manaCost", "Costo di Mana", "Mana Cost", order++));
        fields.Add(ImageField(ct, "artwork", "Illustrazione", "Artwork", order++));
        fields.Add(TextField(ct, "typeLine", "Riga del Tipo", "Type Line", order++, isRequired: true));
        fields.Add(RichTextField(ct, "rulesText", "Testo delle Regole", "Rules Text", order++));
        fields.Add(IntegerField(ct, "loyalty", "Fedeltà", "Loyalty", order++));
        fields.Add(SymbolField(ct, "setRarity", "Rarità", "Rarity", raritySet, order++));
        fields.Add(TextField(ct, "illustrator", "Illustratore", "Illustrator", order++));
        fields.Add(TextField(ct, "collectorNumber", "Numero Collettore", "Collector Number", order++));
        fields.Add(TextField(ct, "copyright", "Copyright", "Copyright", order++));

        ct.Fields = fields;
        ct.AllowedTraits = [.. traits.Select(t => new CardTypeTrait { CardTypeId = ct.Id, CardType = ct, TraitId = t.Id, Trait = t })];
        ct.Templates = [SingleTemplate(ct, key, labelIt, labelEn, CardFace.Front, layout)];
        return ct;
    }

    private static FieldDefinition TextField(CardType ct, string key, string labelIt, string labelEn, int order, bool isRequired = false) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Text, SortOrder = order, IsRequired = isRequired };

    private static FieldDefinition ImageField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Image, SortOrder = order };

    private static FieldDefinition IntegerField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Integer, SortOrder = order };

    private static FieldDefinition RichTextField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.RichText, SortOrder = order };

    private static FieldDefinition SymbolField(CardType ct, string key, string labelIt, string labelEn, SymbolSet set, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.SymbolRef, SymbolSetId = set.Id, SymbolSet = set, SortOrder = order };

    private static Template SingleTemplate(CardType cardType, string key, string labelIt, string labelEn, CardFace face, CardLayout layout)
    {
        var template = new Template
        {
            CardTypeId = cardType.Id,
            CardType = cardType,
            Key = key + "-v1",
            Name = LocalizedText.From(labelIt, labelEn),
            Face = face,
            IsDefault = true,
            SortOrder = 0,
        };
        template.Versions = [PublishedVersion(template, layout)];
        return template;
    }

    private static TemplateVersion PublishedVersion(Template template, CardLayout layout) => new()
    {
        TemplateId = template.Id,
        Template = template,
        VersionNumber = 1,
        Status = TemplateStatus.Published,
        LayoutJson = LayoutSerializer.Serialize(layout),
        PublishedAtUtc = DateTimeOffset.UtcNow,
    };
}

