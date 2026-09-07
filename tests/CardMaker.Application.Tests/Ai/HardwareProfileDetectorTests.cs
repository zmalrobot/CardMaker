using CardMaker.AI.Models;
using CardMaker.Infrastructure.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class HardwareProfileDetectorTests
{
    [Theory]
    [InlineData(2L * 1024 * 1024 * 1024, "gemma-2-2b")]
    [InlineData(4L * 1024 * 1024 * 1024, "gemma-2-2b")]
    [InlineData(6L * 1024 * 1024 * 1024, "gemma-2-2b")]
    [InlineData(8L * 1024 * 1024 * 1024, "gemma-3-4b")]
    [InlineData(12L * 1024 * 1024 * 1024, "gemma-3-4b")]
    [InlineData(16L * 1024 * 1024 * 1024, "gemma-2-9b")]
    [InlineData(24L * 1024 * 1024 * 1024, "gemma-2-9b")]
    [InlineData(32L * 1024 * 1024 * 1024, "gemma-2-27b")]
    [InlineData(64L * 1024 * 1024 * 1024, "gemma-2-27b")]
    public void ResolveRecommendedModel_MapsCorrectProfileByRamSize(long ramBytes, string expectedKey)
    {
        var model = AiModelRegistry.ResolveRecommendedModel(ramBytes);
        Assert.NotNull(model);
        Assert.Equal(expectedKey, model.Key);
    }

    [Fact]
    public void GetTotalPhysicalMemoryBytes_ReturnsPositiveMemory()
    {
        var detector = new HardwareProfileDetector();
        var bytes = detector.GetTotalPhysicalMemoryBytes();

        Assert.True(bytes > 1024L * 1024L * 1024L, "La memoria RAM rilevata deve essere superiore a 1 GB.");
    }

    [Fact]
    public void GetHardwareProfile_ConstructsValidSummary()
    {
        var detector = new HardwareProfileDetector();
        var tempDir = Path.Combine(Path.GetTempPath(), "cardmaker-ai-test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var profile = detector.GetHardwareProfile(tempDir);
            Assert.NotNull(profile);
            Assert.True(profile.TotalPhysicalMemoryGb > 0);
            Assert.False(string.IsNullOrWhiteSpace(profile.RecommendedModelKey));
            Assert.False(string.IsNullOrWhiteSpace(profile.RecommendedModelDisplayName));
            Assert.False(profile.IsModelDownloaded);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
