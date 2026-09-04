using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Placeholders;
using CardMaker.Rendering.Text;
using SkiaSharp;
using Xunit;

namespace CardMaker.Rendering.Tests;

public class AlignmentVisualTests
{
    private static readonly CardRenderer Renderer = new(new TextEngine());

    private sealed class LocalVisualResources : IRenderResources, IDisposable
    {
        private readonly Dictionary<string, SKImage> _images = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SKImage> _symbols = new(StringComparer.OrdinalIgnoreCase);

        public void AddImage(string key, byte[] png)
        {
            using var data = SKData.CreateCopy(png);
            var img = SKImage.FromEncodedData(data);
            if (img != null)
            {
                _images[key] = img;
            }
        }

        public void AddSymbol(string setKey, string symbolKey, byte[] png)
        {
            using var data = SKData.CreateCopy(png);
            var img = SKImage.FromEncodedData(data);
            if (img != null)
            {
                _symbols[$"{setKey}/{symbolKey}"] = img;
            }
        }

        public SKImage? GetImage(Guid assetId) => null;
        public SKImage? GetImageByKey(string assetKey) => _images.GetValueOrDefault(assetKey);
        public SKImage? GetSymbol(string symbolSetKey, string symbolKey) => _symbols.GetValueOrDefault($"{symbolSetKey}/{symbolKey}");
        public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
        {
            isFallback = false;
            return TestFonts.Default;
        }

        public void Dispose()
        {
            foreach (var img in _images.Values)
            {
                img.Dispose();
            }
            foreach (var sym in _symbols.Values)
            {
                sym.Dispose();
            }
        }
    }

    [Fact]
    public void GenerateCardsForVisualInspection()
    {
        var outDir = Path.Combine(AppContext.BaseDirectory, "visual_tests_output");
        Directory.CreateDirectory(outDir);
        var frameGen = new PlaceholderFrameGenerator();
        var symGen = new PlaceholderSymbolGenerator();

        // 1. YuGiOh
        using (var res = new LocalVisualResources())
        {
            var geo = CardGeometry.YuGiOh(300);
            var frameSpec = PlaceholderFrameSpec.YuGiOhSet().First(s => s.Key == "monster-normal");
            res.AddImage("monster-normal", frameGen.Generate(frameSpec, geo));
            res.AddImage(DemoLayouts.FrameAssetKey, frameGen.Generate(frameSpec, geo));
            res.AddSymbol("attributes", "light", symGen.Generate("attributes", "light", 128));
            res.AddSymbol("stars", "level", symGen.Generate("stars", "level", 64));

            var layout = DemoLayouts.YuGiOhMonster();
            var values = DemoLayouts.SampleValues();
            values["level"] = CardValue.FromNumber(7);

            var req = new CardRenderRequest
            {
                Layout = layout,
                Values = values,
                Resources = res,
                Dpi = 300,
                IncludeBleed = false,
                RoundCorners = true,
            };

            var result = Renderer.Render(req);
            File.WriteAllBytes(Path.Combine(outDir, "test_yugioh_current.png"), result.Content);
        }

        // 2. Pokemon
        using (var res = new LocalVisualResources())
        {
            var geo = CardGeometry.PokerSize(300);
            var frameSpec = PlaceholderFrameSpec.PokemonSet().First(s => s.Key == "pokemon-frame-lightning");
            res.AddImage("pokemon-frame-lightning", frameGen.Generate(frameSpec, geo));
            res.AddImage(DemoLayouts.PokemonFrameAssetKey, frameGen.Generate(frameSpec, geo));
            res.AddSymbol("pokemon-energy", "lightning", symGen.Generate("pokemon-energy", "lightning", 128));
            res.AddSymbol("pokemon-energy", "colorless", symGen.Generate("pokemon-energy", "colorless", 128));
            res.AddSymbol("pokemon-energy", "fighting", symGen.Generate("pokemon-energy", "fighting", 128));
            res.AddSymbol("pokemon-rarity", "common", symGen.Generate("pokemon-rarity", "common", 64));

            var layout = DemoLayouts.PokemonBasic();
            var values = DemoLayouts.PokemonSampleValues();

            var req = new CardRenderRequest
            {
                Layout = layout,
                Values = values,
                Resources = res,
                Dpi = 300,
                IncludeBleed = false,
                RoundCorners = true,
            };

            var result = Renderer.Render(req);
            File.WriteAllBytes(Path.Combine(outDir, "test_pokemon_current.png"), result.Content);
        }

        // 3. Magic
        using (var res = new LocalVisualResources())
        {
            var geo = CardGeometry.PokerSize(300);
            var frameSpec = PlaceholderFrameSpec.MtgSet().First(s => s.Key == "mtg-frame-white");
            res.AddImage("mtg-frame-white", frameGen.Generate(frameSpec, geo));
            res.AddImage(DemoLayouts.MtgFrameAssetKey, frameGen.Generate(frameSpec, geo));
            res.AddSymbol("mtg-mana", "W", symGen.Generate("mtg-mana", "W", 128));
            res.AddSymbol("mtg-mana", "3", symGen.Generate("mtg-mana", "3", 128));
            res.AddSymbol("mtg-rarity", "rare", symGen.Generate("mtg-rarity", "rare", 64));

            var layout = DemoLayouts.MtgCreature();
            var values = DemoLayouts.MtgSampleValues();

            var req = new CardRenderRequest
            {
                Layout = layout,
                Values = values,
                Resources = res,
                Dpi = 300,
                IncludeBleed = false,
                RoundCorners = true,
            };

            var result = Renderer.Render(req);
            File.WriteAllBytes(Path.Combine(outDir, "test_mtg_current.png"), result.Content);
        }
    }
}
