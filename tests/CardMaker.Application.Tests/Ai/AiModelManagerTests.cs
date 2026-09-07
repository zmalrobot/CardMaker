using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class AiModelManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeAiConfigService _configService;
    private readonly FakeAiModelDownloader _downloader;
    private readonly AiModelManager _manager;

    public AiModelManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cardmaker-manager-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        _configService = new FakeAiConfigService
        {
            IsEnabled = true,
            ActiveModel = AiModelRegistry.Gemma2B,
            ModelPath = Path.Combine(_tempDir, AiModelRegistry.Gemma2B.FileName)
        };

        _downloader = new FakeAiModelDownloader();
        _manager = new AiModelManager(_configService, _downloader);
    }

    [Fact]
    public async Task EnsureActiveModelReadyAsync_WhenAiDisabled_SetsDisabledState()
    {
        _configService.IsEnabled = false;

        await _manager.EnsureActiveModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Disabled, _manager.CurrentStatus);
        Assert.False(_downloader.WasDownloadCalled);
    }

    [Fact]
    public async Task EnsureActiveModelReadyAsync_WhenModelAlreadyValid_SetsReadyWithoutDownloading()
    {
        _configService.IsEnabled = true;
        _downloader.IsValid = true; // Il file locale è già valido

        await _manager.EnsureActiveModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentStatus);
        Assert.False(_downloader.WasDownloadCalled, "Non deve essere effettuato alcun download se il file è già valido.");
    }

    [Fact]
    public async Task EnsureActiveModelReadyAsync_WhenModelMissing_DownloadsAndValidatesSuccessfully()
    {
        _configService.IsEnabled = true;
        _downloader.IsValid = false; // Inizialmente assente

        var statusHistory = new List<AiModelReadinessStatus>();
        _manager.OnStatusChanged += () => statusHistory.Add(_manager.CurrentStatus);

        // Dopo il download, la validazione ha successo
        _downloader.OnDownload = () => _downloader.IsValid = true;

        await _manager.EnsureActiveModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentStatus);
        Assert.True(_downloader.WasDownloadCalled);
        Assert.Contains(AiModelReadinessStatus.Checking, statusHistory);
        Assert.Contains(AiModelReadinessStatus.Downloading, statusHistory);
        Assert.Contains(AiModelReadinessStatus.Validating, statusHistory);
        Assert.Contains(AiModelReadinessStatus.Ready, statusHistory);
    }

    [Fact]
    public async Task EnsureActiveModelReadyAsync_WhenDownloadFails_TransitionsToErrorWithoutCrashing()
    {
        _configService.IsEnabled = true;
        _downloader.IsValid = false;
        _downloader.ThrowOnDownload = new HttpRequestException("Connessione internet assente");

        await _manager.EnsureActiveModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Error, _manager.CurrentStatus);
        Assert.NotNull(_manager.CurrentProgress?.ErrorMessage);
        Assert.Contains("Connessione internet assente", _manager.CurrentProgress.ErrorMessage);
    }

    [Fact]
    public async Task EnsureActiveModelReadyAsync_WhenValidationFails_TransitionsToErrorState()
    {
        _configService.IsEnabled = true;
        _downloader.IsValid = false;
        _downloader.OnDownload = () => _downloader.IsValid = false; // Rimane non valido dopo download

        await _manager.EnsureActiveModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Error, _manager.CurrentStatus);
        Assert.NotNull(_manager.CurrentProgress?.ErrorMessage);
    }

    [Fact]
    public async Task EnsureActiveModelReadyAsync_WhenOverrideChanged_DownloadsNewSelectedModel()
    {
        _configService.IsEnabled = true;
        _configService.ActiveModel = AiModelRegistry.Gemma4B;
        _configService.ModelPath = Path.Combine(_tempDir, AiModelRegistry.Gemma4B.FileName);
        _downloader.IsValid = false;
        _downloader.OnDownload = () => _downloader.IsValid = true;

        await _manager.EnsureActiveModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentStatus);
        Assert.Equal(AiModelRegistry.Gemma4B.Key, _downloader.LastDownloadedModel?.Key);
    }

    public void Dispose()
    {
        _manager.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    private sealed class FakeAiConfigService : IAiConfigurationService
    {
        public bool IsEnabled { get; set; } = true;
        public AiModelDefinition ActiveModel { get; set; } = AiModelRegistry.Gemma2B;
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
            Task.FromResult(ActiveModel);

        public Task<AiModelDefinition> GetActiveImageModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiModelRegistry.Sd15Turbo);

        public string GetModelsDirectory() => Path.GetTempPath();
    }

    private sealed class FakeAiModelDownloader : IAiModelDownloader
    {
        public bool IsValid { get; set; }
        public bool WasDownloadCalled { get; private set; }
        public AiModelDefinition? LastDownloadedModel { get; private set; }
        public Action? OnDownload { get; set; }
        public Exception? ThrowOnDownload { get; set; }

        public Task DownloadModelAsync(
            AiModelDefinition model,
            string targetFilePath,
            IProgress<AiModelDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            WasDownloadCalled = true;
            LastDownloadedModel = model;

            if (ThrowOnDownload != null)
            {
                throw ThrowOnDownload;
            }

            progress?.Report(new AiModelDownloadProgress(
                ModelKey: model.Key,
                ModelDisplayName: model.DisplayName,
                BytesDownloaded: 500,
                TotalBytes: 1000,
                Percentage: 50.0,
                BytesPerSecond: 100,
                EstimatedRemaining: TimeSpan.FromSeconds(5),
                StatusMessage: "Download",
                State: AiModelReadinessStatus.Downloading));

            OnDownload?.Invoke();
            return Task.CompletedTask;
        }

        public bool ValidateModelFile(AiModelDefinition model, string targetFilePath, out string? validationError)
        {
            if (IsValid)
            {
                validationError = null;
                return true;
            }

            validationError = "File non valido";
            return false;
        }
    }
}
