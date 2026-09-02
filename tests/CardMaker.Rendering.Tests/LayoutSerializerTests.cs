using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;

namespace CardMaker.Rendering.Tests;

public class LayoutSerializerTests
{
    [Fact]
    public void IlLayoutDimostrativoSopravviveAlRoundTrip()
    {
        var original = DemoLayouts.YuGiOhMonster();

        var json = LayoutSerializer.Serialize(original);
        var restored = LayoutSerializer.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Layers.Count, restored.Layers.Count);
        Assert.Equal(original.TextStyles.Count, restored.TextStyles.Count);
        Assert.Equal(LayoutSerializer.Serialize(original), LayoutSerializer.Serialize(restored));
    }

    [Fact]
    public void ITipiDiLayerVengonoRicostruitiCorrettamente()
    {
        var json = LayoutSerializer.Serialize(DemoLayouts.YuGiOhMonster());

        var restored = LayoutSerializer.Deserialize(json)!;

        Assert.Contains(restored.Layers, l => l is StaticImageLayer);
        Assert.Contains(restored.Layers, l => l is ImageSlotLayer);
        Assert.Contains(restored.Layers, l => l is SymbolSlotLayer);
        Assert.Contains(restored.Layers, l => l is TextLayer);
    }

    [Fact]
    public void LeCondizioniAnnidateSopravvivonoAlRoundTrip()
    {
        var restored = LayoutSerializer.Deserialize(LayoutSerializer.Serialize(DemoLayouts.YuGiOhMonster()))!;

        var def = restored.Layers.OfType<TextLayer>().Single(l => l.Id == "def");
        Assert.NotNull(def.VisibleWhen);
        Assert.Equal(ConditionOps.Not, def.VisibleWhen.Op);
        Assert.Equal(ConditionOps.Equal, def.VisibleWhen.Args![0].Op);
    }

    [Fact]
    public void IlLayoutDimostrativoEValido()
    {
        var result = LayoutSerializer.Validate(DemoLayouts.YuGiOhMonster());

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void UnoStileDiTestoInesistenteVieneSegnalato()
    {
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers = [new TextLayer { Id = "x", Rect = new NormalizedRect(0, 0, 1, 0.1), Source = "{{name}}", Style = "inesistente" }],
        };

        var result = LayoutSerializer.Validate(layout);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "layer.unknownTextStyle");
    }

    [Fact]
    public void GliIdDuplicatiVengonoSegnalati()
    {
        var layout = new CardLayout
        {
            Layers =
            [
                new ShapeLayer { Id = "stesso", Rect = new NormalizedRect(0, 0, 1, 1) },
                new ShapeLayer { Id = "stesso", Rect = new NormalizedRect(0, 0, 1, 1) },
            ],
        };

        var result = LayoutSerializer.Validate(layout);

        Assert.Contains(result.Issues, i => i.Code == "layer.duplicateId");
    }

    [Fact]
    public void UnOperatoreDiCondizioneSconosciutoVieneSegnalato()
    {
        var layout = new CardLayout
        {
            Layers =
            [
                new ShapeLayer
                {
                    Id = "x",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    VisibleWhen = new Condition { Op = "shellExec", Field = "name" },
                },
            ],
        };

        var result = LayoutSerializer.Validate(layout);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "condition.unknownOp");
    }

    [Fact]
    public void UnJsonNonDeserializzabileNonEValido()
    {
        var result = LayoutSerializer.Validate(null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "layout.missing");
    }

    [Fact]
    public void IGruppiVengonoAttraversatiDallEnumerazione()
    {
        var layout = new CardLayout
        {
            Layers =
            [
                new GroupLayer
                {
                    Id = "gruppo",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    Children = [new ShapeLayer { Id = "figlio", Rect = new NormalizedRect(0, 0, 0.5, 0.5) }],
                },
            ],
        };

        var ids = layout.EnumerateLayers().Select(l => l.Id).ToList();

        Assert.Equal(["gruppo", "figlio"], ids);
    }

    [Fact]
    public void SymbolRepeaterEToggleGroupSopravvivonoAlRoundTrip()
    {
        var layout = new CardLayout
        {
            Canvas = CanvasDefinition.FromGeometry(CardGeometry.YuGiOh()),
            Layers =
            [
                new SymbolRepeaterLayer
                {
                    Id = "level",
                    Rect = new NormalizedRect(0, 0, 1, 0.1),
                    SymbolSetKey = "stars",
                    SymbolKey = "level",
                    FieldKey = "level",
                    Direction = RepeaterDirection.RightToLeft,
                },
                new ToggleGroupLayer
                {
                    Id = "arrows",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    SymbolSetKey = "link-arrows",
                    FieldKey = "linkArrows",
                    OnSymbolKey = "on",
                    OffSymbolKey = "off",
                    Items = [new ToggleItem { Key = "top", Rect = new NormalizedRect(0.4, 0, 0.2, 0.2) }],
                },
            ],
        };

        var restored = LayoutSerializer.Deserialize(LayoutSerializer.Serialize(layout))!;

        var repeater = Assert.IsType<SymbolRepeaterLayer>(restored.Layers.Single(l => l.Id == "level"));
        Assert.Equal(RepeaterDirection.RightToLeft, repeater.Direction);
        Assert.Equal("level", repeater.FieldKey);

        var toggle = Assert.IsType<ToggleGroupLayer>(restored.Layers.Single(l => l.Id == "arrows"));
        Assert.Equal("on", toggle.OnSymbolKey);
        Assert.Single(toggle.Items);
        Assert.Equal("top", toggle.Items[0].Key);
    }

    [Fact]
    public void UnMassimoDiPosizioniNonPositivoVieneSegnalato()
    {
        var layout = new CardLayout
        {
            Layers =
            [
                new SymbolRepeaterLayer
                {
                    Id = "x",
                    Rect = new NormalizedRect(0, 0, 1, 0.1),
                    SymbolSetKey = "stars",
                    SymbolKey = "level",
                    MaxCount = 0,
                },
            ],
        };

        var result = LayoutSerializer.Validate(layout);

        Assert.Contains(result.Issues, i => i.Code == "layer.invalidMaxCount");
    }

    [Fact]
    public void UnGruppoDiStatoSenzaPosizioniVieneSegnalato()
    {
        var layout = new CardLayout
        {
            Layers =
            [
                new ToggleGroupLayer
                {
                    Id = "x",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    SymbolSetKey = "link-arrows",
                    FieldKey = "linkArrows",
                    OnSymbolKey = "on",
                },
            ],
        };

        var result = LayoutSerializer.Validate(layout);

        Assert.Contains(result.Issues, i => i.Code == "layer.toggleGroupWithoutItems");
    }

    [Fact]
    public void IGruppiTrasportanoLeCondizioniDeiNuoviTipiDiLayerF2()
    {
        var layout = new CardLayout
        {
            Layers =
            [
                new GroupLayer
                {
                    Id = "gruppo",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    Children =
                    [
                        new SymbolRepeaterLayer
                        {
                            Id = "level", Rect = new NormalizedRect(0, 0, 1, 0.1),
                            SymbolSetKey = "stars", SymbolKey = "level", Count = 1,
                            VisibleWhen = Condition.Equal("kind", "monster"),
                        },
                        new RichTextLayer
                        {
                            Id = "effect", Rect = new NormalizedRect(0, 0.2, 1, 0.5), Source = "{{text}}",
                            VisibleWhen = Condition.Not(Condition.Equal("kind", "monster")),
                        },
                    ],
                },
            ],
        };

        var ids = layout.EnumerateLayers().Select(l => l.Id).ToList();

        // L'enumerazione attraversa il gruppo indipendentemente dalle condizioni: sono valutate in fase di render.
        Assert.Equal(["gruppo", "level", "effect"], ids);
        Assert.NotNull(layout.Layers.OfType<GroupLayer>().Single().Children.OfType<SymbolRepeaterLayer>().Single().VisibleWhen);
        Assert.NotNull(layout.Layers.OfType<GroupLayer>().Single().Children.OfType<RichTextLayer>().Single().VisibleWhen);
    }
}

