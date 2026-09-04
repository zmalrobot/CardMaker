using CardMaker.Infrastructure.Rendering;
using Xunit;

namespace CardMaker.Application.Tests.Rendering;

public sealed class LruCacheHardeningTests
{
    [Fact]
    public void TEST_UNIT_009_LruCacheReusesEvictedNodesAndMaintainsCapacity()
    {
        const int capacity = 5;
        var cache = new LruCache<int, string>(capacity, disposeOnEviction: false);

        // Fill cache up to capacity
        for (var i = 0; i < capacity; i++)
        {
            cache.Set(i, $"Value-{i}");
        }
        Assert.Equal(capacity, cache.Count);

        // COLL-PERF-002: Continuously insert 1000 items - recycled from _nodePool
        for (var i = capacity; i < 1000; i++)
        {
            cache.Set(i, $"Value-{i}");
            Assert.Equal(capacity, cache.Count);
        }

        // Only the last 5 items must be present
        for (var i = 995; i < 1000; i++)
        {
            Assert.Equal($"Value-{i}", cache.TryGet(i));
        }
        Assert.Null(cache.TryGet(994));
    }

    [Fact]
    public void TEST_UNIT_010_UpdatingExistingKeyUpdatesValueWithoutEvicting()
    {
        var cache = new LruCache<string, string>(3, disposeOnEviction: false);
        cache.Set("k1", "v1");
        cache.Set("k2", "v2");
        cache.Set("k3", "v3");

        // Update existing key
        cache.Set("k1", "v1-updated");

        Assert.Equal(3, cache.Count);
        Assert.Equal("v1-updated", cache.TryGet("k1"));

        // Inserting a 4th key should evict k2 (since k1 was updated and became most recent)
        cache.Set("k4", "v4");
        Assert.Equal("v1-updated", cache.TryGet("k1"));
        Assert.Null(cache.TryGet("k2"));
        Assert.Equal("v3", cache.TryGet("k3"));
        Assert.Equal("v4", cache.TryGet("k4"));
    }

    [Fact]
    public void TEST_UNIT_011_ClearRecyclesNodesAndResetsCount()
    {
        var cache = new LruCache<int, string>(10, disposeOnEviction: false);
        for (var i = 0; i < 10; i++)
        {
            cache.Set(i, $"val-{i}");
        }

        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.Null(cache.TryGet(0));

        // Refill to ensure recycled nodes are properly reused
        for (var i = 0; i < 5; i++)
        {
            cache.Set(i, $"new-{i}");
        }
        Assert.Equal(5, cache.Count);
        Assert.Equal("new-0", cache.TryGet(0));
    }

    [Fact]
    public async Task TEST_CONC_002_HighConcurrencyMultiThreadStressTest()
    {
        const int capacity = 50;
        var cache = new LruCache<int, string>(capacity, disposeOnEviction: false);

        // 20 concurrent tasks doing random Set and TryGet
        var tasks = Enumerable.Range(0, 20).Select(workerId => Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                var key = (workerId * 100) + (i % 30);
                cache.Set(key, $"worker-{workerId}-val-{i}");
                var _ = cache.TryGet(key);
            }
        }));

        await Task.WhenAll(tasks);

        // Count must never exceed capacity
        Assert.True(cache.Count <= capacity);
    }
}

