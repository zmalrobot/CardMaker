using System.Reflection;
using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class AiModelManagerImageDownloadTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeAiConfig _configService;
    private readonly FakeAiDownloader _downloader;
    private readonly AiModelManager _manager;

    public AiModelManagerImageDownloadTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cardmaker-img-download-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        _configService = new FakeAiConfig
        {
            IsTextEnabled = true,
            IsImageEnabled = true,
            ActiveTextModel = AiModelRegistry.Gemma2B,
            ActiveImageModel = AiModelRegistry.Sd15Turbo,
            TextModelPath = Path.Combine(_tempDir, AiModelRegistry.Gemma2B.FileName),
            ImageModelPath = Path.Combine(_tempDir, AiModelRegistry.Sd15Turbo.FileName)
        };

        _downloader = new FakeAiDownloader();
        _manager = new AiModelManager(_configService, _downloader);
    }

    [Fact]
    public async Task EnsureActiveImageModelReadyAsync_WhenDisabled_SetsDisabledStateAndNoDownload()
    {
        _configService.IsImageEnabled = false;

        await _manager.EnsureActiveImageModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Disabled, _manager.CurrentImageStatus);
        Assert.False(_downloader.WasDownloadCalled);
    }

    [Fact]
    public async Task EnsureActiveImageModelReadyAsync_WhenModelAlreadyValid_SetsReadyWithoutDownloading()
    {
        _configService.IsImageEnabled = true;
        _downloader.IsValid = true;

        await _manager.EnsureActiveImageModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentImageStatus);
        Assert.False(_downloader.WasDownloadCalled);
    }

    [Fact]
    public async Task EnsureActiveImageModelReadyAsync_WhenModelMissing_DownloadsAndValidatesSuccessfully()
    {
        _configService.IsImageEnabled = true;
        _downloader.IsValid = false;

        var statusHistory = new List<AiModelReadinessStatus>();
        _manager.OnStatusChanged += () => statusHistory.Add(_manager.CurrentImageStatus);

        _downloader.OnDownload = () => _downloader.IsValid = true;

        await _manager.EnsureActiveImageModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentImageStatus);
        Assert.True(_downloader.WasDownloadCalled);
        Assert.Equal(AiModelRegistry.Sd15Turbo.Key, _downloader.LastDownloadedModel?.Key);
        Assert.Contains(AiModelReadinessStatus.Checking, statusHistory);
        Assert.Contains(AiModelReadinessStatus.Downloading, statusHistory);
        Assert.Contains(AiModelReadinessStatus.Validating, statusHistory);
        Assert.Contains(AiModelReadinessStatus.Ready, statusHistory);
    }

    [Fact]
    public async Task EnsureActiveImageModelReadyAsync_WhenDownloadInterrupted_SetsErrorState()
    {
        _configService.IsImageEnabled = true;
        _downloader.IsValid = false;
        _downloader.ThrowOnDownload = new OperationCanceledException();

        await _manager.EnsureActiveImageModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Error, _manager.CurrentImageStatus);
        Assert.NotEqual(AiModelReadinessStatus.Ready, _manager.CurrentImageStatus);
    }

    [Fact]
    public async Task EnsureActiveImageModelReadyAsync_WhenValidationFails_TransitionsToErrorState()
    {
        _configService.IsImageEnabled = true;
        _downloader.IsValid = false;
        _downloader.OnDownload = () => _downloader.IsValid = false;

        await _manager.EnsureActiveImageModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Error, _manager.CurrentImageStatus);
        Assert.NotNull(_manager.CurrentImageProgress?.ErrorMessage);
    }

    [Fact]
    public async Task EnsureActiveImageModelReadyAsync_WithOverride_DownloadsConfiguredModel()
    {
        _configService.IsImageEnabled = true;
        _configService.ActiveImageModel = AiModelRegistry.DreamShaper8;
        _configService.ImageModelPath = Path.Combine(_tempDir, AiModelRegistry.DreamShaper8.FileName);
        _downloader.IsValid = false;
        _downloader.OnDownload = () => _downloader.IsValid = true;

        await _manager.EnsureActiveImageModelReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentImageStatus);
        Assert.Equal(AiModelRegistry.DreamShaper8.Key, _downloader.LastDownloadedModel?.Key);
    }

    [Fact]
    public async Task EnsureAllActiveModelsReadyAsync_PreparesBothTextAndImageModelsSequentially()
    {
        _configService.IsTextEnabled = true;
        _configService.IsImageEnabled = true;
        _downloader.IsValid = false;
        _downloader.OnDownload = () => _downloader.IsValid = true;

        await _manager.EnsureAllActiveModelsReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentStatus);
        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentImageStatus);
        Assert.Equal(AiModelReadinessStatus.Ready, _manager.OverallStatus);
    }

    [Fact]
    public async Task OverallStatus_WhenTextReadyAndImageDownloading_ReturnsPartiallyReady()
    {
        _configService.IsTextEnabled = true;
        _configService.IsImageEnabled = true;

        _downloader.IsValid = true;
        await _manager.EnsureActiveModelReadyAsync();
        Assert.Equal(AiModelReadinessStatus.Ready, _manager.CurrentStatus);

        _downloader.IsValid = false;
        _downloader.OnDownload = () =>
        {
            Assert.Equal(AiModelReadinessStatus.PartiallyReady, _manager.OverallStatus);
            Assert.Contains("Testo pronto", _manager.OverallStatusSummary);
            _downloader.IsValid = true;
        };

        await _manager.EnsureActiveImageModelReadyAsync();
        Assert.Equal(AiModelReadinessStatus.Ready, _manager.OverallStatus);
    }

    [Fact]
    public async Task OverallStatus_WhenBothDisabled_ReturnsDisabled()
    {
        _configService.IsTextEnabled = false;
        _configService.IsImageEnabled = false;

        await _manager.EnsureAllActiveModelsReadyAsync();

        Assert.Equal(AiModelReadinessStatus.Disabled, _manager.CurrentStatus);
        Assert.Equal(AiModelReadinessStatus.Disabled, _manager.CurrentImageStatus);
        Assert.Equal(AiModelReadinessStatus.Disabled, _manager.OverallStatus);
        Assert.Equal("AI disabilitata", _manager.OverallStatusSummary);
    }

    [Fact]
    public void AiModelDefinition_ContainsAllRequiredMetadata()
    {
        var model = AiModelRegistry.Sd15Turbo;

        Assert.Equal("sd-1.5-turbo", model.Key);
        Assert.NotEmpty(model.DisplayName);
        Assert.Equal(AiModelCapability.Image, model.Capability);
        Assert.Equal("GGUF", model.Format);
        Assert.True(model.RecommendedRamGb >= 4);
        Assert.True(model.RecommendedVramGb >= 0);
        Assert.NotEmpty(model.RecommendedHardware!);
        Assert.True(model.ExpectedSizeBytes > 0);
        Assert.NotEmpty(model.DownloadUrlDirect);
    }

    [Fact]
    public void CardMakerAiVersion_IsExtractedFromAssembly()
    {
        var asm = typeof(AiModelRegistry).Assembly;
        var ver = asm.GetName().Version;

        Assert.NotNull(ver);
        Assert.True(ver.Major >= 1);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    private sealed class FakeAiConfig : IAiConfigurationService
    {
        public bool IsTextEnabled { get; set; } = true;
        public bool IsImageEnabled { get; set; } = true;
        public AiModelDefinition ActiveTextModel { get; set; } = AiModelRegistry.Gemma2B;
        public AiModelDefinition ActiveImageModel { get; set; } = AiModelRegistry.Sd15Turbo;
        public string TextModelPath { get; set; } = "gemma.gguf";
        public string ImageModelPath { get; set; } = "sd.gguf";

        public Task<AiSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiSettingsDto { IsEnabled = IsTextEnabled, IsImageGenerationEnabled = IsImageEnabled });

        public Task SaveSettingsAsync(AiSettingsDto settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsAiEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(IsTextEnabled);

        public Task<bool> IsImageGenerationEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(IsImageEnabled);

        public Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(TextModelPath);

        public Task<string> GetActiveImageModelPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ImageModelPath);

        public Task<AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveTextModel);

        public Task<AiModelDefinition> GetActiveImageModelDefinitionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveImageModel);

        public string GetModelsDirectory() => Path.GetTempPath();
    }

    private sealed class FakeAiDownloader : IAiModelDownloader
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
                StatusMessage: "Download in corso",
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

            validationError = "File del modello non valido o non presente.";
            return false;
        }
    }
}
