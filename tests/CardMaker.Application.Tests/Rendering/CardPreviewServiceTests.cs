using CardMaker.Application.Rendering;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Infrastructure.Rendering;
using CardMaker.Rendering;
using CardMaker.Rendering.Text;
using Xunit;

namespace CardMaker.Application.Tests.Rendering;

public sealed class CardPreviewServiceTests
{
    private readonly CardPreviewService _sut;
    private readonly FakeResourceLoader _loader;

    public CardPreviewServiceTests()
    {
        _loader = new FakeResourceLoader();
        var renderer = new CardRenderer(new TextEngine());
        _sut = new CardPreviewService(_loader, renderer);
    }

    private sealed class FakeResourceLoader : IRenderResourceLoader
    {
        public int LoadCallCount { get; private set; }

        public Task<PreloadedRenderResources> LoadResourcesAsync(
            CardLayout layout,
            IReadOnlyDictionary<string, CardValue> values,
            Guid? gameId,
            CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            return Task.FromResult(new PreloadedRenderResources());
        }

        public Task<PreloadedRenderResources> LoadResourcesAsync(
            IEnumerable<CardLayout> layouts,
            IReadOnlyDictionary<string, CardValue> values,
            Guid? gameId,
            CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            return Task.FromResult(new PreloadedRenderResources());
        }
    }

    private static string SimpleLayoutJson =>
        LayoutSerializer.Serialize(new CardLayout
        {
            Canvas = new CanvasDefinition { WidthMm = 63, HeightMm = 88 },
            Layers =
            [
                new ShapeLayer
                {
                    Id = "shape-bg",
                    Rect = new NormalizedRect(0, 0, 1, 1),
                    FillColor = "#FFFFFF"
                }
            ]
        });

    [Fact]
    public async Task TEST_INT_002_RenderAsyncCachesParsedLayoutAndProducesValidPreview()
    {
        var request = new CardPreviewRequest
        {
            LayoutJson = SimpleLayoutJson,
            Values = new Dictionary<string, CardValue>(),
            Dpi = 150,
        };

        // Act - first call parses and caches
        var result1 = await _sut.RenderAsync(request);

        // Act - second call reuses cached layout (CACHE-PERF-002, SER-PERF-002)
        var result2 = await _sut.RenderAsync(request);

        // Assert
        Assert.True(result1.Succeeded);
        Assert.True(result2.Succeeded);
        Assert.NotNull(result1.Content);
        Assert.NotNull(result2.Content);
        Assert.True(result1.Content.Length > 0);
        Assert.Equal(result1.Content.Length, result2.Content.Length);
        Assert.Equal(2, _loader.LoadCallCount);
    }

    [Fact]
    public async Task TEST_INT_003_RenderAsyncReturnsFailureOnInvalidJson()
    {
        var request = new CardPreviewRequest
        {
            LayoutJson = "{ invalid-json: true ",
            Values = new Dictionary<string, CardValue>(),
            Dpi = 150,
        };

        var result = await _sut.RenderAsync(request);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task TEST_CONC_001_ConcurrentPreviewRequestsExecuteSafely()
    {
        var request = new CardPreviewRequest
        {
            LayoutJson = SimpleLayoutJson,
            Values = new Dictionary<string, CardValue>(),
            Dpi = 100,
        };

        // Run 10 concurrent render requests
        var tasks = Enumerable.Range(0, 10).Select(_ => _sut.RenderAsync(request));
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Succeeded);
            Assert.NotNull(r.Content);
            Assert.True(r.Content.Length > 0);
        });
    }
}
