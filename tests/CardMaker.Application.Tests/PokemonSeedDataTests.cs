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

public class PokemonSeedDataTests
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
                var spec = PlaceholderFrameSpec.PokemonSet().FirstOrDefault(s => s.Key == key)
                    ?? PlaceholderFrameSpec.PokemonSet()[3];
                var png = _frameGen.Generate(spec, CardGeometry.PokerSize(150));
                using var data = SKData.CreateCopy(png);
                var img = SKImage.FromEncodedData(data);
                if (img is not null)
                {
                    _created.Add(img);
                }
                return img;
            }
            return MakeSolid(SKColors.DarkGreen);
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
    public void CostruisceIlGrafoPokemonCompleto()
    {
        var graph = PokemonSeedData.Build();

        Assert.NotNull(graph.Game);
        Assert.Equal("pokemon", graph.Game.Key);
        Assert.Equal(63, graph.Game.WidthMm);
        Assert.Equal(88, graph.Game.HeightMm);
        Assert.Equal(2, graph.Game.BleedMm);

        // 3 pokemon monster + 3 trainer + 2 energy = 8
        Assert.Equal(8, graph.CardTypes.Count);
        Assert.Contains(graph.CardTypes, c => c.Key == "pokemon-basic");
        Assert.Contains(graph.CardTypes, c => c.Key == "pokemon-stage1");
        Assert.Contains(graph.CardTypes, c => c.Key == "pokemon-stage2");
        Assert.Contains(graph.CardTypes, c => c.Key == "trainer-item");
        Assert.Contains(graph.CardTypes, c => c.Key == "trainer-supporter");
        Assert.Contains(graph.CardTypes, c => c.Key == "trainer-stadium");
        Assert.Contains(graph.CardTypes, c => c.Key == "energy-basic");
        Assert.Contains(graph.CardTypes, c => c.Key == "energy-special");

        // SymbolSets
        Assert.Equal(2, graph.SymbolSets.Count);
        var energySet = Assert.Single(graph.SymbolSets, s => s.Key == "pokemon-energy");
        Assert.Equal(11, energySet.Symbols.Count);
        Assert.Contains(energySet.Symbols, s => s.Key == "grass");
        Assert.Contains(energySet.Symbols, s => s.Key == "fire");
        Assert.Contains(energySet.Symbols, s => s.Key == "water");
        Assert.Contains(energySet.Symbols, s => s.Key == "lightning");
        Assert.Contains(energySet.Symbols, s => s.Key == "psychic");
        Assert.Contains(energySet.Symbols, s => s.Key == "fighting");
        Assert.Contains(energySet.Symbols, s => s.Key == "darkness");
        Assert.Contains(energySet.Symbols, s => s.Key == "metal");
        Assert.Contains(energySet.Symbols, s => s.Key == "fairy");
        Assert.Contains(energySet.Symbols, s => s.Key == "dragon");
        Assert.Contains(energySet.Symbols, s => s.Key == "colorless");

        var raritySet = Assert.Single(graph.SymbolSets, s => s.Key == "pokemon-rarity");
        Assert.Equal(3, raritySet.Symbols.Count);

        // OptionLists
        Assert.Equal(3, graph.OptionLists.Count);
        Assert.Contains(graph.OptionLists, o => o.Key == "pokemon-stages");
        Assert.Contains(graph.OptionLists, o => o.Key == "trainer-types");
        Assert.Contains(graph.OptionLists, o => o.Key == "energy-types");

        // Traits
        Assert.Equal(7, graph.Traits.Count);
        Assert.Contains(graph.Traits, t => t.Key == "ex");
        Assert.Contains(graph.Traits, t => t.Key == "v");
        Assert.Contains(graph.Traits, t => t.Key == "vmax");
    }

    [Fact]
    public void TuttiILayoutDeiTemplatePokemonSonoValidi()
    {
        var graph = PokemonSeedData.Build();
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

        Assert.Equal(8, templateCount);
    }

    [Fact]
    public void RenderizzaDemoPokemonSenzaErrori()
    {
        var layout = DemoLayouts.PokemonBasic();
        var values = DemoLayouts.PokemonSampleValues();
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

        File.WriteAllBytes("/tmp/rendered_pokemon_card.png", result.Content);
    }

    [Fact]
    public void GeneratoreSimboliProduceSimboliPokemon()
    {
        var generator = new PlaceholderSymbolGenerator();

        var grass = generator.Generate("pokemon-energy", "grass");
        Assert.NotNull(grass);
        Assert.True(grass.Length > 0);

        var lightning = generator.Generate("pokemon-energy", "lightning");
        Assert.NotNull(lightning);
        Assert.True(lightning.Length > 0);

        var rare = generator.Generate("pokemon-rarity", "rare");
        Assert.NotNull(rare);
        Assert.True(rare.Length > 0);
    }

    [Fact]
    public void GeneratoreFrameProduceFramePokemon()
    {
        var generator = new PlaceholderFrameGenerator();
        var geometry = CardGeometry.PokerSize();

        foreach (var spec in PlaceholderFrameSpec.PokemonSet())
        {
            var png = generator.Generate(spec, geometry);
            Assert.NotNull(png);
            Assert.True(png.Length > 0);
        }
    }
}

