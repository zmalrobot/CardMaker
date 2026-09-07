using CardMaker.AI.Abstractions;
using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class CardImageGenerationServiceTests
{
    [Fact]
    public void BuildPositivePrompt_IncludesGameStyleTitleLoreAndUserPrompt()
    {
        var request = new CardAiImageGenerationRequestDto
        {
            GameKey = "yugioh",
            StyleKey = "anime-tcg",
            CardTitle = "Drago Occhi Blu",
            CardDescriptionOrLore = "Un potente drago con occhi azzurri scintillanti.",
            UserPrompt = "epic roaring pose, lightning effects"
        };

        var prompt = CardImageGenerationService.BuildPositivePrompt(request);

        Assert.Contains("Yu-Gi-Oh! card art style", prompt);
        Assert.Contains("anime style", prompt);
        Assert.Contains("subject: Drago Occhi Blu", prompt);
        Assert.Contains("visual theme: Un potente drago", prompt);
        Assert.Contains("epic roaring pose, lightning effects", prompt);
    }

    [Fact]
    public void BuildPositivePrompt_TruncatesLongLore()
    {
        var longLore = new string('A', 200);
        var request = new CardAiImageGenerationRequestDto
        {
            GameKey = "mtg",
            StyleKey = "fantasy-oil",
            CardTitle = "Ancient Dragon",
            CardDescriptionOrLore = longLore
        };

        var prompt = CardImageGenerationService.BuildPositivePrompt(request);

        Assert.Contains(new string('A', 120), prompt);
        Assert.DoesNotContain(new string('A', 121), prompt);
    }

    [Fact]
    public async Task GenerateArtworkAsync_Throws_WhenImageGenerationDisabled()
    {
        var engine = new FakeImageEngine();
        var config = new FakeAiConfig { ImageAiEnabled = false };
        var service = new CardImageGenerationService(engine, config);

        var request = new CardAiImageGenerationRequestDto
        {
            GameKey = "pokemon",
            CardTitle = "Pikachu"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateArtworkAsync(request));

        Assert.Contains("disabilitata", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateArtworkAsync_Throws_WhenModelFileNotFound()
    {
        var engine = new FakeImageEngine();
        var config = new FakeAiConfig
        {
            ImageAiEnabled = true,
            ActiveImageModelPath = "non_existent_model_file.gguf"
        };
        var service = new CardImageGenerationService(engine, config);

        var request = new CardAiImageGenerationRequestDto
        {
            GameKey = "pokemon",
            CardTitle = "Pikachu"
        };

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.GenerateArtworkAsync(request));
    }

    [Fact]
    public async Task GenerateArtworkAsync_ExecutesAndReturnsResult()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var fakeEngine = new FakeImageEngine
            {
                ResultToReturn = new ImageGenerationResult(
                    PngBytes: [0x89, 0x50, 0x4E, 0x47],
                    Width: 512,
                    Height: 512,
                    SeedUsed: 4242,
                    DurationMs: 150,
                    ModelUsed: "sd-1.5-turbo")
            };

            var config = new FakeAiConfig
            {
                ImageAiEnabled = true,
                ActiveImageModelPath = tempFile,
                ActiveImageModelDefinition = AiModelRegistry.Sd15Turbo
            };

            var service = new CardImageGenerationService(fakeEngine, config);

            var request = new CardAiImageGenerationRequestDto
            {
                GameKey = "yugioh",
                StyleKey = "anime-tcg",
                CardTitle = "Dark Magician",
                NegativePrompt = "lowres",
                Seed = 4242
            };

            var result = await service.GenerateArtworkAsync(request);

            Assert.NotNull(result);
            Assert.Equal(4, result.ImageBytes.Length);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(512, result.Width);
            Assert.Equal(512, result.Height);
            Assert.Equal(4242L, result.SeedUsed);
            Assert.Equal(150L, result.DurationMs);
            Assert.Equal(AiModelRegistry.Sd15Turbo.DisplayName, result.ModelUsed);

            Assert.NotNull(fakeEngine.LastRequest);
            Assert.Contains("lowres", fakeEngine.LastRequest.NegativePrompt);
            Assert.Contains(CardImageGenerationService.DefaultNegativePrompt, fakeEngine.LastRequest.NegativePrompt);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task UnloadModelAsync_CallsEngineUnload()
    {
        var engine = new FakeImageEngine();
        var config = new FakeAiConfig();
        var service = new CardImageGenerationService(engine, config);

        await service.UnloadModelAsync();

        Assert.True(engine.UnloadCalled);
    }

    [Theory]
    [InlineData(4L * 1024 * 1024 * 1024, "sd-1.5-turbo")]
    [InlineData(8L * 1024 * 1024 * 1024, "dreamshaper-8")]
    [InlineData(16L * 1024 * 1024 * 1024, "sdxl-lightning-4step")]
    [InlineData(32L * 1024 * 1024 * 1024, "sdxl-lightning-4step")]
    public void ResolveRecommendedImageModel_SelectsExpectedModel(long ramBytes, string expectedKey)
    {
        var model = AiModelRegistry.ResolveRecommendedImageModel(ramBytes);
        Assert.Equal(expectedKey, model.Key);
    }

    private sealed class FakeImageEngine : IImageGenerationEngine
    {
        public bool IsLoaded => false;
        public string? ModelPath => null;
        public bool UnloadCalled { get; private set; }
        public ImagePromptRequest? LastRequest { get; private set; }
        public ImageGenerationResult ResultToReturn { get; set; } = new([0x89, 0x50, 0x4E, 0x47], 512, 512, 1, 100, "fake-model");

        public Task LoadModelAsync(string modelPath, int contextSize = 2048, int threads = 0, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ImageGenerationResult> GenerateImageAsync(
            string modelPath,
            ImagePromptRequest request,
            int threads = 0,
            IProgress<AiProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(ResultToReturn);
        }

        public Task UnloadModelAsync()
        {
            UnloadCalled = true;
            return Task.CompletedTask;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeAiConfig : IAiConfigurationService
    {
        public bool ImageAiEnabled { get; set; } = true;
        public string ActiveImageModelPath { get; set; } = "fake_image_model.gguf";
        public AiModelDefinition ActiveImageModelDefinition { get; set; } = AiModelRegistry.Sd15Turbo;

        public Task<bool> IsAiEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> IsImageGenerationEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(ImageAiEnabled);
        public Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default) => Task.FromResult("mock.gguf");
        public Task<string> GetActiveImageModelPathAsync(CancellationToken cancellationToken = default) => Task.FromResult(ActiveImageModelPath);
        public Task<AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default) => Task.FromResult(AiModelRegistry.Gemma2B);
        public Task<AiModelDefinition> GetActiveImageModelDefinitionAsync(CancellationToken cancellationToken = default) => Task.FromResult(ActiveImageModelDefinition);
        public Task<AiSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AiSettingsDto { IsImageGenerationEnabled = ImageAiEnabled });
        public Task SaveSettingsAsync(AiSettingsDto settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public string GetModelsDirectory() => Path.GetTempPath();
    }
}
