using CardMaker.AI.Abstractions;
using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class CardTextGenerationServiceTests
{
    [Fact]
    public void GetAvailableStyles_ReturnsExpectedStyles()
    {
        var engine = new FakeTextEngine();
        var config = new FakeAiConfig();
        var service = new CardTextGenerationService(engine, config);

        var styles = service.GetAvailableStyles();

        Assert.NotNull(styles);
        Assert.True(styles.Count >= 5);
        Assert.Contains(styles, s => s.Key == "classico");
        Assert.Contains(styles, s => s.Key == "epico");
        Assert.Contains(styles, s => s.Key == "oscuro");
        Assert.Contains(styles, s => s.Key == "umoristico");
        Assert.Contains(styles, s => s.Key == "tecnico");
    }

    [Theory]
    [InlineData("{\"title\": \"Mago Supremo\", \"description\": \"Infligge 1000 danni.\"}", "Mago Supremo", "Infligge 1000 danni.")]
    [InlineData("```json\n{\"title\": \"Pikachu Elettrico\", \"description\": \"Scarica potente.\"}\n```", "Pikachu Elettrico", "Scarica potente.")]
    [InlineData("Ecco il risultato:\n{\"Title\": \"Drago d'Ombra\", \"Description\": \"Volare. Travolgere.\"}", "Drago d'Ombra", "Volare. Travolgere.")]
    [InlineData("Risposta non JSON pura ma contiene \"title\": \"Cavaliere Solare\" e \"description\": \"Attacca con vigore.\"", "Cavaliere Solare", "Attacca con vigore.")]
    public void ParseAiJsonOutput_ExtractsTitleAndDescriptionCorrectly(string rawOutput, string expectedTitle, string expectedDesc)
    {
        var (title, desc) = CardTextGenerationService.ParseAiJsonOutput(rawOutput, "Fallback");
        Assert.Equal(expectedTitle, title);
        Assert.Equal(expectedDesc, desc);
    }

    [Fact]
    public void ParseAiJsonOutput_FallsBackGracefullyOnEmptyInput()
    {
        var (title, desc) = CardTextGenerationService.ParseAiJsonOutput(string.Empty, "Mio Titolo");
        Assert.Equal("Mio Titolo", title);
        Assert.Equal(string.Empty, desc);
    }

    [Fact]
    public async Task GenerateCardTextAsync_ThrowsWhenAiDisabled()
    {
        var engine = new FakeTextEngine();
        var config = new FakeAiConfig { IsEnabled = false };
        var service = new CardTextGenerationService(engine, config);

        var request = new CardAiGenerationRequestDto
        {
            GameKey = "yugioh",
            UserPrompt = "Crea un guerriero"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateCardTextAsync(request));
    }

    [Fact]
    public async Task GenerateCardTextAsync_ThrowsFileNotFoundWhenModelMissing()
    {
        var engine = new FakeTextEngine();
        var config = new FakeAiConfig
        {
            IsEnabled = true,
            ModelPath = Path.Combine(Path.GetTempPath(), "non-existent-" + Guid.NewGuid() + ".gguf")
        };
        var service = new CardTextGenerationService(engine, config);

        var request = new CardAiGenerationRequestDto
        {
            GameKey = "yugioh",
            UserPrompt = "Crea un guerriero"
        };

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GenerateCardTextAsync(request));
    }

    [Fact]
    public async Task GenerateCardTextAsync_ProducesResultWhenModelPresent()
    {
        var tempModel = Path.GetTempFileName();
        try
        {
            var engine = new FakeTextEngine
            {
                ResultToReturn = "{\"title\": \"Drago Antico\", \"description\": \"Rinasce dalle ceneri.\"}"
            };
            var config = new FakeAiConfig
            {
                IsEnabled = true,
                ModelPath = tempModel
            };
            var service = new CardTextGenerationService(engine, config);

            var request = new CardAiGenerationRequestDto
            {
                GameKey = "yugioh",
                CardTypeKey = "monster",
                StyleKey = "epico",
                UserPrompt = "Un drago ancestrale che risorge"
            };

            var result = await service.GenerateCardTextAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Drago Antico", result.Title);
            Assert.Equal("Rinasce dalle ceneri.", result.Description);
            Assert.True(engine.WasGenerateCalled);
        }
        finally
        {
            if (File.Exists(tempModel))
            {
                File.Delete(tempModel);
            }
        }
    }

    private sealed class FakeTextEngine : ITextGenerationEngine
    {
        public string? ModelPath { get; set; }
        public bool IsLoaded { get; set; }
        public bool WasGenerateCalled { get; private set; }
        public bool WasResetCalled { get; private set; }
        public bool WasUnloadCalled { get; private set; }
        public string ResultToReturn { get; set; } = "{\"title\": \"Carta Fake\", \"description\": \"Desc Fake\"}";

        public Task LoadModelAsync(string modelPath, int contextSize = 2048, int threads = 0, CancellationToken cancellationToken = default)
        {
            IsLoaded = true;
            ModelPath = modelPath;
            return Task.CompletedTask;
        }

        public Task<TextPromptResult> GenerateTextAsync(
            string modelPath,
            TextPromptRequest request,
            int contextSize = 2048,
            int threads = 0,
            IProgress<AiProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            WasGenerateCalled = true;
            progress?.Report(new AiProgressUpdate(AiProgressStage.Generating, "Generazione"));
            return Task.FromResult(new TextPromptResult(ResultToReturn, 42, 10));
        }

        public void ResetContext() => WasResetCalled = true;

        public Task UnloadModelAsync()
        {
            WasUnloadCalled = true;
            IsLoaded = false;
            return Task.CompletedTask;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeAiConfig : IAiConfigurationService
    {
        public bool IsEnabled { get; set; } = true;
        public string ModelPath { get; set; } = "fake.gguf";

        public Task<AiSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiSettingsDto { IsEnabled = IsEnabled });

        public Task SaveSettingsAsync(AiSettingsDto settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsAiEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(IsEnabled);

        public Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ModelPath);

        public Task<AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiModelRegistry.Gemma2B);

        public string GetModelsDirectory() => Path.GetTempPath();
    }
}
