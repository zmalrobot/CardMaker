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

/// <summary>
/// Costruisce l'intero grafo di contenuti Yu-Gi-Oh! (Game, CardType, campi, template, set di
/// simboli, liste opzioni, traits) come dati in memoria, pronti per essere salvati dal seeder.
/// Nessuna logica di gioco nel motore (ADR-001): qui c'e' solo dato, riusa i layer di F1/F2 e le
/// regioni gia' definite per i frame segnaposto di F0.
/// </summary>
public static class YuGiOhSeedData
{
    public const string GameKey = "yugioh";

    private sealed record MonsterSpec(
        string Key, string Label, string FrameKey, PlaceholderLayout Layout,
        bool HasDefenseBox, bool HasStars, bool RankStyle, bool HasLinkArrows, bool HasPendulum, bool IsRush = false);

    private sealed record PropertySpec(string Key, string Label, string FrameKey, string SymbolSetKey, bool IsRush = false);

    public sealed record SeedGraph(
        Game Game,
        IReadOnlyList<CardType> CardTypes,
        IReadOnlyList<Trait> Traits,
        IReadOnlyList<OptionList> OptionLists,
        IReadOnlyList<SymbolSet> SymbolSets);

    private static readonly MonsterSpec[] MonsterSpecs =
    [
        new("monster-normal", "Normal Monster", "monster-normal", PlaceholderLayout.Monster, true, true, false, false, false),
        new("monster-effect", "Effect Monster", "monster-effect", PlaceholderLayout.Monster, true, true, false, false, false),
        new("monster-ritual", "Ritual Monster", "monster-ritual", PlaceholderLayout.Monster, true, true, false, false, false),
        new("monster-fusion", "Fusion Monster", "monster-fusion", PlaceholderLayout.Monster, true, true, false, false, false),
        new("monster-synchro", "Synchro Monster", "monster-synchro", PlaceholderLayout.Monster, true, true, false, false, false),
        new("monster-xyz", "Xyz Monster", "monster-xyz", PlaceholderLayout.Monster, true, true, true, false, false),
        new("monster-link", "Link Monster", "monster-link", PlaceholderLayout.Monster, false, false, false, true, false),
        new("monster-pendulum-normal", "Pendulum Normal Monster", "pendulum-effect", PlaceholderLayout.MonsterPendulum, true, true, false, false, true),
        new("monster-pendulum-effect", "Pendulum Effect Monster", "pendulum-effect", PlaceholderLayout.MonsterPendulum, true, true, false, false, true),
        new("monster-pendulum-ritual", "Pendulum Ritual Monster", "pendulum-effect", PlaceholderLayout.MonsterPendulum, true, true, false, false, true),
        new("monster-pendulum-fusion", "Pendulum Fusion Monster", "pendulum-effect", PlaceholderLayout.MonsterPendulum, true, true, false, false, true),
        new("monster-pendulum-synchro", "Pendulum Synchro Monster", "pendulum-effect", PlaceholderLayout.MonsterPendulum, true, true, false, false, true),
        new("monster-pendulum-xyz", "Pendulum Xyz Monster", "pendulum-effect", PlaceholderLayout.MonsterPendulum, true, true, true, false, true),
        new("rush-monster-normal", "Rush Normal Monster", "rush-monster-effect", PlaceholderLayout.Monster, true, true, false, false, false, IsRush: true),
        new("rush-monster-effect", "Rush Effect Monster", "rush-monster-effect", PlaceholderLayout.Monster, true, true, false, false, false, IsRush: true),
        new("rush-monster-ritual", "Rush Ritual Monster", "rush-monster-effect", PlaceholderLayout.Monster, true, true, false, false, false, IsRush: true),
        new("rush-monster-fusion", "Rush Fusion Monster", "rush-monster-effect", PlaceholderLayout.Monster, true, true, false, false, false, IsRush: true),
        new("rush-monster-synchro", "Rush Synchro Monster", "rush-monster-effect", PlaceholderLayout.Monster, true, true, false, false, false, IsRush: true),
    ];

    private static readonly PropertySpec[] PropertySpecs =
    [
        new("spell", "Spell Card", "spell", "spell-properties"),
        new("trap", "Trap Card", "trap", "trap-properties"),
        new("rush-spell", "Rush Spell Card", "rush-spell", "spell-properties", IsRush: true),
    ];

    /// <summary>Le 8 posizioni delle frecce Link, in coordinate relative al layer (F2, ADR-022).</summary>
    private static readonly (string Key, NormalizedRect Rect)[] LinkArrowPositions =
    [
        ("top", new NormalizedRect(0.40, 0.02, 0.20, 0.08)),
        ("topRight", new NormalizedRect(0.75, 0.08, 0.15, 0.10)),
        ("right", new NormalizedRect(0.85, 0.40, 0.10, 0.20)),
        ("bottomRight", new NormalizedRect(0.75, 0.75, 0.15, 0.10)),
        ("bottom", new NormalizedRect(0.40, 0.85, 0.20, 0.08)),
        ("bottomLeft", new NormalizedRect(0.10, 0.75, 0.15, 0.10)),
        ("left", new NormalizedRect(0.05, 0.40, 0.10, 0.20)),
        ("topLeft", new NormalizedRect(0.10, 0.08, 0.15, 0.10)),
    ];

    private static readonly string[] AttributeKeys = ["dark", "light", "water", "fire", "earth", "wind", "divine"];
    private static readonly string[] StarKeys = ["level", "rank"];
    private static readonly string[] LinkArrowSymbolKeys = ["on", "off"];
    private static readonly string[] SpellPropertyKeys = ["normal", "quick-play", "continuous", "equip", "field", "ritual"];
    private static readonly string[] TrapPropertyKeys = ["normal", "continuous", "counter"];
    private static readonly string[] RaceKeys =
    [
        "dragon", "spellcaster", "warrior", "zombie", "fiend", "fairy", "machine", "aqua",
        "pyro", "rock", "winged-beast", "plant", "insect", "thunder", "beast",
        "beast-warrior", "psychic", "reptile", "sea-serpent", "dinosaur", "wyrm", "cyberse",
    ];
    private static readonly string[] RarityKeys = ["common", "rare", "super-rare", "ultra-rare", "secret-rare", "ghost-rare"];
    private static readonly string[] EditionKeys = ["unlimited", "first-edition", "limited"];
    private static readonly string[] MaximumSliceKeys = ["left", "center", "right"];
    private static readonly string[] TraitKeys = ["tuner", "flip", "union", "toon", "spirit", "gemini"];

    public static SeedGraph Build()
    {
        var game = new Game
        {
            Key = GameKey,
            Name = LocalizedText.From("Yu-Gi-Oh!"),
            Description = LocalizedText.From("Yu-Gi-Oh! classico e Rush Duel."),
            WidthMm = 59,
            HeightMm = 86,
            CornerRadiusMm = 2,
            BleedMm = 2,
            SafeZoneMm = 3,
            DefaultDpi = 600,
            IsPublished = false,
        };

        var attributes = BuildSymbolSet(game, "attributes", "Attributi", AttributeKeys);
        var stars = BuildSymbolSet(game, "stars", "Stelle", StarKeys);
        var linkArrows = BuildSymbolSet(game, "link-arrows", "Frecce Link", LinkArrowSymbolKeys);
        var spellProperties = BuildSymbolSet(game, "spell-properties", "Proprieta' Magia", SpellPropertyKeys);
        var trapProperties = BuildSymbolSet(game, "trap-properties", "Proprieta' Trappola", TrapPropertyKeys);

        var races = BuildOptionList(game, "races", "Razze", RaceKeys);
        var rarities = BuildOptionList(game, "rarities", "Rarita'", RarityKeys);
        var editions = BuildOptionList(game, "editions", "Edizioni", EditionKeys);
        var maximumSlice = BuildOptionList(game, "maximum-slice", "Fetta Maximum", MaximumSliceKeys);

        var traits = TraitKeys
            .Select((key, i) => new Trait { GameId = game.Id, Game = game, Key = key, Name = LocalizedText.From(Capitalize(key)), Group = "ability", SortOrder = i })
            .ToList();

        var cardTypes = new List<CardType>();

        foreach (var spec in MonsterSpecs)
        {
            cardTypes.Add(BuildMonsterCardType(game, spec, attributes, linkArrows, races, rarities, editions, traits));
        }

        foreach (var spec in PropertySpecs)
        {
            var propertySet = spec.SymbolSetKey == "spell-properties" ? spellProperties : trapProperties;
            cardTypes.Add(BuildPropertyCardType(game, spec, propertySet, rarities, editions));
        }

        cardTypes.Add(BuildTokenCardType(game));
        cardTypes.Add(BuildMaximumCardType(game, attributes, races, rarities, editions, maximumSlice));
        cardTypes.Add(BuildSkillCardType(game));
        cardTypes.Add(BuildBackCardType(game, "card-back-classic", "Retro classico", "back-classic"));
        cardTypes.Add(BuildBackCardType(game, "rush-back", "Retro Rush Duel", "back-classic"));

        return new SeedGraph(
            game,
            cardTypes,
            traits,
            [races, rarities, editions, maximumSlice],
            [attributes, stars, linkArrows, spellProperties, trapProperties]);
    }

    // ---- CardType builders ----

    private static CardType BuildMonsterCardType(
        Game game, MonsterSpec spec, SymbolSet attributes, SymbolSet linkArrows,
        OptionList races, OptionList rarities, OptionList editions, IReadOnlyList<Trait> traits)
    {
        var cardType = new CardType { GameId = game.Id, Game = game, Key = spec.Key, Name = LocalizedText.From(spec.Label) };
        var fields = new List<FieldDefinition>();
        var order = 0;

        fields.Add(TextField(cardType, "name", "Nome", order++));
        fields.Add(ImageField(cardType, "artwork", "Illustrazione", order++));
        fields.Add(SymbolField(cardType, "attribute", "Attributo", attributes, order++));
        fields.Add(EnumField(cardType, "race", "Razza/Tipo", races, order++));

        if (spec.HasStars)
        {
            fields.Add(IntegerField(cardType, spec.RankStyle ? "rank" : "level", spec.RankStyle ? "Rank" : "Livello", order++));
        }

        if (spec.HasPendulum)
        {
            fields.Add(IntegerField(cardType, "pendulumScale", "Scala Pendulum", order++));
            fields.Add(RichTextField(cardType, "pendulumEffectText", "Testo Pendulum", order++));
        }

        fields.Add(RichTextField(cardType, "effectText", "Testo effetto", order++));
        fields.Add(IntegerField(cardType, "atk", "ATK", order++));

        if (spec.HasDefenseBox)
        {
            fields.Add(IntegerField(cardType, "def", "DEF", order++));
        }

        if (spec.HasLinkArrows)
        {
            fields.Add(ToggleSetField(cardType, "linkArrows", "Frecce Link", linkArrows, order++));
        }

        fields.Add(TextField(cardType, "setCode", "Codice set", order++));
        fields.Add(EnumField(cardType, "rarity", "Rarita'", rarities, order++));
        fields.Add(EnumField(cardType, "edition", "Edizione", editions, order++));
        cardType.Fields = fields;
        cardType.AllowedTraits = [.. traits.Select(t => new CardTypeTrait { CardTypeId = cardType.Id, CardType = cardType, TraitId = t.Id, Trait = t })];

        var regions = PlaceholderFrameGenerator.GetRegions(spec.Layout);
        var layout = BuildMonsterLayout(regions, spec);
        cardType.Templates = [SingleTemplate(cardType, spec.Key, spec.Label, CardFace.Front, layout)];

        return cardType;
    }

    private static CardType BuildPropertyCardType(Game game, PropertySpec spec, SymbolSet propertySet, OptionList rarities, OptionList editions)
    {
        var cardType = new CardType { GameId = game.Id, Game = game, Key = spec.Key, Name = LocalizedText.From(spec.Label) };
        cardType.Fields =
        [
            TextField(cardType, "name", "Nome", 0),
            ImageField(cardType, "artwork", "Illustrazione", 1),
            SymbolField(cardType, "property", "Proprieta'", propertySet, 2),
            RichTextField(cardType, "effectText", "Testo effetto", 3),
            TextField(cardType, "setCode", "Codice set", 4),
            EnumField(cardType, "rarity", "Rarita'", rarities, 5),
            EnumField(cardType, "edition", "Edizione", editions, 6),
        ];

        var regions = PlaceholderFrameGenerator.GetRegions(PlaceholderLayout.SpellTrap);
        var layout = BuildPropertyLayout(regions, spec);
        cardType.Templates = [SingleTemplate(cardType, spec.Key, spec.Label, CardFace.Front, layout)];

        return cardType;
    }

    private static CardType BuildTokenCardType(Game game)
    {
        var cardType = new CardType { GameId = game.Id, Game = game, Key = "token", Name = LocalizedText.From("Token") };
        cardType.Fields =
        [
            TextField(cardType, "name", "Nome", 0),
            ImageField(cardType, "artwork", "Illustrazione", 1),
            RichTextField(cardType, "description", "Descrizione", 2),
            IntegerField(cardType, "atk", "ATK", 3),
            IntegerField(cardType, "def", "DEF", 4),
        ];

        var regions = PlaceholderFrameGenerator.GetRegions(PlaceholderLayout.Monster);
        var textStyles = BuildTextStyles(string.Empty);
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            TextStyles = textStyles,
            Layers =
            [
                new ImageSlotLayer { Id = "artwork", Name = "Illustrazione", Z = 0, Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover },
                new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = "token", Fit = ImageFit.Stretch },
                new TextLayer { Id = "name", Name = "Nome", Z = 2, Rect = regions.NameBox, Source = "{{name}}", Style = "cardName" },
                new RichTextLayer { Id = "description", Name = "Descrizione", Z = 2, Rect = regions.EffectBox, Source = "{{description}}", Style = "effectText" },
                new TextLayer { Id = "atk", Name = "ATK", Z = 2, Rect = regions.AtkBox, Source = "ATK/{{atk}}", Style = "atkDef" },
                new TextLayer { Id = "def", Name = "DEF", Z = 2, Rect = regions.DefBox!.Value, Source = "DEF/{{def}}", Style = "atkDef" },
            ],
        };

        cardType.Templates = [SingleTemplate(cardType, "token", "Token", CardFace.Front, layout)];
        return cardType;
    }

    private static CardType BuildSkillCardType(Game game)
    {
        var cardType = new CardType { GameId = game.Id, Game = game, Key = "rush-skill", Name = LocalizedText.From("Rush Skill Card") };
        cardType.Fields =
        [
            TextField(cardType, "name", "Nome", 0),
            ImageField(cardType, "artwork", "Illustrazione", 1),
            RichTextField(cardType, "effectText", "Testo effetto", 2),
        ];

        var regions = PlaceholderFrameGenerator.GetRegions(PlaceholderLayout.SpellTrap);
        var textStyles = BuildTextStyles("rush-");
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            TextStyles = textStyles,
            Layers =
            [
                new ImageSlotLayer { Id = "artwork", Name = "Illustrazione", Z = 0, Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover },
                new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = "skill", Fit = ImageFit.Stretch },
                new TextLayer { Id = "name", Name = "Nome", Z = 2, Rect = regions.NameBox, Source = "{{name}}", Style = "cardName" },
                new RichTextLayer { Id = "effect", Name = "Testo effetto", Z = 2, Rect = regions.EffectBox, Source = "{{effectText}}", Style = "effectText" },
            ],
        };

        cardType.Templates = [SingleTemplate(cardType, "rush-skill", "Rush Skill Card", CardFace.Front, layout)];
        return cardType;
    }

    private static CardType BuildBackCardType(Game game, string key, string label, string frameKey)
    {
        var cardType = new CardType { GameId = game.Id, Game = game, Key = key, Name = LocalizedText.From(label) };
        cardType.Fields = [];

        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers = [new StaticImageLayer { Id = "back", Name = "Retro", Z = 0, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = frameKey, Fit = ImageFit.Stretch }],
        };

        cardType.Templates = [SingleTemplate(cardType, key, label, CardFace.Back, layout)];
        return cardType;
    }

    /// <summary>
    /// Un solo CardType con 3 template alternativi: la scelta usa <see cref="Template.SelectionRuleJson"/>
    /// sul campo <c>maximumSlice</c>, non tre CardType separati. Esercita davvero le regole di
    /// selezione dei template (checklist F3), non solo il crop a fetta di F2.
    /// </summary>
    private static CardType BuildMaximumCardType(Game game, SymbolSet attributes, OptionList races, OptionList rarities, OptionList editions, OptionList maximumSlice)
    {
        var cardType = new CardType { GameId = game.Id, Game = game, Key = "rush-monster-maximum", Name = LocalizedText.From("Rush Maximum Monster") };
        cardType.Fields =
        [
            TextField(cardType, "name", "Nome", 0),
            ImageField(cardType, "artwork", "Illustrazione panoramica", 1),
            SymbolField(cardType, "attribute", "Attributo", attributes, 2),
            EnumField(cardType, "race", "Razza/Tipo", races, 3),
            IntegerField(cardType, "level", "Livello", 4),
            RichTextField(cardType, "effectText", "Testo effetto", 5),
            IntegerField(cardType, "maximumAtk", "Maximum ATK", 6),
            EnumField(cardType, "maximumSlice", "Fetta da mostrare", maximumSlice, 7),
            TextField(cardType, "setCode", "Codice set", 8),
            EnumField(cardType, "rarity", "Rarita'", rarities, 9),
            EnumField(cardType, "edition", "Edizione", editions, 10),
        ];

        var regions = PlaceholderFrameGenerator.GetRegions(PlaceholderLayout.Monster);
        var textStyles = BuildTextStyles("rush-");
        var sliceNames = new[] { "left", "center", "right" };
        var templates = new List<Template>();

        for (var i = 0; i < sliceNames.Length; i++)
        {
            var layout = new CardLayout
            {
                Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
                TextStyles = textStyles,
                Layers =
                [
                    new ImageSlotLayer
                    {
                        Id = "artwork", Name = "Illustrazione", Z = 0, Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover,
                        SliceCount = 3, SliceIndex = i, SliceAxis = SliceAxis.Horizontal,
                    },
                    new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = "rush-monster-effect", Fit = ImageFit.Stretch },
                    new SymbolSlotLayer { Id = "attribute", Name = "Attributo", Z = 2, Rect = regions.AttributeBox, SymbolSetKey = "attributes", FieldKey = "attribute" },
                    new TextLayer { Id = "name", Name = "Nome", Z = 2, Rect = regions.NameBox, Source = "{{name}}", Style = "cardName" },
                    new RichTextLayer { Id = "effect", Name = "Testo effetto", Z = 2, Rect = regions.EffectBox, Source = "{{effectText}}", Style = "effectText" },
                    new TextLayer { Id = "atk", Name = "Maximum ATK", Z = 2, Rect = regions.AtkBox, Source = "MAXIMUM ATK/{{maximumAtk}}", Style = "atkDef" },
                ],
            };

            var template = new Template
            {
                CardTypeId = cardType.Id,
                CardType = cardType,
                Key = $"rush-monster-maximum-{sliceNames[i]}-v1",
                Name = LocalizedText.From($"Rush Maximum Monster ({sliceNames[i]})"),
                Face = CardFace.Front,
                IsDefault = i == 1,
                SortOrder = i,
                SelectionRuleJson = JsonSerializer.Serialize(Condition.Equal("maximumSlice", sliceNames[i]), LayoutSerializer.Options),
            };
            template.Versions = [PublishedVersion(template, layout)];
            templates.Add(template);
        }

        cardType.Templates = templates;
        return cardType;
    }

    // ---- Layout builders ----

    private static CardLayout BuildMonsterLayout(PlaceholderRegions regions, MonsterSpec spec)
    {
        var textStyles = BuildTextStyles(spec.IsRush ? "rush-" : string.Empty);
        var layers = new List<LayerDefinition>
        {
            new ImageSlotLayer { Id = "artwork", Name = "Illustrazione", Z = 0, Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover, MinSourceWidth = 900, MinSourceHeight = 900 },
            new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = spec.FrameKey, Fit = ImageFit.Stretch },
            new SymbolSlotLayer { Id = "attribute", Name = "Attributo", Z = 2, Rect = regions.AttributeBox, SymbolSetKey = "attributes", FieldKey = "attribute" },
        };

        if (spec.HasStars)
        {
            layers.Add(new SymbolRepeaterLayer
            {
                Id = "stars",
                Name = spec.RankStyle ? "Rank" : "Livello",
                Z = 2,
                Rect = regions.LevelStrip,
                SymbolSetKey = "stars",
                SymbolKey = spec.RankStyle ? "rank" : "level",
                FieldKey = spec.RankStyle ? "rank" : "level",
                MaxCount = 12,
                Direction = spec.RankStyle ? RepeaterDirection.LeftToRight : RepeaterDirection.RightToLeft,
            });
        }

        if (spec.HasLinkArrows)
        {
            layers.Add(new ToggleGroupLayer
            {
                Id = "linkArrows",
                Name = "Frecce Link",
                Z = 3,
                Rect = new NormalizedRect(0, 0, 1, 1),
                SymbolSetKey = "link-arrows",
                FieldKey = "linkArrows",
                OnSymbolKey = "on",
                OffSymbolKey = "off",
                Items = [.. LinkArrowPositions.Select(p => new ToggleItem { Key = p.Key, Rect = p.Rect })],
            });
        }

        layers.Add(new TextLayer { Id = "name", Name = "Nome carta", Z = 3, Rect = regions.NameBox, Source = "{{name}}", Style = "cardName" });
        layers.Add(new TextLayer { Id = "type-line", Name = "Type line", Z = 3, Rect = regions.TypeLineBox, Source = "[{{race}}]", Style = "typeLine" });

        if (spec.HasPendulum && regions.PendulumBox is { } pendulumBox)
        {
            layers.Add(new RichTextLayer { Id = "pendulum-effect", Name = "Testo Pendulum", Z = 3, Rect = pendulumBox, Source = "{{pendulumEffectText}}", Style = "effectText" });
        }

        layers.Add(new RichTextLayer { Id = "effect", Name = "Testo effetto", Z = 3, Rect = regions.EffectBox, Source = "{{effectText}}", Style = "effectText" });
        layers.Add(new TextLayer { Id = "atk", Name = "ATK", Z = 3, Rect = regions.AtkBox, Source = "ATK/{{atk}}", Style = "atkDef" });

        if (spec.HasDefenseBox && regions.DefBox is { } defBox)
        {
            layers.Add(new TextLayer { Id = "def", Name = "DEF", Z = 3, Rect = defBox, Source = "DEF/{{def}}", Style = "atkDef" });
        }

        layers.Add(new TextLayer { Id = "set-code", Name = "Codice set", Z = 3, Rect = new NormalizedRect(0.640, 0.163, 0.245, 0.020), Source = "{{setCode}}", Style = "smallPrint" });

        return new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            TextStyles = textStyles,
            Layers = layers,
        };
    }

    private static CardLayout BuildPropertyLayout(PlaceholderRegions regions, PropertySpec spec)
    {
        var textStyles = BuildTextStyles(spec.IsRush ? "rush-" : string.Empty);
        return new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            TextStyles = textStyles,
            Layers =
            [
                new ImageSlotLayer { Id = "artwork", Name = "Illustrazione", Z = 0, Rect = regions.ArtWindow, FieldKey = "artwork", Fit = ImageFit.Cover },
                new StaticImageLayer { Id = "frame", Name = "Frame", Z = 1, Rect = new NormalizedRect(0, 0, 1, 1), AssetKey = spec.FrameKey, Fit = ImageFit.Stretch },
                new SymbolSlotLayer { Id = "property", Name = "Proprieta'", Z = 2, Rect = regions.AttributeBox, SymbolSetKey = spec.SymbolSetKey, FieldKey = "property" },
                new TextLayer { Id = "name", Name = "Nome", Z = 2, Rect = regions.NameBox, Source = "{{name}}", Style = "cardName" },
                new RichTextLayer { Id = "effect", Name = "Testo effetto", Z = 2, Rect = regions.EffectBox, Source = "{{effectText}}", Style = "effectText" },
                new TextLayer { Id = "set-code", Name = "Codice set", Z = 2, Rect = new NormalizedRect(0.640, 0.163, 0.245, 0.020), Source = "{{setCode}}", Style = "smallPrint" },
            ],
        };
    }

    private static Dictionary<string, TextStyle> BuildTextStyles(string fontPrefix) => new(StringComparer.Ordinal)
    {
        ["cardName"] = new()
        {
            Font = fontPrefix + "card-name", SizePt = 13, Color = "#101010", MaxLines = 1, PaddingXPt = 2,
            AutoFit = new AutoFitSettings { Mode = AutoFitMode.Condense, MinScaleX = 0.55, MinSizePt = 13 },
        },
        ["typeLine"] = new()
        {
            Font = fontPrefix + "type-line", SizePt = 5.5, Color = "#101010", MaxLines = 1, PaddingXPt = 2,
            AutoFit = new AutoFitSettings { Mode = AutoFitMode.Condense, MinScaleX = 0.6, MinSizePt = 5.5 },
        },
        ["effectText"] = new() { Font = fontPrefix + "effect", SizePt = 6.5, Color = "#101010", LineHeight = 1.12, PaddingXPt = 3 },
        ["atkDef"] = new() { Font = "atk-def-value", SizePt = 7, Color = "#101010", Align = TextAlign.Right, VerticalAlign = VerticalAlign.Middle, MaxLines = 1 },
        ["smallPrint"] = new() { Font = "set-code", SizePt = 3.4, Color = "#202020", Align = TextAlign.Right, VerticalAlign = VerticalAlign.Middle, MaxLines = 1 },
    };

    // ---- Field factories ----

    private static FieldDefinition TextField(CardType ct, string key, string label, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.Text, SortOrder = order };

    private static FieldDefinition ImageField(CardType ct, string key, string label, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.Image, SortOrder = order };

    private static FieldDefinition IntegerField(CardType ct, string key, string label, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.Integer, SortOrder = order };

    private static FieldDefinition RichTextField(CardType ct, string key, string label, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.RichText, SortOrder = order };

    private static FieldDefinition SymbolField(CardType ct, string key, string label, SymbolSet set, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.SymbolRef, SymbolSetId = set.Id, SymbolSet = set, SortOrder = order };

    private static FieldDefinition ToggleSetField(CardType ct, string key, string label, SymbolSet set, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.ToggleSet, SymbolSetId = set.Id, SymbolSet = set, SortOrder = order };

    private static FieldDefinition EnumField(CardType ct, string key, string label, OptionList list, int order) => new()
    { CardTypeId = ct.Id, CardType = ct, Key = key, Label = LocalizedText.From(label), Kind = FieldKind.Enum, OptionListId = list.Id, OptionList = list, SortOrder = order };

    // ---- Template / helpers ----

    private static Template SingleTemplate(CardType cardType, string key, string label, CardFace face, CardLayout layout)
    {
        var template = new Template
        {
            CardTypeId = cardType.Id,
            CardType = cardType,
            Key = key + "-v1",
            Name = LocalizedText.From(label),
            Face = face,
            IsDefault = true,
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

    private static SymbolSet BuildSymbolSet(Game game, string key, string name, IReadOnlyList<string> symbolKeys)
    {
        var set = new SymbolSet { GameId = game.Id, Game = game, Key = key, Name = LocalizedText.From(name) };
        set.Symbols = [.. symbolKeys.Select((k, i) => new Symbol
        {
            SymbolSetId = set.Id,
            SymbolSet = set,
            Key = k,
            Name = LocalizedText.From(Capitalize(k)),
            InlineToken = $"{{sym:{key}.{k}}}",
            SortOrder = i,
        })];
        return set;
    }

    private static OptionList BuildOptionList(Game game, string key, string name, IReadOnlyList<string> itemKeys)
    {
        var list = new OptionList { GameId = game.Id, Game = game, Key = key, Name = LocalizedText.From(name) };
        list.Items = [.. itemKeys.Select((k, i) => new OptionItem
        {
            OptionListId = list.Id,
            OptionList = list,
            Key = k,
            Label = LocalizedText.From(Capitalize(k.Replace('-', ' '))),
            SortOrder = i,
        })];
        return list;
    }

    private static string Capitalize(string key) =>
        key.Length == 0 ? key : char.ToUpperInvariant(key[0]) + key[1..];
}
