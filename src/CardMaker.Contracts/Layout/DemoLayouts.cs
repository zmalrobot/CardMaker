using CardMaker.Contracts.Geometry;

namespace CardMaker.Contracts.Layout;

/// <summary>
/// Layout di prova costruito sulle regioni dei frame segnaposto. Serve a collaudare il motore
/// prima che esistano template reali: e' un <b>dato</b> come tutti gli altri, non un caso speciale.
/// </summary>
public static class DemoLayouts
{
    public const string FrameAssetKey = "placeholder-monster-effect";

    public static CardLayout YuGiOhMonster() => new()
    {
        Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
        TextStyles = new Dictionary<string, TextStyle>(StringComparer.Ordinal)
        {
            ["cardName"] = new()
            {
                Font = "card-name",
                SizePt = 13,
                Color = "#101010",
                Align = TextAlign.Left,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
                PaddingXPt = 2,
                // Il nome si comprime invece di rimpicciolirsi: e' il comportamento delle carte vere.
                AutoFit = new AutoFitSettings { Mode = AutoFitMode.Condense, MinScaleX = 0.55, MinSizePt = 13 },
            },
            ["typeLine"] = new()
            {
                Font = "type-line",
                SizePt = 5.5,
                Color = "#101010",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
                PaddingXPt = 2,
                AutoFit = new AutoFitSettings { Mode = AutoFitMode.Condense, MinScaleX = 0.6, MinSizePt = 5.5 },
            },
            ["effectText"] = new()
            {
                Font = "effect",
                SizePt = 6.5,
                Color = "#101010",
                Align = TextAlign.Left,
                VerticalAlign = VerticalAlign.Top,
                LineHeight = 1.12,
                PaddingXPt = 3,
                PaddingYPt = 1,
                AutoFit = new AutoFitSettings
                {
                    Mode = AutoFitMode.ShrinkAndCondense,
                    MinSizePt = 3.6,
                    MinScaleX = 0.75,
                    MinLineHeight = 0.95,
                },
            },
            ["atkDef"] = new()
            {
                Font = "atk-def-value",
                SizePt = 7,
                Color = "#101010",
                Align = TextAlign.Right,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
                PaddingXPt = 1,
                AutoFit = new AutoFitSettings { Mode = AutoFitMode.Shrink, MinSizePt = 5 },
            },
            ["smallPrint"] = new()
            {
                Font = "set-code",
                SizePt = 3.6,
                Color = "#101010",
                Align = TextAlign.Right,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
                AutoFit = new AutoFitSettings { Mode = AutoFitMode.Shrink, MinSizePt = 2.4 },
            },
            ["edition"] = new()
            {
                Font = "edition",
                SizePt = 3.6,
                Color = "#101010",
                Align = TextAlign.Left,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["passcode"] = new()
            {
                Font = "passcode",
                SizePt = 3.6,
                Color = "#101010",
                Align = TextAlign.Left,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["copyright"] = new()
            {
                Font = "copyright",
                SizePt = 3.2,
                Color = "#101010",
                Align = TextAlign.Right,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
        },
        Computed =
        [
            new ComputedField
            {
                Key = "typeLine",
                Expr = new ComputedExpression
                {
                    Op = ComputedOps.Join,
                    Separator = "/",
                    Prefix = "[",
                    Suffix = "]",
                    Args = ["{{race}}", "{{summonMethod}}", "{{abilities}}", "{{effectFlag}}"],
                },
            },
        ],
        Layers =
        [
            // L'artwork sta sotto: la finestra del frame e' trasparente e lo lascia trasparire.
            new ImageSlotLayer
            {
                Id = "artwork",
                Name = "Illustrazione",
                Z = 10,
                Rect = new NormalizedRect(0.115, 0.175, 0.770, 0.560),
                FieldKey = "artwork",
                Fit = ImageFit.Cover,
                MinSourceWidth = 900,
                MinSourceHeight = 900,
            },
            new StaticImageLayer
            {
                Id = "frame",
                Name = "Frame",
                Z = 20,
                Rect = new NormalizedRect(0, 0, 1, 1),
                FullBleed = true,
                AssetKey = FrameAssetKey,
                Fit = ImageFit.Stretch,
            },
            new SymbolSlotLayer
            {
                Id = "attribute",
                Name = "Attributo",
                Z = 30,
                Rect = new NormalizedRect(0.820, 0.033, 0.110, 0.072),
                SymbolSetKey = "attributes",
                FieldKey = "attribute",
            },
            new SymbolRepeaterLayer
            {
                Id = "stars",
                Name = "Livello",
                Z = 30,
                Rect = new NormalizedRect(0.100, 0.115, 0.800, 0.042),
                SymbolSetKey = "stars",
                SymbolKey = "level",
                FieldKey = "level",
                MaxCount = 12,
                Direction = RepeaterDirection.RightToLeft,
            },
            new TextLayer
            {
                Id = "name",
                Name = "Nome carta",
                Z = 30,
                Rect = new NormalizedRect(0.070, 0.038, 0.720, 0.058),
                Source = "{{name}}",
                Style = "cardName",
            },
            new TextLayer
            {
                Id = "edition",
                Name = "Edizione",
                Z = 30,
                Rect = new NormalizedRect(0.080, 0.718, 0.350, 0.022),
                Source = "{{edition}}",
                Style = "edition",
            },
            new TextLayer
            {
                Id = "set-code",
                Name = "Codice set",
                Z = 30,
                Rect = new NormalizedRect(0.550, 0.718, 0.360, 0.022),
                Source = "{{setCode}}",
                Style = "smallPrint",
            },
            new TextLayer
            {
                Id = "type-line",
                Name = "Type line",
                Z = 30,
                Rect = new NormalizedRect(0.080, 0.750, 0.840, 0.028),
                Source = "{{typeLine}}",
                Style = "typeLine",
            },
            new TextLayer
            {
                Id = "effect",
                Name = "Testo effetto",
                Z = 30,
                Rect = new NormalizedRect(0.070, 0.782, 0.860, 0.145),
                Source = "{{effectText}}",
                Style = "effectText",
            },
            new TextLayer
            {
                Id = "atk",
                Name = "ATK",
                Z = 30,
                Rect = new NormalizedRect(0.530, 0.932, 0.190, 0.026),
                Source = "ATK/{{atk}}",
                Style = "atkDef",
            },
            new TextLayer
            {
                Id = "def",
                Name = "DEF",
                Z = 30,
                Rect = new NormalizedRect(0.730, 0.932, 0.190, 0.026),
                Source = "DEF/{{def}}",
                Style = "atkDef",
                // I Link Monster non hanno DEF: il layer sparisce senza duplicare il template.
                VisibleWhen = Condition.Not(Condition.Equal("summonMethod", "Link")),
            },
            new TextLayer
            {
                Id = "passcode",
                Name = "Numero seriale / Passcode",
                Z = 30,
                Rect = new NormalizedRect(0.065, 0.962, 0.250, 0.018),
                Source = "{{passcode}}",
                Style = "passcode",
            },
            new TextLayer
            {
                Id = "copyright",
                Name = "Copyright",
                Z = 30,
                Rect = new NormalizedRect(0.400, 0.962, 0.520, 0.018),
                Source = "{{copyright}}",
                Style = "copyright",
            },
        ],
    };

    public static Dictionary<string, CardValue> SampleValues() => new(StringComparer.Ordinal)
    {
        ["name"] = CardValue.FromText("Drago Bianco Occhi Blu"),
        ["attribute"] = CardValue.FromText("light"),
        ["race"] = CardValue.FromText("Drago"),
        ["summonMethod"] = CardValue.FromText(string.Empty),
        ["abilities"] = CardValue.FromText(string.Empty),
        ["effectFlag"] = CardValue.FromText("Normale"),
        ["effectText"] = CardValue.FromText(
            "Questo leggendario drago è un potente motore di distruzione. "
            + "Praticamente invincibile, sono ben pochi coloro che hanno "
            + "affrontato questa magnifica creatura e sono sopravvissuti per raccontarlo."),
        ["atk"] = CardValue.FromNumber(3000),
        ["def"] = CardValue.FromNumber(2500),
        ["setCode"] = CardValue.FromText("LOB-IT001"),
        ["edition"] = CardValue.FromText("1ª EDIZIONE"),
        ["passcode"] = CardValue.FromText("89631139"),
        ["copyright"] = CardValue.FromText("©2020 Studio Dice/SHUEISHA, TV TOKYO, KONAMI"),
    };

    public const string PokemonFrameAssetKey = "placeholder-pokemon-frame-lightning";

    public static CardLayout PokemonBasic() => new()
    {
        Canvas = CanvasDefinition.FromGeometry(CardGeometry.PokerSize()),
        TextStyles = new Dictionary<string, TextStyle>(StringComparer.Ordinal)
        {
            ["pokemonName"] = new()
            {
                Font = "pokemon-name",
                SizePt = 14,
                Color = "#101010",
                Align = TextAlign.Left,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
                AutoFit = new AutoFitSettings { Mode = AutoFitMode.Condense, MinScaleX = 0.65, MinSizePt = 11 },
            },
            ["hp"] = new()
            {
                Font = "pokemon-hp",
                SizePt = 15,
                Color = "#D00000",
                Align = TextAlign.Right,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["stage"] = new()
            {
                Font = "pokemon-stage",
                SizePt = 6.5,
                Color = "#202020",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["pokedex"] = new()
            {
                Font = "pokemon-flavor",
                SizePt = 4.6,
                Color = "#202020",
                Align = TextAlign.Center,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["attackName"] = new()
            {
                Font = "pokemon-attack-name",
                SizePt = 11,
                Color = "#101010",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["attackDamage"] = new()
            {
                Font = "pokemon-attack-damage",
                SizePt = 12,
                Color = "#101010",
                Align = TextAlign.Right,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["attackEffect"] = new()
            {
                Font = "pokemon-body",
                SizePt = 6.8,
                Color = "#202020",
                LineHeight = 1.15,
            },
            ["footerStat"] = new()
            {
                Font = "pokemon-body",
                SizePt = 5.5,
                Color = "#202020",
                Align = TextAlign.Center,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["flavorText"] = new()
            {
                Font = "pokemon-flavor",
                SizePt = 4.8,
                Color = "#303030",
                LineHeight = 1.15,
            },
            ["smallPrint"] = new()
            {
                Font = "pokemon-small",
                SizePt = 4.2,
                Color = "#202020",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
        },
        Layers =
        [
            new ImageSlotLayer
            {
                Id = "artwork",
                Name = "Illustrazione",
                Z = 10,
                Rect = new NormalizedRect(0.075, 0.100, 0.850, 0.420),
                FieldKey = "artwork",
                Fit = ImageFit.Cover,
                MinSourceWidth = 900,
                MinSourceHeight = 900,
            },
            new StaticImageLayer
            {
                Id = "frame",
                Name = "Frame",
                Z = 20,
                Rect = new NormalizedRect(0, 0, 1, 1),
                FullBleed = true,
                AssetKey = PokemonFrameAssetKey,
                Fit = ImageFit.Stretch,
            },
            new TextLayer
            {
                Id = "stage",
                Name = "Fase",
                Z = 30,
                Rect = new NormalizedRect(0.075, 0.026, 0.120, 0.066),
                Source = "{{stage}}",
                Style = "stage",
            },
            new TextLayer
            {
                Id = "name",
                Name = "Nome Pokémon",
                Z = 30,
                Rect = new NormalizedRect(0.200, 0.026, 0.430, 0.066),
                Source = "{{name}}",
                Style = "pokemonName",
            },
            new TextLayer
            {
                Id = "hp",
                Name = "Punti Salute",
                Z = 30,
                Rect = new NormalizedRect(0.630, 0.026, 0.190, 0.066),
                Source = "HP {{hp}}",
                Style = "hp",
            },
            new SymbolSlotLayer
            {
                Id = "energy-type",
                Name = "Tipo Energia",
                Z = 30,
                Rect = new NormalizedRect(0.835, 0.035, 0.090, 0.048),
                SymbolSetKey = "pokemon-energy",
                FieldKey = "energyType",
            },
            new TextLayer
            {
                Id = "pokedex",
                Name = "Info Pokédex",
                Z = 30,
                Rect = new NormalizedRect(0.080, 0.525, 0.840, 0.026),
                Source = "{{pokedexEntry}}",
                Style = "pokedex",
            },
            new TextLayer
            {
                Id = "attack1-name",
                Name = "Nome Attacco 1",
                Z = 30,
                Rect = new NormalizedRect(0.180, 0.565, 0.520, 0.035),
                Source = "{{attack1Name}}",
                Style = "attackName",
            },
            new TextLayer
            {
                Id = "attack1-damage",
                Name = "Danno Attacco 1",
                Z = 30,
                Rect = new NormalizedRect(0.720, 0.565, 0.180, 0.035),
                Source = "{{attack1Damage}}",
                Style = "attackDamage",
            },
            new RichTextLayer
            {
                Id = "attack1-effect",
                Name = "Effetto Attacco 1",
                Z = 30,
                Rect = new NormalizedRect(0.105, 0.605, 0.790, 0.075),
                Source = "{{attack1Effect}}",
                Style = "attackEffect",
            },
            new TextLayer
            {
                Id = "attack2-name",
                Name = "Nome Attacco 2",
                Z = 30,
                Rect = new NormalizedRect(0.180, 0.690, 0.520, 0.035),
                Source = "{{attack2Name}}",
                Style = "attackName",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new TextLayer
            {
                Id = "attack2-damage",
                Name = "Danno Attacco 2",
                Z = 30,
                Rect = new NormalizedRect(0.720, 0.690, 0.180, 0.035),
                Source = "{{attack2Damage}}",
                Style = "attackDamage",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new RichTextLayer
            {
                Id = "attack2-effect",
                Name = "Effetto Attacco 2",
                Z = 30,
                Rect = new NormalizedRect(0.105, 0.730, 0.790, 0.075),
                Source = "{{attack2Effect}}",
                Style = "attackEffect",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new SymbolSlotLayer
            {
                Id = "weakness",
                Name = "Debolezza",
                Z = 30,
                Rect = new NormalizedRect(0.120, 0.885, 0.045, 0.045),
                SymbolSetKey = "pokemon-energy",
                FieldKey = "weakness",
            },
            new TextLayer
            {
                Id = "weakness-val",
                Name = "Valore Debolezza",
                Z = 30,
                Rect = new NormalizedRect(0.170, 0.885, 0.080, 0.045),
                Source = "{{weaknessValue}}",
                Style = "footerStat",
            },
            new TextLayer
            {
                Id = "retreat-label",
                Name = "Ritirata Label",
                Z = 30,
                Rect = new NormalizedRect(0.660, 0.885, 0.140, 0.045),
                Source = "Ritirata:",
                Style = "footerStat",
            },
            new TextLayer
            {
                Id = "retreat-cost",
                Name = "Costo Ritirata",
                Z = 30,
                Rect = new NormalizedRect(0.800, 0.885, 0.080, 0.045),
                Source = "{{retreatCost}}",
                Style = "footerStat",
            },
            new RichTextLayer
            {
                Id = "flavor",
                Name = "Descrizione Pokédex",
                Z = 30,
                Rect = new NormalizedRect(0.105, 0.815, 0.790, 0.060),
                Source = "{{flavorText}}",
                Style = "flavorText",
                VisibleWhen = Condition.Not(Condition.Equal("flavorText", "")),
            },
            new TextLayer
            {
                Id = "illustrator",
                Name = "Illustratore",
                Z = 30,
                Rect = new NormalizedRect(0.075, 0.940, 0.350, 0.022),
                Source = "{{illustrator}}",
                Style = "smallPrint",
            },
            new TextLayer
            {
                Id = "card-number",
                Name = "Numero Carta",
                Z = 30,
                Rect = new NormalizedRect(0.075, 0.965, 0.200, 0.022),
                Source = "{{cardNumber}}",
                Style = "smallPrint",
            },
            new SymbolSlotLayer
            {
                Id = "rarity",
                Name = "Rarità",
                Z = 30,
                Rect = new NormalizedRect(0.285, 0.965, 0.025, 0.022),
                SymbolSetKey = "pokemon-rarity",
                FieldKey = "rarity",
            },
            new TextLayer
            {
                Id = "copyright",
                Name = "Copyright",
                Z = 30,
                Rect = new NormalizedRect(0.350, 0.965, 0.575, 0.022),
                Source = "{{copyright}}",
                Style = "smallPrint",
            },
        ],
    };

    public static Dictionary<string, CardValue> PokemonSampleValues() => new(StringComparer.Ordinal)
    {
        ["name"] = CardValue.FromText("Pikachu"),
        ["stage"] = CardValue.FromText("BASE"),
        ["evolvesFrom"] = CardValue.FromText(string.Empty),
        ["hp"] = CardValue.FromNumber(60),
        ["energyType"] = CardValue.FromText("lightning"),
        ["pokedexEntry"] = CardValue.FromText("NO. 025  Pokémon Topo  Alt: 0.4 m  Peso: 6.0 kg"),
        ["attack1Name"] = CardValue.FromText("Carica Lampo"),
        ["attack1Damage"] = CardValue.FromText("10"),
        ["attack1Effect"] = CardValue.FromText("Lancia una moneta. Se esce testa, questo attacco infligge 10 danni in più."),
        ["attack2Name"] = CardValue.FromText("Fulmine"),
        ["attack2Damage"] = CardValue.FromText("50"),
        ["attack2Effect"] = CardValue.FromText("Scarta tutte le Energie assegnate a questo Pokémon."),
        ["weakness"] = CardValue.FromText("fighting"),
        ["weaknessValue"] = CardValue.FromText("×2"),
        ["retreatCost"] = CardValue.FromNumber(1),
        ["flavorText"] = CardValue.FromText("Quando si riuniscono molti di questi Pokémon, la loro elettricità può accumularsi e creare improvvisi temporali."),
        ["illustrator"] = CardValue.FromText("Illus. Ken Sugimori"),
        ["cardNumber"] = CardValue.FromText("025/102"),
        ["rarity"] = CardValue.FromText("common"),
        ["regulationMark"] = CardValue.FromText("G"),
        ["copyright"] = CardValue.FromText("©2024 Pokémon / Nintendo / Creatures / GAME FREAK"),
    };

    public const string MtgFrameAssetKey = "placeholder-mtg-frame-white";

    public static CardLayout MtgCreature() => new()
    {
        Canvas = CanvasDefinition.FromGeometry(CardGeometry.PokerSize()),
        TextStyles = new Dictionary<string, TextStyle>(StringComparer.Ordinal)
        {
            ["cardName"] = new()
            {
                Font = "mtg-name",
                SizePt = 11.5,
                Color = "#111111",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["manaCost"] = new()
            {
                Font = "mtg-name",
                SizePt = 10,
                Color = "#111111",
                Align = TextAlign.Right,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["typeLine"] = new()
            {
                Font = "mtg-type-line",
                SizePt = 9.5,
                Color = "#111111",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["rulesText"] = new()
            {
                Font = "mtg-rules",
                SizePt = 8.5,
                Color = "#111111",
                LineHeight = 1.15,
            },
            ["flavorText"] = new()
            {
                Font = "mtg-flavor",
                SizePt = 8.0,
                Color = "#333333",
                LineHeight = 1.15,
            },
            ["pt"] = new()
            {
                Font = "mtg-pt",
                SizePt = 12,
                Color = "#111111",
                Align = TextAlign.Center,
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
            ["smallPrint"] = new()
            {
                Font = "mtg-small",
                SizePt = 4.8,
                Color = "#333333",
                VerticalAlign = VerticalAlign.Middle,
                MaxLines = 1,
            },
        },
        Layers =
        [
            new ImageSlotLayer
            {
                Id = "artwork",
                Name = "Artwork",
                Z = 10,
                Rect = new NormalizedRect(0.075, 0.100, 0.850, 0.450),
                FieldKey = "artwork",
                Fit = ImageFit.Cover,
            },
            new StaticImageLayer
            {
                Id = "frame",
                Name = "Frame",
                Z = 20,
                Rect = new NormalizedRect(0, 0, 1, 1),
                FullBleed = true,
                AssetKey = MtgFrameAssetKey,
                Fit = ImageFit.Stretch,
            },
            new TextLayer
            {
                Id = "name",
                Name = "Nome Carta",
                Z = 30,
                Rect = new NormalizedRect(0.080, 0.034, 0.550, 0.058),
                Source = "{{name}}",
                Style = "cardName",
            },
            new TextLayer
            {
                Id = "mana-cost",
                Name = "Costo di Mana",
                Z = 30,
                Rect = new NormalizedRect(0.650, 0.034, 0.270, 0.058),
                Source = "{{manaCost}}",
                Style = "manaCost",
            },
            new TextLayer
            {
                Id = "type-line",
                Name = "Riga del Tipo",
                Z = 30,
                Rect = new NormalizedRect(0.080, 0.558, 0.750, 0.046),
                Source = "{{typeLine}}",
                Style = "typeLine",
            },
            new SymbolSlotLayer
            {
                Id = "rarity",
                Name = "Simbolo Rarità",
                Z = 30,
                Rect = new NormalizedRect(0.855, 0.564, 0.060, 0.034),
                SymbolSetKey = "mtg-rarity",
                FieldKey = "setRarity",
            },
            new RichTextLayer
            {
                Id = "rules",
                Name = "Testo Regole",
                Z = 30,
                Rect = new NormalizedRect(0.095, 0.622, 0.800, 0.155),
                Source = "{{rulesText}}",
                Style = "rulesText",
            },
            new RichTextLayer
            {
                Id = "flavor",
                Name = "Testo di Colore",
                Z = 30,
                Rect = new NormalizedRect(0.095, 0.785, 0.620, 0.075),
                Source = "{{flavorText}}",
                Style = "flavorText",
                VisibleWhen = Condition.Not(Condition.Equal("flavorText", "")),
            },
            new TextLayer
            {
                Id = "pt",
                Name = "Forza / Costituzione",
                Z = 30,
                Rect = new NormalizedRect(0.740, 0.865, 0.180, 0.048),
                Source = "{{power}}/{{toughness}}",
                Style = "pt",
            },
            new TextLayer
            {
                Id = "collector-number",
                Name = "Numero Collettore",
                Z = 30,
                Rect = new NormalizedRect(0.075, 0.935, 0.250, 0.022),
                Source = "{{collectorNumber}}",
                Style = "smallPrint",
            },
            new TextLayer
            {
                Id = "illustrator",
                Name = "Illustratore",
                Z = 30,
                Rect = new NormalizedRect(0.075, 0.960, 0.350, 0.022),
                Source = "Illus. {{illustrator}}",
                Style = "smallPrint",
            },
            new TextLayer
            {
                Id = "copyright",
                Name = "Copyright",
                Z = 30,
                Rect = new NormalizedRect(0.450, 0.960, 0.475, 0.022),
                Source = "{{copyright}}",
                Style = "smallPrint",
            },
        ],
    };

    public static Dictionary<string, CardValue> MtgSampleValues() => new(StringComparer.Ordinal)
    {
        ["name"] = CardValue.FromText("Angelo Serra"),
        ["manaCost"] = CardValue.FromText("{3}{W}{W}"),
        ["typeLine"] = CardValue.FromText("Creatura — Angelo"),
        ["rulesText"] = CardValue.FromText("Volare, cautela (Questa creatura attacca senza TAPpare.)"),
        ["flavorText"] = CardValue.FromText("Nata con ali di luce e una spada di vendetta, non conosce paura né perdono."),
        ["power"] = CardValue.FromText("4"),
        ["toughness"] = CardValue.FromText("4"),
        ["setRarity"] = CardValue.FromText("rare"),
        ["collectorNumber"] = CardValue.FromText("023/280"),
        ["illustrator"] = CardValue.FromText("Douglas Shuler"),
        ["copyright"] = CardValue.FromText("™ & © 2024 Wizards of the Coast"),
    };
}
