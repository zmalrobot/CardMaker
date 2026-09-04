using System.Text.Json;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;
using CardMaker.Domain.Templates;
using CardMaker.Rendering.Placeholders;

namespace CardMaker.Infrastructure.Content;

public static class PokemonSeedData
{
    public const string GameKey = "pokemon";

    private sealed record PokemonSpec(
        string Key, string LabelIt, string LabelEn, string FrameKey, PlaceholderLayout Layout, string Stage);

    private static readonly PokemonSpec[] PokemonSpecs =
    [
        new("pokemon-basic", "Pokémon Base", "Basic Pokémon", "pokemon-frame-lightning", PlaceholderLayout.Pokemon, "BASE"),
        new("pokemon-stage1", "Pokémon Fase 1", "Stage 1 Pokémon", "pokemon-frame-lightning", PlaceholderLayout.Pokemon, "FASE 1"),
        new("pokemon-stage2", "Pokémon Fase 2", "Stage 2 Pokémon", "pokemon-frame-lightning", PlaceholderLayout.Pokemon, "FASE 2"),
    ];

    public static SeedGraph Build()
    {
        var game = new Game
        {
            Key = GameKey,
            Name = LocalizedText.From("Pokémon TCG", "Pokémon TCG"),
            Description = LocalizedText.From(
                "Gioco di Carte Collezionabili Pokémon (formato Poker 63×88 mm).",
                "Pokémon Trading Card Game (Poker format 63×88 mm)."),
            WidthMm = 63,
            HeightMm = 88,
            BleedMm = 2,
            CornerRadiusMm = 3,
            SafeZoneMm = 3,
            DefaultDpi = 600,
        };

        var energySet = BuildEnergySymbolSet(game);
        var raritySet = BuildRaritySymbolSet(game);
        var symbolSets = new List<SymbolSet> { energySet, raritySet };

        var stages = BuildStagesOptionList(game);
        var trainerTypes = BuildTrainerTypesOptionList(game);
        var energyTypes = BuildEnergyTypesOptionList(game);
        var optionLists = new List<OptionList> { stages, trainerTypes, energyTypes };

        var traits = BuildTraits(game);

        var cardTypes = new List<CardType>();
        foreach (var spec in PokemonSpecs)
        {
            cardTypes.Add(BuildPokemonCardType(game, spec, energySet, raritySet, stages, traits));
        }

        cardTypes.Add(BuildTrainerCardType(game, "trainer-item", "Strumento", "Item", "pokemon-frame-trainer", trainerTypes));
        cardTypes.Add(BuildTrainerCardType(game, "trainer-supporter", "Aiuto", "Supporter", "pokemon-frame-trainer", trainerTypes));
        cardTypes.Add(BuildTrainerCardType(game, "trainer-stadium", "Stadio", "Stadium", "pokemon-frame-trainer", trainerTypes));
        cardTypes.Add(BuildEnergyCardType(game, "energy-basic", "Energia Base", "Basic Energy", "pokemon-frame-energy", energySet));
        cardTypes.Add(BuildEnergyCardType(game, "energy-special", "Energia Speciale", "Special Energy", "pokemon-frame-energy", energySet));

        return new SeedGraph(game, cardTypes, traits, optionLists, symbolSets);
    }

    private static SymbolSet BuildEnergySymbolSet(Game game)
    {
        var set = new SymbolSet
        {
            GameId = game.Id,
            Game = game,
            Key = "pokemon-energy",
            Name = LocalizedText.From("Energie Pokémon", "Pokémon Energy"),
        };

        var symbols = new (string Key, string LabelIt, string LabelEn)[]
        {
            ("grass", "Erba", "Grass"),
            ("fire", "Fuoco", "Fire"),
            ("water", "Acqua", "Water"),
            ("lightning", "Lampo", "Lightning"),
            ("psychic", "Psico", "Psychic"),
            ("fighting", "Lotta", "Fighting"),
            ("darkness", "Oscurità", "Darkness"),
            ("metal", "Metallo", "Metal"),
            ("fairy", "Folletto", "Fairy"),
            ("dragon", "Drago", "Dragon"),
            ("colorless", "Incolore", "Colorless"),
        };

        set.Symbols = [.. symbols.Select((s, i) => new Symbol
        {
            SymbolSetId = set.Id,
            SymbolSet = set,
            Key = s.Key,
            Name = LocalizedText.From(s.LabelIt, s.LabelEn),
            InlineToken = $"{{sym:{set.Key}.{s.Key}}}",
            SortOrder = i,
        })];

        return set;
    }

    private static SymbolSet BuildRaritySymbolSet(Game game)
    {
        var set = new SymbolSet
        {
            GameId = game.Id,
            Game = game,
            Key = "pokemon-rarity",
            Name = LocalizedText.From("Rarità Pokémon", "Pokémon Rarity"),
        };

        var symbols = new (string Key, string LabelIt, string LabelEn)[]
        {
            ("common", "Comune", "Common"),
            ("uncommon", "Non comune", "Uncommon"),
            ("rare", "Rara", "Rare"),
        };

        set.Symbols = [.. symbols.Select((s, i) => new Symbol
        {
            SymbolSetId = set.Id,
            SymbolSet = set,
            Key = s.Key,
            Name = LocalizedText.From(s.LabelIt, s.LabelEn),
            InlineToken = $"{{sym:{set.Key}.{s.Key}}}",
            SortOrder = i,
        })];

        return set;
    }

    private static OptionList BuildStagesOptionList(Game game)
    {
        var list = new OptionList
        {
            GameId = game.Id,
            Game = game,
            Key = "pokemon-stages",
            Name = LocalizedText.From("Fase Pokémon", "Pokémon Stage"),
        };

        var items = new (string Key, string LabelIt, string LabelEn)[]
        {
            ("base", "Base", "Basic"),
            ("stage1", "Fase 1", "Stage 1"),
            ("stage2", "Fase 2", "Stage 2"),
        };

        list.Items = [.. items.Select((item, i) => new OptionItem
        {
            OptionListId = list.Id,
            OptionList = list,
            Key = item.Key,
            Label = LocalizedText.From(item.LabelIt, item.LabelEn),
            SortOrder = i,
        })];

        return list;
    }

    private static OptionList BuildTrainerTypesOptionList(Game game)
    {
        var list = new OptionList
        {
            GameId = game.Id,
            Game = game,
            Key = "trainer-types",
            Name = LocalizedText.From("Tipo Allenatore", "Trainer Type"),
        };

        var items = new (string Key, string LabelIt, string LabelEn)[]
        {
            ("item", "Strumento", "Item"),
            ("supporter", "Aiuto", "Supporter"),
            ("stadium", "Stadio", "Stadium"),
        };

        list.Items = [.. items.Select((item, i) => new OptionItem
        {
            OptionListId = list.Id,
            OptionList = list,
            Key = item.Key,
            Label = LocalizedText.From(item.LabelIt, item.LabelEn),
            SortOrder = i,
        })];

        return list;
    }

    private static OptionList BuildEnergyTypesOptionList(Game game)
    {
        var list = new OptionList
        {
            GameId = game.Id,
            Game = game,
            Key = "energy-types",
            Name = LocalizedText.From("Tipo Energia", "Energy Type"),
        };

        var items = new (string Key, string LabelIt, string LabelEn)[]
        {
            ("basic", "Base", "Basic"),
            ("special", "Speciale", "Special"),
        };

        list.Items = [.. items.Select((item, i) => new OptionItem
        {
            OptionListId = list.Id,
            OptionList = list,
            Key = item.Key,
            Label = LocalizedText.From(item.LabelIt, item.LabelEn),
            SortOrder = i,
        })];

        return list;
    }

    private static List<Trait> BuildTraits(Game game)
    {
        var traits = new (string Key, string LabelIt, string LabelEn)[]
        {
            ("ex", "ex", "ex"),
            ("v", "V", "V"),
            ("vmax", "VMAX", "VMAX"),
            ("vstar", "VSTAR", "VSTAR"),
            ("radiant", "Lucente", "Radiant"),
            ("ancient", "Tempo Passato", "Ancient"),
            ("future", "Tempo Futuro", "Future"),
        };

        return [.. traits.Select((t, i) => new Trait
        {
            GameId = game.Id,
            Game = game,
            Key = t.Key,
            Name = LocalizedText.From(t.LabelIt, t.LabelEn),
            SortOrder = i,
        })];
    }

    private static CardType BuildPokemonCardType(
        Game game, PokemonSpec spec, SymbolSet energySet, SymbolSet raritySet,
        OptionList stages, IReadOnlyList<Trait> traits)
    {
        var cardType = new CardType
        {
            GameId = game.Id,
            Game = game,
            Key = spec.Key,
            Name = LocalizedText.From(spec.LabelIt, spec.LabelEn),
        };

        var fields = new List<FieldDefinition>();
        var order = 0;

        fields.Add(TextField(cardType, "name", "Nome Pokémon", "Pokémon Name", order++));
        fields.Add(EnumField(cardType, "stage", "Fase", "Stage", stages, order++));
        fields.Add(TextField(cardType, "evolvesFrom", "Si evolve da", "Evolves From", order++));
        fields.Add(IntegerField(cardType, "hp", "Punti Salute (HP)", "Hit Points (HP)", order++));
        fields.Add(SymbolField(cardType, "energyType", "Tipo Energia", "Energy Type", energySet, order++));
        fields.Add(ImageField(cardType, "artwork", "Illustrazione", "Artwork", order++));
        fields.Add(TextField(cardType, "pokedexEntry", "Info Pokédex", "Pokédex Info", order++));

        fields.Add(TextField(cardType, "attack1Name", "Nome Attacco 1", "Attack 1 Name", order++));
        fields.Add(TextField(cardType, "attack1Damage", "Danno Attacco 1", "Attack 1 Damage", order++));
        fields.Add(RichTextField(cardType, "attack1Effect", "Effetto Attacco 1", "Attack 1 Effect", order++));

        fields.Add(TextField(cardType, "attack2Name", "Nome Attacco 2", "Attack 2 Name", order++));
        fields.Add(TextField(cardType, "attack2Damage", "Danno Attacco 2", "Attack 2 Damage", order++));
        fields.Add(RichTextField(cardType, "attack2Effect", "Effetto Attacco 2", "Attack 2 Effect", order++));

        fields.Add(SymbolField(cardType, "weakness", "Debolezza", "Weakness", energySet, order++));
        fields.Add(TextField(cardType, "weaknessValue", "Valore Debolezza", "Weakness Value", order++));
        fields.Add(SymbolField(cardType, "resistance", "Resistenza", "Resistance", energySet, order++));
        fields.Add(TextField(cardType, "resistanceValue", "Valore Resistenza", "Resistance Value", order++));
        fields.Add(IntegerField(cardType, "retreatCost", "Costo Ritirata", "Retreat Cost", order++));

        fields.Add(RichTextField(cardType, "flavorText", "Descrizione Pokédex", "Pokédex Flavor Text", order++));
        fields.Add(TextField(cardType, "illustrator", "Illustratore", "Illustrator", order++));
        fields.Add(TextField(cardType, "cardNumber", "Numero Carta", "Card Number", order++));
        fields.Add(SymbolField(cardType, "rarity", "Rarità", "Rarity", raritySet, order++));
        fields.Add(TextField(cardType, "regulationMark", "Lettera Regolamento", "Regulation Mark", order++));
        fields.Add(TextField(cardType, "copyright", "Copyright", "Copyright", order++));

        cardType.Fields = fields;
        cardType.AllowedTraits = [.. traits.Select(t => new CardTypeTrait { CardTypeId = cardType.Id, CardType = cardType, TraitId = t.Id, Trait = t })];

        var regions = PlaceholderFrameGenerator.GetRegions(spec.Layout);
        var layout = BuildPokemonLayout(regions, spec);
        cardType.Templates = [SingleTemplate(cardType, spec.Key, spec.LabelIt, spec.LabelEn, CardFace.Front, layout)];

        return cardType;
    }

    private static CardType BuildTrainerCardType(
        Game game, string key, string labelIt, string labelEn, string frameKey, OptionList trainerTypes)
    {
        var cardType = new CardType
        {
            GameId = game.Id,
            Game = game,
            Key = key,
            Name = LocalizedText.From(labelIt, labelEn),
        };

        cardType.Fields =
        [
            TextField(cardType, "name", "Nome Carta", "Card Name", 0),
            EnumField(cardType, "trainerType", "Sottotipo Allenatore", "Trainer Subtype", trainerTypes, 1),
            ImageField(cardType, "artwork", "Illustrazione", "Artwork", 2),
            RichTextField(cardType, "effectText", "Testo Effetto", "Effect Text", 3),
            TextField(cardType, "illustrator", "Illustratore", "Illustrator", 4),
            TextField(cardType, "cardNumber", "Numero Carta", "Card Number", 5),
            TextField(cardType, "copyright", "Copyright", "Copyright", 6),
        ];

        var regions = PlaceholderFrameGenerator.GetRegions(PlaceholderLayout.PokemonTrainer);
        var layout = BuildTrainerLayout(regions, frameKey);
        cardType.Templates = [SingleTemplate(cardType, key, labelIt, labelEn, CardFace.Front, layout)];

        return cardType;
    }

    private static CardType BuildEnergyCardType(
        Game game, string key, string labelIt, string labelEn, string frameKey, SymbolSet energySet)
    {
        var cardType = new CardType
        {
            GameId = game.Id,
            Game = game,
            Key = key,
            Name = LocalizedText.From(labelIt, labelEn),
        };

        cardType.Fields =
        [
            TextField(cardType, "name", "Nome Energia", "Energy Name", 0),
            SymbolField(cardType, "energyType", "Tipo Energia", "Energy Type", energySet, 1),
            RichTextField(cardType, "effectText", "Testo Regola", "Rule Text", 2),
            TextField(cardType, "copyright", "Copyright", "Copyright", 3),
        ];

        var regions = PlaceholderFrameGenerator.GetRegions(PlaceholderLayout.PokemonEnergy);
        var layout = BuildEnergyLayout(regions, frameKey);
        cardType.Templates = [SingleTemplate(cardType, key, labelIt, labelEn, CardFace.Front, layout)];

        return cardType;
    }

    // ---- Layout builders ----

    private static CardLayout BuildPokemonLayout(PlaceholderRegions regions, PokemonSpec spec)
    {
        var textStyles = BuildPokemonTextStyles();
        var layers = new List<LayerDefinition>
        {
            new ImageSlotLayer
            {
                Id = "artwork", Name = "Illustrazione", Z = 0,
                Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover,
                MinSourceWidth = 900, MinSourceHeight = 900,
            },
            new StaticImageLayer
            {
                Id = "frame", Name = "Frame", Z = 1,
                Rect = new NormalizedRect(0, 0, 1, 1), FullBleed = true, AssetKey = spec.FrameKey, Fit = ImageFit.Stretch,
            },
            new TextLayer
            {
                Id = "stage", Name = "Fase", Z = 2,
                Rect = regions.LevelStrip, Source = "{{stage}}", Style = "stage",
            },
            new TextLayer
            {
                Id = "evolves-from", Name = "Si evolve da", Z = 2,
                Rect = new NormalizedRect(0.180, 0.020, 0.400, 0.020),
                Source = "{{evolvesFrom}}", Style = "evolvesFrom",
                VisibleWhen = Condition.Not(Condition.Equal("evolvesFrom", "")),
            },
            new TextLayer
            {
                Id = "name", Name = "Nome Pokémon", Z = 2,
                Rect = regions.NameBox, Source = "{{name}}", Style = "pokemonName",
            },
            new TextLayer
            {
                Id = "hp", Name = "Punti Salute", Z = 2,
                Rect = regions.DefBox ?? new NormalizedRect(0.640, 0.040, 0.190, 0.045),
                Source = "HP {{hp}}", Style = "hp",
            },
            new SymbolSlotLayer
            {
                Id = "energy-type", Name = "Tipo Energia", Z = 2,
                Rect = regions.AttributeBox, SymbolSetKey = "pokemon-energy", FieldKey = "energyType",
            },
            new TextLayer
            {
                Id = "pokedex", Name = "Info Pokédex", Z = 2,
                Rect = regions.TypeLineBox, Source = "{{pokedexEntry}}", Style = "pokedex",
            },
            new TextLayer
            {
                Id = "attack1-name", Name = "Nome Attacco 1", Z = 2,
                Rect = new NormalizedRect(0.180, 0.565, 0.520, 0.035),
                Source = "{{attack1Name}}", Style = "attackName",
            },
            new TextLayer
            {
                Id = "attack1-damage", Name = "Danno Attacco 1", Z = 2,
                Rect = new NormalizedRect(0.720, 0.565, 0.180, 0.035),
                Source = "{{attack1Damage}}", Style = "attackDamage",
            },
            new RichTextLayer
            {
                Id = "attack1-effect", Name = "Effetto Attacco 1", Z = 2,
                Rect = new NormalizedRect(0.100, 0.605, 0.800, 0.075),
                Source = "{{attack1Effect}}", Style = "attackEffect",
            },
            new TextLayer
            {
                Id = "attack2-name", Name = "Nome Attacco 2", Z = 2,
                Rect = new NormalizedRect(0.180, 0.690, 0.520, 0.035),
                Source = "{{attack2Name}}", Style = "attackName",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new TextLayer
            {
                Id = "attack2-damage", Name = "Danno Attacco 2", Z = 2,
                Rect = new NormalizedRect(0.720, 0.690, 0.180, 0.035),
                Source = "{{attack2Damage}}", Style = "attackDamage",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new RichTextLayer
            {
                Id = "attack2-effect", Name = "Effetto Attacco 2", Z = 2,
                Rect = new NormalizedRect(0.100, 0.730, 0.800, 0.075),
                Source = "{{attack2Effect}}", Style = "attackEffect",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new SymbolSlotLayer
            {
                Id = "weakness", Name = "Debolezza", Z = 2,
                Rect = new NormalizedRect(0.120, 0.895, 0.045, 0.032),
                SymbolSetKey = "pokemon-energy", FieldKey = "weakness",
            },
            new TextLayer
            {
                Id = "weakness-val", Name = "Valore Debolezza", Z = 2,
                Rect = new NormalizedRect(0.170, 0.895, 0.080, 0.032),
                Source = "{{weaknessValue}}", Style = "footerStat",
            },
            new SymbolSlotLayer
            {
                Id = "resistance", Name = "Resistenza", Z = 2,
                Rect = new NormalizedRect(0.420, 0.895, 0.045, 0.032),
                SymbolSetKey = "pokemon-energy", FieldKey = "resistance",
                VisibleWhen = Condition.Not(Condition.Equal("resistance", "")),
            },
            new TextLayer
            {
                Id = "resistance-val", Name = "Valore Resistenza", Z = 2,
                Rect = new NormalizedRect(0.470, 0.895, 0.080, 0.032),
                Source = "{{resistanceValue}}", Style = "footerStat",
                VisibleWhen = Condition.Not(Condition.Equal("resistance", "")),
            },
            new TextLayer
            {
                Id = "retreat-label", Name = "Ritirata Label", Z = 2,
                Rect = new NormalizedRect(0.680, 0.895, 0.120, 0.032),
                Source = "Costo:", Style = "footerStat",
            },
            new TextLayer
            {
                Id = "retreat-cost", Name = "Costo Ritirata", Z = 2,
                Rect = new NormalizedRect(0.800, 0.895, 0.100, 0.032),
                Source = "{{retreatCost}}", Style = "footerStat",
            },
            new RichTextLayer
            {
                Id = "flavor", Name = "Descrizione Pokédex", Z = 2,
                Rect = new NormalizedRect(0.100, 0.815, 0.800, 0.065),
                Source = "{{flavorText}}", Style = "flavorText",
                VisibleWhen = Condition.Not(Condition.Equal("flavorText", "")),
            },
            new TextLayer
            {
                Id = "illustrator", Name = "Illustratore", Z = 2,
                Rect = new NormalizedRect(0.080, 0.945, 0.350, 0.020),
                Source = "{{illustrator}}", Style = "smallPrint",
            },
            new TextLayer
            {
                Id = "card-number", Name = "Numero Carta", Z = 2,
                Rect = new NormalizedRect(0.080, 0.965, 0.200, 0.020),
                Source = "{{cardNumber}}", Style = "smallPrint",
            },
            new SymbolSlotLayer
            {
                Id = "rarity", Name = "Rarità", Z = 2,
                Rect = new NormalizedRect(0.285, 0.965, 0.025, 0.020),
                SymbolSetKey = "pokemon-rarity", FieldKey = "rarity",
            },
            new TextLayer
            {
                Id = "copyright", Name = "Copyright", Z = 2,
                Rect = new NormalizedRect(0.350, 0.965, 0.570, 0.020),
                Source = "{{copyright}}", Style = "copyright",
            },
        };

        return new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.PokerSize()),
            TextStyles = textStyles,
            Layers = layers,
        };
    }

    public static CardLayout BuildTrainerLayout(PlaceholderRegions regions, string frameKey)
    {
        var textStyles = BuildPokemonTextStyles();
        return new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.PokerSize()),
            TextStyles = textStyles,
            Layers =
            [
                new ImageSlotLayer { Id = "artwork", Name = "Illustrazione", Z = 0, Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover },
                new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), FullBleed = true, AssetKey = frameKey, Fit = ImageFit.Stretch },
                new TextLayer { Id = "name", Name = "Nome Carta", Z = 2, Rect = regions.NameBox, Source = "{{name}}", Style = "pokemonName" },
                new TextLayer { Id = "subtype", Name = "Sottotipo", Z = 2, Rect = regions.AttributeBox, Source = "{{trainerType}}", Style = "stage" },
                new RichTextLayer { Id = "effect", Name = "Effetto", Z = 2, Rect = regions.EffectBox, Source = "{{effectText}}", Style = "attackEffect" },
                new TextLayer { Id = "illustrator", Name = "Illustratore", Z = 2, Rect = new NormalizedRect(0.080, 0.945, 0.350, 0.020), Source = "{{illustrator}}", Style = "smallPrint" },
                new TextLayer { Id = "card-number", Name = "Numero Carta", Z = 2, Rect = new NormalizedRect(0.080, 0.965, 0.200, 0.020), Source = "{{cardNumber}}", Style = "smallPrint" },
                new TextLayer { Id = "copyright", Name = "Copyright", Z = 2, Rect = new NormalizedRect(0.350, 0.965, 0.570, 0.020), Source = "{{copyright}}", Style = "copyright" },
            ],
        };
    }

    public static CardLayout BuildEnergyLayout(PlaceholderRegions regions, string frameKey)
    {
        var textStyles = BuildPokemonTextStyles();
        return new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.PokerSize()),
            TextStyles = textStyles,
            Layers =
            [
                new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), FullBleed = true, AssetKey = frameKey, Fit = ImageFit.Stretch },
                new TextLayer { Id = "name", Name = "Nome Energia", Z = 2, Rect = regions.NameBox, Source = "{{name}}", Style = "pokemonName" },
                new SymbolSlotLayer { Id = "energy-type", Name = "Tipo Energia", Z = 2, Rect = regions.AttributeBox, SymbolSetKey = "pokemon-energy", FieldKey = "energyType" },
                new SymbolSlotLayer { Id = "big-symbol", Name = "Simbolo Gigante", Z = 2, Rect = regions.ArtWindow, SymbolSetKey = "pokemon-energy", FieldKey = "energyType" },
                new RichTextLayer { Id = "effect", Name = "Effetto", Z = 2, Rect = regions.EffectBox, Source = "{{effectText}}", Style = "attackEffect", VisibleWhen = Condition.Not(Condition.Equal("effectText", "")) },
                new TextLayer { Id = "copyright", Name = "Copyright", Z = 2, Rect = new NormalizedRect(0.350, 0.965, 0.570, 0.020), Source = "{{copyright}}", Style = "copyright" },
            ],
        };
    }

    public static Dictionary<string, TextStyle> BuildPokemonTextStyles() => new(StringComparer.Ordinal)
    {
        ["pokemonName"] = new()
        {
            Font = "pokemon-name", SizePt = 14, Color = "#101010", MaxLines = 1,
            AutoFit = new AutoFitSettings { Mode = AutoFitMode.Condense, MinScaleX = 0.65, MinSizePt = 11 },
        },
        ["hp"] = new()
        {
            Font = "pokemon-hp", SizePt = 15, Color = "#D00000", Align = TextAlign.Right, VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["stage"] = new()
        {
            Font = "pokemon-stage", SizePt = 6.5, Color = "#202020", VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["evolvesFrom"] = new()
        {
            Font = "pokemon-flavor", SizePt = 5.0, Color = "#404040", VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["pokedex"] = new()
        {
            Font = "pokemon-flavor", SizePt = 4.6, Color = "#202020", Align = TextAlign.Center, VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["attackName"] = new()
        {
            Font = "pokemon-attack-name", SizePt = 11, Color = "#101010", VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["attackDamage"] = new()
        {
            Font = "pokemon-attack-damage", SizePt = 12, Color = "#101010", Align = TextAlign.Right, VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["attackEffect"] = new()
        {
            Font = "pokemon-body", SizePt = 6.8, Color = "#202020", LineHeight = 1.15,
        },
        ["footerStat"] = new()
        {
            Font = "pokemon-body", SizePt = 5.5, Color = "#202020", Align = TextAlign.Center, VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["flavorText"] = new()
        {
            Font = "pokemon-flavor", SizePt = 4.8, Color = "#303030", LineHeight = 1.15,
        },
        ["smallPrint"] = new()
        {
            Font = "pokemon-small", SizePt = 4.2, Color = "#202020", VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
        ["copyright"] = new()
        {
            Font = "pokemon-small", SizePt = 3.6, Color = "#202020", Align = TextAlign.Right, VerticalAlign = VerticalAlign.Middle, MaxLines = 1,
        },
    };

    // ---- Field factories ----

    private static FieldDefinition TextField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Text, SortOrder = order };

    private static FieldDefinition ImageField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Image, SortOrder = order };

    private static FieldDefinition IntegerField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Integer, SortOrder = order };

    private static FieldDefinition RichTextField(CardType ct, string key, string labelIt, string labelEn, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.RichText, SortOrder = order };

    private static FieldDefinition SymbolField(CardType ct, string key, string labelIt, string labelEn, SymbolSet set, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.SymbolRef, SymbolSetId = set.Id, SymbolSet = set, SortOrder = order };

    private static FieldDefinition EnumField(CardType ct, string key, string labelIt, string labelEn, OptionList list, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(labelIt, labelEn), Kind = FieldKind.Enum, OptionListId = list.Id, OptionList = list, SortOrder = order };

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

