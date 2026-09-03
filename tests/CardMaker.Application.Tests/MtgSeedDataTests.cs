using System.Text.Json;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Storage;
using CardMaker.Rendering;
using CardMaker.Rendering.Fonts;
using CardMaker.Rendering.Placeholders;
using CardMaker.Rendering.Text;
using SkiaSharp;

namespace CardMaker.Application.Tests;

public class MtgSeedDataTests
{
    private sealed class StubResources : IRenderResources, IDisposable
    {
        private readonly List<SKImage> _created = [];
        private readonly PlaceholderFrameGenerator _frameGen = new();
        private readonly PlaceholderSymbolGenerator _symGen = new();

        public SKImage? GetImage(Guid assetId) => null;

        public SKImage? GetImageByKey(string assetKey)
        {
            if (assetKey.StartsWith("placeholder-", StringComparison.Ordinal))
            {
                var key = assetKey["placeholder-".Length..];
                var spec = PlaceholderFrameSpec.MtgSet().FirstOrDefault(s => s.Key == key)
                    ?? PlaceholderFrameSpec.MtgSet()[0];
                var png = _frameGen.Generate(spec, CardGeometry.PokerSize(150));
                using var data = SKData.CreateCopy(png);
                var img = SKImage.FromEncodedData(data);
                if (img is not null)
                {
                    _created.Add(img);
                }
                return img;
            }
            return MakeSolid(SKColors.DarkRed);
        }

        public SKImage? GetSymbol(string symbolSetKey, string symbolKey)
        {
            var png = _symGen.Generate(symbolSetKey, symbolKey, 128);
            using var data = SKData.CreateCopy(png);
            var img = SKImage.FromEncodedData(data);
            if (img is not null)
            {
                _created.Add(img);
            }
            return img;
        }

        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            if (!string.IsNullOrEmpty(roleAlias))
            {
                var bytes = FontService.GetEmbeddedFontBytes(roleAlias);
                if (bytes is not null)
                {
                    using var data = SKData.CreateCopy(bytes);
                    var tf = SKTypeface.FromData(data);
                    if (tf is not null)
                    {
                        isFallback = false;
                        return tf;
                    }
                }
            }
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
    public void CostruisceIlGrafoMtgCompleto()
    {
        var graph = MtgSeedData.Build();

        Assert.NotNull(graph.Game);
        Assert.Equal("mtg", graph.Game.Key);
        Assert.Equal(63, graph.Game.WidthMm);
        Assert.Equal(88, graph.Game.HeightMm);
        Assert.Equal(2, graph.Game.BleedMm);

        // 7 card types
        Assert.Equal(7, graph.CardTypes.Count);
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-creature");
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-instant");
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-sorcery");
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-enchantment");
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-artifact");
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-planeswalker");
        Assert.Contains(graph.CardTypes, c => c.Key == "mtg-land");

        // SymbolSets
        Assert.Equal(2, graph.SymbolSets.Count);
        var manaSet = Assert.Single(graph.SymbolSets, s => s.Key == "mtg-mana");
        Assert.Equal(18, manaSet.Symbols.Count);
        Assert.Contains(manaSet.Symbols, s => s.Key == "w");
        Assert.Contains(manaSet.Symbols, s => s.Key == "u");
        Assert.Contains(manaSet.Symbols, s => s.Key == "b");
        Assert.Contains(manaSet.Symbols, s => s.Key == "r");
        Assert.Contains(manaSet.Symbols, s => s.Key == "g");
        Assert.Contains(manaSet.Symbols, s => s.Key == "c");
        Assert.Contains(manaSet.Symbols, s => s.Key == "tap");

        var raritySet = Assert.Single(graph.SymbolSets, s => s.Key == "mtg-rarity");
        Assert.Equal(4, raritySet.Symbols.Count);
        Assert.Contains(raritySet.Symbols, s => s.Key == "common");
        Assert.Contains(raritySet.Symbols, s => s.Key == "uncommon");
        Assert.Contains(raritySet.Symbols, s => s.Key == "rare");
        Assert.Contains(raritySet.Symbols, s => s.Key == "mythic");

        // OptionLists
        Assert.Equal(2, graph.OptionLists.Count);
        Assert.Contains(graph.OptionLists, o => o.Key == "mtg-colors");
        Assert.Contains(graph.OptionLists, o => o.Key == "mtg-rarities");

        // Traits
        Assert.Equal(6, graph.Traits.Count);
        Assert.Contains(graph.Traits, t => t.Key == "legendary");
        Assert.Contains(graph.Traits, t => t.Key == "snow");
    }

    [Fact]
    public void TuttiILayoutDeiTemplateMtgSonoValidi()
    {
        var graph = MtgSeedData.Build();
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

                    var layout = LayoutSerializer.Deserialize(version.LayoutJson!);
                    Assert.NotNull(layout);
                    Assert.NotEmpty(layout.Layers);
                    Assert.NotEmpty(layout.TextStyles);
                    Assert.Equal(63, layout.Canvas.WidthMm);
                    Assert.Equal(88, layout.Canvas.HeightMm);
                }
            }
        }

        Assert.Equal(7, templateCount);
    }

    [Fact]
    public void RenderizzaDemoMtgSenzaErrori()
    {
        var layout = DemoLayouts.MtgCreature();
        var values = DemoLayouts.MtgSampleValues();
        using var resources = new StubResources();
        var renderer = new CardRenderer(new TextEngine());

        var request = new CardRenderRequest
        {
            Layout = layout,
            Values = values,
            Resources = resources,
            Dpi = 150,
            IncludeBleed = false,
            Format = RenderOutputFormat.Png,
        };

        var result = renderer.Render(request);

        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        Assert.True(result.WidthPx > 0);
        Assert.True(result.HeightPx > 0);
        Assert.NotEmpty(result.Content);

        File.WriteAllBytes("/tmp/rendered_mtg_card.png", result.Content);
    }

    [Fact]
    public void GeneratoreSimboliProduceSimboliMtg()
    {
        var generator = new PlaceholderSymbolGenerator();

        var white = generator.Generate("mtg-mana", "w");
        Assert.NotNull(white);
        Assert.True(white.Length > 0);

        var blue = generator.Generate("mtg-mana", "u");
        Assert.NotNull(blue);
        Assert.True(blue.Length > 0);

        var black = generator.Generate("mtg-mana", "b");
        Assert.NotNull(black);
        Assert.True(black.Length > 0);

        var red = generator.Generate("mtg-mana", "r");
        Assert.NotNull(red);
        Assert.True(red.Length > 0);

        var green = generator.Generate("mtg-mana", "g");
        Assert.NotNull(green);
        Assert.True(green.Length > 0);

        var colorless = generator.Generate("mtg-mana", "c");
        Assert.NotNull(colorless);
        Assert.True(colorless.Length > 0);

        var tap = generator.Generate("mtg-mana", "tap");
        Assert.NotNull(tap);
        Assert.True(tap.Length > 0);

        var mythic = generator.Generate("mtg-rarity", "mythic");
        Assert.NotNull(mythic);
        Assert.True(mythic.Length > 0);
    }

    [Fact]
    public void GeneratoreFrameProduceFrameMtg()
    {
        var generator = new PlaceholderFrameGenerator();
        var geometry = CardGeometry.PokerSize();

        foreach (var spec in PlaceholderFrameSpec.MtgSet())
        {
            var png = generator.Generate(spec, geometry);
            Assert.NotNull(png);
            Assert.True(png.Length > 0);
        }
    }
}

