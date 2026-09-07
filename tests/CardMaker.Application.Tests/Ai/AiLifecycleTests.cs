using CardMaker.AI.Abstractions;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class AiLifecycleTests
{
    [Fact]
    public void ResetContext_CallsUnderlyingEngineReset()
    {
        var engine = new MockEngine();
        var config = new MockConfig();
        var service = new CardTextGenerationService(engine, config);

        service.ResetContext();

        Assert.True(engine.ResetCalled);
    }

    [Fact]
    public async Task UnloadModelAsync_CallsUnderlyingEngineUnload()
    {
        var engine = new MockEngine();
        var config = new MockConfig();
        var service = new CardTextGenerationService(engine, config);

        await service.UnloadModelAsync();

        Assert.True(engine.UnloadCalled);
    }

    [Fact]
    public async Task GenerateCardTextAsync_RespectsCancellationToken()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var engine = new MockEngine();
            var config = new MockConfig { ModelPath = tempFile };
            var service = new CardTextGenerationService(engine, config);

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Gia cancellato

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GenerateCardTextAsync(new CardAiGenerationRequestDto { GameKey = "mtg" }, cancellationToken: cts.Token));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private sealed class MockEngine : ITextGenerationEngine
    {
        public string? ModelPath => null;
        public bool IsLoaded => false;
        public bool ResetCalled { get; private set; }
        public bool UnloadCalled { get; private set; }

        public Task LoadModelAsync(string modelPath, int contextSize = 2048, int threads = 0, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<TextPromptResult> GenerateTextAsync(
            string modelPath,
            TextPromptRequest request,
            int contextSize = 2048,
            int threads = 0,
            IProgress<AiProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new TextPromptResult("{\"title\":\"T\",\"description\":\"D\"}", 10, 1));
        }

        public void ResetContext() => ResetCalled = true;

        public Task UnloadModelAsync()
        {
            UnloadCalled = true;
            return Task.CompletedTask;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class MockConfig : IAiConfigurationService
    {
        public string ModelPath { get; set; } = "mock.gguf";

        public Task<AiSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiSettingsDto { IsEnabled = true });

        public Task SaveSettingsAsync(AiSettingsDto settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsAiEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ModelPath);

        public Task<CardMaker.AI.Models.AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CardMaker.AI.Models.AiModelRegistry.Gemma2B);

        public string GetModelsDirectory() => Path.GetTempPath();
    }
}
