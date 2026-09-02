using System.Text.Json;
using CardMaker.Application.Content;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Content;
using CardMaker.Rendering;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Application.Tests;

public class YuGiOhSeedDataTests
{
    private sealed class StubResources : IRenderResources, IDisposable
    {
        private readonly List<SKImage> _created = [];

        public SKImage? GetImage(Guid assetId) => null;

        public SKImage? GetImageByKey(string assetKey) => MakeSolid(SKColors.DarkBlue);

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey) => MakeSolid(SKColors.Goldenrod);

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            isFallback = true;
            return FontRegistry.Fallback;
        }

        public SKImage MakeSolid(SKColor color, int size = 64)
        {
            var info = new SKImageInfo(size, size);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(color);
            var image = surface.Snapshot();
            _created.Add(image);
            return image;
        }

        public void Dispose()
        {
            foreach (var image in _created)
            {
                image.Dispose();
            }
        }
    }

    [Fact]
    public void CostruisceIlGrafoYuGiOhCompleto()
    {
        var graph = YuGiOhSeedData.Build();

        Assert.NotNull(graph.Game);
        Assert.Equal("yugioh", graph.Game.Key);
        Assert.Equal(59, graph.Game.WidthMm);
        Assert.Equal(86, graph.Game.HeightMm);
        Assert.Equal(2, graph.Game.CornerRadiusMm);
        Assert.Equal(2, graph.Game.BleedMm);
        Assert.Equal(3, graph.Game.SafeZoneMm);
        Assert.Equal(600, graph.Game.DefaultDpi);

        // Verifica CardTypes (18 monster + 3 spell/trap + token + maximum + skill + 2 back = 26)
        Assert.Equal(26, graph.CardTypes.Count);

        // Verifica SymbolSets
        Assert.Equal(5, graph.SymbolSets.Count);
        Assert.Contains(graph.SymbolSets, s => s.Key == "attributes");
        Assert.Contains(graph.SymbolSets, s => s.Key == "stars");
        Assert.Contains(graph.SymbolSets, s => s.Key == "link-arrows");
        Assert.Contains(graph.SymbolSets, s => s.Key == "spell-properties");
        Assert.Contains(graph.SymbolSets, s => s.Key == "trap-properties");

        // Verifica OptionLists
        Assert.Equal(4, graph.OptionLists.Count);
        Assert.Contains(graph.OptionLists, o => o.Key == "races");
        Assert.Contains(graph.OptionLists, o => o.Key == "rarities");
        Assert.Contains(graph.OptionLists, o => o.Key == "editions");
        Assert.Contains(graph.OptionLists, o => o.Key == "maximum-slice");

        // Verifica Traits
        Assert.Equal(6, graph.Traits.Count);
        Assert.Contains(graph.Traits, t => t.Key == "tuner");
        Assert.Contains(graph.Traits, t => t.Key == "flip");
        Assert.Contains(graph.Traits, t => t.Key == "union");
        Assert.Contains(graph.Traits, t => t.Key == "toon");
        Assert.Contains(graph.Traits, t => t.Key == "spirit");
        Assert.Contains(graph.Traits, t => t.Key == "gemini");
    }

    [Fact]
    public void TuttiILayoutDeiTemplateSonoValidi()
    {
        var graph = YuGiOhSeedData.Build();
        var templateCount = 0;

        foreach (var cardType in graph.CardTypes)
        {
            Assert.NotEmpty(cardType.Templates);
            foreach (var template in cardType.Templates)
            {
                templateCount++;
                Assert.NotEmpty(template.Versions);

                foreach (var version in template.Versions)
                {
                    Assert.Equal(TemplateStatus.Published, version.Status);
                    Assert.False(string.IsNullOrWhiteSpace(version.LayoutJson));

                    var layout = JsonSerializer.Deserialize<CardLayout>(version.LayoutJson, LayoutSerializer.Options);
                    Assert.NotNull(layout);

                    var validationResult = LayoutSerializer.Validate(layout);
                    Assert.True(
                        validationResult.IsValid,
                        $"Template '{template.Key}' ha errori di validazione: {string.Join(", ", validationResult.Issues.Select(i => i.Message))}");

                    // Verifica coordinate normalizzate comprese tra 0 e 1 (ADR-008)
                    foreach (var layer in layout.Layers)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(layer.Id));
                        Assert.InRange(layer.Rect.X, 0.0, 1.0);
                        Assert.InRange(layer.Rect.Y, 0.0, 1.0);
                        Assert.InRange(layer.Rect.Width, 0.0, 1.0);
                        Assert.InRange(layer.Rect.Height, 0.0, 1.0);
                    }
                }
            }
        }

        // 18 mostri + 3 magie/trappole + 1 token + 3 fette maximum + 1 skill + 2 back = 28 template totali
        Assert.Equal(28, templateCount);
    }

    [Fact]
    public void MaximumMonsterSelezionaTemplateInBaseAllaFetta()
    {
        var graph = YuGiOhSeedData.Build();
        var maximumCardType = graph.CardTypes.FirstOrDefault(c => c.Key == "rush-monster-maximum");
        Assert.NotNull(maximumCardType);
        Assert.Equal(3, maximumCardType.Templates.Count);

        var selector = new TemplateSelector();

        var leftValues = new Dictionary<string, CardValue> { ["maximumSlice"] = CardValue.FromText("left") };
        var centerValues = new Dictionary<string, CardValue> { ["maximumSlice"] = CardValue.FromText("center") };
        var rightValues = new Dictionary<string, CardValue> { ["maximumSlice"] = CardValue.FromText("right") };

        var selectedLeft = selector.SelectTemplate(maximumCardType.Templates, leftValues);
        var selectedCenter = selector.SelectTemplate(maximumCardType.Templates, centerValues);
        var selectedRight = selector.SelectTemplate(maximumCardType.Templates, rightValues);

        Assert.Equal("rush-monster-maximum-left-v1", selectedLeft?.Key);
        Assert.Equal("rush-monster-maximum-center-v1", selectedCenter?.Key);
        Assert.Equal("rush-monster-maximum-right-v1", selectedRight?.Key);
    }

    [Fact]
    public void TuttiITemplateSeedatiRenderizzanoSenzaErrori()
    {
        var graph = YuGiOhSeedData.Build();
        var renderer = new CardRenderer(new TextEngine());
        using var resources = new StubResources();

        var sampleValues = new Dictionary<string, CardValue>
        {
            ["name"] = CardValue.FromText("Drago Bianco Occhi Blu"),
            ["attribute"] = CardValue.FromText("light"),
            ["race"] = CardValue.FromText("dragon"),
            ["level"] = CardValue.FromNumber(8),
            ["rank"] = CardValue.FromNumber(4),
            ["atk"] = CardValue.FromNumber(3000),
            ["def"] = CardValue.FromNumber(2500),
            ["effectText"] = CardValue.FromText("Questo drago leggendario e' una potente macchina di distruzione."),
            ["pendulumScale"] = CardValue.FromNumber(1),
            ["pendulumEffectText"] = CardValue.FromText("Una volta per turno puoi evocare..."),
            ["setCode"] = CardValue.FromText("LOB-I001"),
            ["rarity"] = CardValue.FromText("ultra-rare"),
            ["edition"] = CardValue.FromText("first-edition"),
            ["property"] = CardValue.FromText("quick-play"),
            ["maximumAtk"] = CardValue.FromNumber(4000),
            ["maximumSlice"] = CardValue.FromText("center"),
            ["linkArrows"] = CardValue.FromList(["top", "bottom"]),
            ["description"] = CardValue.FromText("Segna-Mostro speciale."),
        };

        foreach (var cardType in graph.CardTypes)
        {
            foreach (var template in cardType.Templates)
            {
                foreach (var version in template.Versions)
                {
                    var layout = JsonSerializer.Deserialize<CardLayout>(version.LayoutJson, LayoutSerializer.Options);
                    Assert.NotNull(layout);

                    var request = new CardRenderRequest
                    {
                        Layout = layout,
                        Values = sampleValues,
                        Resources = resources,
                        Dpi = 96,
                        IncludeBleed = false,
                        Format = RenderOutputFormat.Png,
                    };

                    var result = renderer.Render(request);
                    Assert.NotNull(result);
                    Assert.Equal("image/png", result.ContentType);
                    Assert.True(result.WidthPx > 0);
                    Assert.True(result.HeightPx > 0);
                    Assert.NotEmpty(result.Content);
                }
            }
        }
    }
}
