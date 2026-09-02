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
}
