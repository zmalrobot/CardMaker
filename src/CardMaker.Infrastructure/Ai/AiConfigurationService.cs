using System.Text.Json;
using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardMaker.Infrastructure.Ai;

/// <summary>
/// Gestione persistente delle impostazioni AI e risoluzione automatica/manuale del modello GGUF attivo.
/// Salva le configurazioni nel file dataRoot/ai-settings.json.
/// </summary>
public sealed class AiConfigurationService : IAiConfigurationService, IDisposable
{
    private readonly AiInfrastructureOptions _options;
    private readonly IHardwareProfileDetector _hardwareDetector;
    private readonly ILogger<AiConfigurationService>? _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AiConfigurationService(
        IOptions<AiInfrastructureOptions> options,
        IHardwareProfileDetector hardwareDetector,
        ILogger<AiConfigurationService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hardwareDetector);

        _options = options.Value;
        _hardwareDetector = hardwareDetector;
        _logger = logger;

        if (!Directory.Exists(_options.ModelsDirectory))
        {
            try
            {
                Directory.CreateDirectory(_options.ModelsDirectory);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Impossibile creare la cartella modelli '{Dir}': {Message}", _options.ModelsDirectory, ex.Message);
            }
        }
    }

    public string GetModelsDirectory()
    {
        return _options.ModelsDirectory;
    }

    public async Task<AiSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_options.SettingsFilePath))
            {
                return new AiSettingsDto
                {
                    IsEnabled = true,
                    SelectedModelKey = AiModelRegistry.AutoModelKey,
                    CpuThreads = 0,
                    CustomModelsDirectory = null
                };
            }

            var json = await File.ReadAllTextAsync(_options.SettingsFilePath, cancellationToken).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AiSettingsDto>(json, JsonOptions);
            return settings ?? new AiSettingsDto();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante la lettura di ai-settings.json: {Message}. Vengono utilizzati i valori predefiniti.", ex.Message);
            return new AiSettingsDto();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveSettingsAsync(AiSettingsDto settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var dir = Path.GetDirectoryName(_options.SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_options.SettingsFilePath, json, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("Impostazioni AI salvate con successo.");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> IsAiEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return settings.IsEnabled;
    }

    public async Task<bool> IsImageGenerationEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return settings.IsEnabled && settings.IsImageGenerationEnabled;
    }

    public async Task<AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settings.SelectedModelKey) ||
            settings.SelectedModelKey.Equals(AiModelRegistry.AutoModelKey, StringComparison.OrdinalIgnoreCase))
        {
            var totalMemory = _hardwareDetector.GetTotalPhysicalMemoryBytes();
            return AiModelRegistry.ResolveRecommendedModel(totalMemory);
        }

        var customModel = AiModelRegistry.FindByKey(settings.SelectedModelKey);
        if (customModel != null)
        {
            return customModel;
        }

        var fallbackMemory = _hardwareDetector.GetTotalPhysicalMemoryBytes();
        return AiModelRegistry.ResolveRecommendedModel(fallbackMemory);
    }

    public async Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        var activeModel = await GetActiveModelDefinitionAsync(cancellationToken).ConfigureAwait(false);

        var modelsDirectory = !string.IsNullOrWhiteSpace(settings.CustomModelsDirectory) && Directory.Exists(settings.CustomModelsDirectory)
            ? settings.CustomModelsDirectory
            : _options.ModelsDirectory;

        return Path.Combine(modelsDirectory, activeModel.FileName);
    }

    public async Task<AiModelDefinition> GetActiveImageModelDefinitionAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settings.SelectedImageModelKey) ||
            settings.SelectedImageModelKey.Equals(AiModelRegistry.AutoModelKey, StringComparison.OrdinalIgnoreCase))
        {
            var totalMemory = _hardwareDetector.GetTotalPhysicalMemoryBytes();
            return AiModelRegistry.ResolveRecommendedImageModel(totalMemory);
        }

        var customModel = AiModelRegistry.FindByKey(settings.SelectedImageModelKey);
        if (customModel != null)
        {
            return customModel;
        }

        var fallbackMemory = _hardwareDetector.GetTotalPhysicalMemoryBytes();
        return AiModelRegistry.ResolveRecommendedImageModel(fallbackMemory);
    }

    public async Task<string> GetActiveImageModelPathAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        var activeModel = await GetActiveImageModelDefinitionAsync(cancellationToken).ConfigureAwait(false);

        var modelsDirectory = !string.IsNullOrWhiteSpace(settings.CustomModelsDirectory) && Directory.Exists(settings.CustomModelsDirectory)
            ? settings.CustomModelsDirectory
            : _options.ModelsDirectory;

        return Path.Combine(modelsDirectory, activeModel.FileName);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _fileLock.Dispose();
    }
}
