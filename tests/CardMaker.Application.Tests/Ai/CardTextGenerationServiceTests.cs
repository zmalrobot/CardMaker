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
    [InlineData("{\"title\": \"Mago Supremo\", \"description\": \"Infligge 1000 danni.\", \"attack\": 2500, \"defense\": 2100}", "Mago Supremo", "Infligge 1000 danni.", 2500, 2100)]
    [InlineData("```json\n{\"title\": \"Pikachu Elettrico\", \"description\": \"Scarica potente.\", \"atk\": 120, \"def\": 90}\n```", "Pikachu Elettrico", "Scarica potente.", 120, 90)]
    [InlineData("Ecco il risultato:\n{\"Title\": \"Drago d'Ombra\", \"Description\": \"Volare. Travolgere.\", \"attack\": \"3000\", \"defense\": \"2500\"}", "Drago d'Ombra", "Volare. Travolgere.", 3000, 2500)]
    [InlineData("Risposta non JSON pura ma contiene \"title\": \"Cavaliere Solare\" e \"description\": \"Attacca con vigore.\" con \"attack\": 1800 e \"defense\": 1500", "Cavaliere Solare", "Attacca con vigore.", 1800, 1500)]
    [InlineData("{\"title\": \"Spada Mistica\", \"description\": \"Aumenta ATK di 500.\", \"attack\": null, \"defense\": null}", "Spada Mistica", "Aumenta ATK di 500.", null, null)]
    public void ParseAiJsonOutput_ExtractsAllFieldsCorrectly(string rawOutput, string expectedTitle, string expectedDesc, int? expectedAtk, int? expectedDef)
    {
        var (title, desc, atk, def) = CardTextGenerationService.ParseAiJsonOutput(rawOutput, "Fallback");
        Assert.Equal(expectedTitle, title);
        Assert.Equal(expectedDesc, desc);
        Assert.Equal(expectedAtk, atk);
        Assert.Equal(expectedDef, def);
    }

    [Fact]
    public void ParseAiJsonOutput_FallsBackGracefullyOnEmptyInput()
    {
        var (title, desc, atk, def) = CardTextGenerationService.ParseAiJsonOutput(string.Empty, "Mio Titolo");
        Assert.Equal("Mio Titolo", title);
        Assert.Equal(string.Empty, desc);
        Assert.Null(atk);
        Assert.Null(def);
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
                ResultToReturn = "{\"title\": \"Drago Antico\", \"description\": \"Rinasce dalle ceneri.\", \"attack\": 2800, \"defense\": 2400}"
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
                CardTypeKey = "monster-normal",
                CardTypeName = "Mostro Normale",
                SupportsAttack = true,
                SupportsDefense = true,
                StyleKey = "epico",
                UserPrompt = "Un drago ancestrale che risorge"
            };

            var result = await service.GenerateCardTextAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Drago Antico", result.Title);
            Assert.Equal("Rinasce dalle ceneri.", result.Description);
            Assert.Equal(2800, result.Attack);
            Assert.Equal(2400, result.Defense);
            Assert.True(engine.WasGenerateCalled);
            Assert.Contains("MOSTRO NORMALE SENZA EFFETTO", engine.LastPromptRequest?.UserPrompt);
            Assert.Contains("VINCOLO TASSATIVO TIPOLOGIA - MOSTRO NORMALE", engine.LastPromptRequest?.UserPrompt);
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
        public TextPromptRequest? LastPromptRequest { get; private set; }
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
            LastPromptRequest = request;
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

        public Task<bool> IsImageGenerationEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(IsEnabled);

        public Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ModelPath);

        public Task<string> GetActiveImageModelPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ModelPath);

        public Task<AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiModelRegistry.Gemma2B);

        public Task<AiModelDefinition> GetActiveImageModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiModelRegistry.Sd15Turbo);

        public string GetModelsDirectory() => Path.GetTempPath();
    }
}
