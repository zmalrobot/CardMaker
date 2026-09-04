using CardMaker.Rendering.Fonts;
using SkiaSharp;
using Xunit;

namespace CardMaker.Rendering.Tests;

public sealed class FontRegistryHardeningTests
{
    private sealed class ThreadSafeFontSource(byte[] defaultFontBytes) : IFontSource
    {
        public int CallCount;

        public byte[]? GetFontBytes(string roleAlias)
        {
            Interlocked.Increment(ref CallCount);
            return defaultFontBytes;
        }
    }

    private static byte[] GetRobotoBytes()
    {
        var assembly = typeof(TestFonts).Assembly;
        using var stream = assembly.GetManifestResourceStream("CardMaker.Rendering.Tests.TestAssets.Roboto-Regular.ttf")
            ?? throw new InvalidOperationException("Embedded test font not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task TEST_CONC_003_HighConcurrencyFontResolutionWithoutLockContention()
    {
        // Arrange
        var fontBytes = GetRobotoBytes();

        var source = new ThreadSafeFontSource(fontBytes);
        using var registry = new FontRegistry(source);

        // Act - LOCK-PERF-002: 30 concurrent tasks resolving various aliases
        var tasks = Enumerable.Range(0, 30).Select(workerId => Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                var alias = $"role-{(i % 5)}";
                var resolved = registry.Resolve(alias);
                Assert.NotNull(resolved.Typeface);
                Assert.False(resolved.IsFallback);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Highly concurrent resolution without lock contention (LOCK-PERF-002)
        // Lock-free cache converges with minimal duplicate loads across 1500 parallel calls
        Assert.InRange(source.CallCount, 5, 25);

        // Subsequent sequential requests hit cache with zero additional loads
        var beforeSequential = source.CallCount;
        for (var i = 0; i < 5; i++)
        {
            var resolved = registry.Resolve($"role-{i}");
            Assert.NotNull(resolved.Typeface);
        }
        Assert.Equal(beforeSequential, source.CallCount);
    }

    [Fact]
    public void TEST_UNIT_012_DisposeSafelyDrainsConcurrentBagWithoutExceptions()
    {
        var fontBytes = GetRobotoBytes();

        var source = new ThreadSafeFontSource(fontBytes);
        var registry = new FontRegistry(source);

        registry.Resolve("font-a");
        registry.Resolve("font-b");
        registry.Resolve("font-c");

        // Act - dispose clears cache and drains ConcurrentBag
        registry.Dispose();

        // Repeated dispose should be idempotent
        registry.Dispose();
    }
}
