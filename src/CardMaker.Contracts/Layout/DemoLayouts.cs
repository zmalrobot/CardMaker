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
                Rect = new NormalizedRect(0.080, 0.734, 0.350, 0.016),
                Source = "{{edition}}",
                Style = "edition",
            },
            new TextLayer
            {
                Id = "set-code",
                Name = "Codice set",
                Z = 30,
                Rect = new NormalizedRect(0.550, 0.734, 0.360, 0.016),
                Source = "{{setCode}}",
                Style = "smallPrint",
            },
            new TextLayer
            {
                Id = "type-line",
                Name = "Type line",
                Z = 30,
                Rect = new NormalizedRect(0.070, 0.752, 0.860, 0.028),
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
                AssetKey = PokemonFrameAssetKey,
                Fit = ImageFit.Stretch,
            },
            new TextLayer
            {
                Id = "stage",
                Name = "Fase",
                Z = 30,
                Rect = new NormalizedRect(0.075, 0.035, 0.120, 0.050),
                Source = "{{stage}}",
                Style = "stage",
            },
            new TextLayer
            {
                Id = "name",
                Name = "Nome Pokémon",
                Z = 30,
                Rect = new NormalizedRect(0.200, 0.035, 0.430, 0.050),
                Source = "{{name}}",
                Style = "pokemonName",
            },
            new TextLayer
            {
                Id = "hp",
                Name = "Punti Salute",
                Z = 30,
                Rect = new NormalizedRect(0.630, 0.035, 0.190, 0.050),
                Source = "HP {{hp}}",
                Style = "hp",
            },
            new SymbolSlotLayer
            {
                Id = "energy-type",
                Name = "Tipo Energia",
                Z = 30,
                Rect = new NormalizedRect(0.835, 0.035, 0.090, 0.050),
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
                Rect = new NormalizedRect(0.100, 0.605, 0.800, 0.075),
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
                Rect = new NormalizedRect(0.100, 0.730, 0.800, 0.075),
                Source = "{{attack2Effect}}",
                Style = "attackEffect",
                VisibleWhen = Condition.Not(Condition.Equal("attack2Name", "")),
            },
            new SymbolSlotLayer
            {
                Id = "weakness",
                Name = "Debolezza",
                Z = 30,
                Rect = new NormalizedRect(0.120, 0.888, 0.045, 0.038),
                SymbolSetKey = "pokemon-energy",
                FieldKey = "weakness",
            },
            new TextLayer
            {
                Id = "weakness-val",
                Name = "Valore Debolezza",
                Z = 30,
                Rect = new NormalizedRect(0.170, 0.888, 0.080, 0.038),
                Source = "{{weaknessValue}}",
                Style = "footerStat",
            },
            new TextLayer
            {
                Id = "retreat-label",
                Name = "Ritirata Label",
                Z = 30,
                Rect = new NormalizedRect(0.660, 0.888, 0.140, 0.038),
                Source = "Ritirata:",
                Style = "footerStat",
            },
            new TextLayer
            {
                Id = "retreat-cost",
                Name = "Costo Ritirata",
                Z = 30,
                Rect = new NormalizedRect(0.800, 0.888, 0.100, 0.038),
                Source = "{{retreatCost}}",
                Style = "footerStat",
            },
            new RichTextLayer
            {
                Id = "flavor",
                Name = "Descrizione Pokédex",
                Z = 30,
                Rect = new NormalizedRect(0.100, 0.815, 0.800, 0.065),
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
}
