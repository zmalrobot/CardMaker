using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;
using CardMaker.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace CardMaker.Application.Tests.Ai;

public sealed class AiConfigurationServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _settingsFile;
    private readonly string _modelsDir;
    private readonly HardwareProfileDetector _detector;
    private readonly AiConfigurationService _configService;

    public AiConfigurationServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "cardmaker-config-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
        _modelsDir = Path.Combine(_tempRoot, "models");
        _settingsFile = Path.Combine(_tempRoot, "ai-settings.json");

        var options = Options.Create(new AiInfrastructureOptions
        {
            DataRoot = _tempRoot,
            ModelsDirectory = _modelsDir,
            SettingsFilePath = _settingsFile
        });

        _detector = new HardwareProfileDetector();
        _configService = new AiConfigurationService(options, _detector);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultWhenFileNotPresent()
    {
        var settings = await _configService.GetSettingsAsync();

        Assert.NotNull(settings);
        Assert.True(settings.IsEnabled);
        Assert.Equal(AiModelRegistry.AutoModelKey, settings.SelectedModelKey);
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsAndReloadsSuccessfully()
    {
        var newSettings = new AiSettingsDto
        {
            IsEnabled = false,
            SelectedModelKey = "gemma-2-9b",
            CpuThreads = 4
        };

        await _configService.SaveSettingsAsync(newSettings);
        var loaded = await _configService.GetSettingsAsync();

        Assert.NotNull(loaded);
        Assert.False(loaded.IsEnabled);
        Assert.Equal("gemma-2-9b", loaded.SelectedModelKey);
        Assert.Equal(4, loaded.CpuThreads);
    }

    [Fact]
    public async Task GetActiveModelDefinitionAsync_HonorsManualOverride()
    {
        await _configService.SaveSettingsAsync(new AiSettingsDto
        {
            IsEnabled = true,
            SelectedModelKey = "gemma-2-27b"
        });

        var modelDef = await _configService.GetActiveModelDefinitionAsync();

        Assert.NotNull(modelDef);
        Assert.Equal("gemma-2-27b", modelDef.Key);
        Assert.Equal(32, modelDef.RecommendedRamGb);
    }

    [Fact]
    public async Task GetActiveModelPathAsync_CombinesModelsDirectoryAndFileName()
    {
        await _configService.SaveSettingsAsync(new AiSettingsDto
        {
            IsEnabled = true,
            SelectedModelKey = "gemma-2-2b"
        });

        var path = await _configService.GetActiveModelPathAsync();

        Assert.EndsWith(AiModelRegistry.Gemma2B.FileName, path, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(_modelsDir, path, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _configService.Dispose();
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // Ignora file lock in tear down
            }
        }
    }
}
