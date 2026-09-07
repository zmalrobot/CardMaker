using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;

namespace CardMaker.Application.Ai;

/// <summary>
/// Implementazione thread-safe del coordinatore dello stato dei modelli AI.
/// Gestisce il ciclo: Verifica configurazione -> Rilevamento locale -> Download / Resume -> Validazione -> Ready.
/// </summary>
public sealed class AiModelManager : IAiModelManager, IDisposable
{
    private readonly IAiConfigurationService _configService;
    private readonly IAiModelDownloader _downloader;
    private readonly ILogger<AiModelManager>? _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private CancellationTokenSource? _downloadCts;
    private bool _disposed;

    public AiModelReadinessStatus CurrentStatus { get; private set; } = AiModelReadinessStatus.Checking;
    public AiModelDownloadProgress? CurrentProgress { get; private set; }
    public AiModelDefinition? ActiveModel { get; private set; }
    public string? ActiveModelPath { get; private set; }

    public event Action? OnStatusChanged;

    public AiModelManager(
        IAiConfigurationService configService,
        IAiModelDownloader downloader,
        ILogger<AiModelManager>? logger = null)
    {
        _configService = configService;
        _downloader = downloader;
        _logger = logger;
    }

    public async Task EnsureActiveModelReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var isEnabled = await _configService.IsAiEnabledAsync(cancellationToken).ConfigureAwait(false);
            if (!isEnabled)
            {
                SetState(AiModelReadinessStatus.Disabled, null);
                _logger?.LogInformation("Funzionalita AI disabilitata dalle impostazioni: nessun controllo o download del modello richiesto.");
                return;
            }

            SetState(AiModelReadinessStatus.Checking, "Verifica del modello locale...");

            var modelDef = await _configService.GetActiveModelDefinitionAsync(cancellationToken).ConfigureAwait(false);
            var modelPath = await _configService.GetActiveModelPathAsync(cancellationToken).ConfigureAwait(false);

            ActiveModel = modelDef;
            ActiveModelPath = modelPath;

            if (!forceDownload && _downloader.ValidateModelFile(modelDef, modelPath, out _))
            {
                _logger?.LogInformation("Modello AI '{ModelKey}' presente e valido in '{ModelPath}'. Stato: Ready.", modelDef.Key, modelPath);
                SetState(AiModelReadinessStatus.Ready, "Modello pronto all'uso.");
                return;
            }

            _logger?.LogInformation("Modello AI '{ModelKey}' non presente o non valido. Avvio download asincrono da '{DownloadUrl}'...",
                modelDef.Key, modelDef.DownloadUrlDirect);

            _downloadCts?.Cancel();
            _downloadCts?.Dispose();
            _downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var progressReporter = new Progress<AiModelDownloadProgress>(p =>
            {
                if (CurrentStatus == AiModelReadinessStatus.Downloading)
                {
                    CurrentProgress = p;
                    NotifyStatusChanged();
                }
            });

            SetState(AiModelReadinessStatus.Downloading, "Avvio download del modello...", modelDef);

            await _downloader.DownloadModelAsync(modelDef, modelPath, progressReporter, _downloadCts.Token).ConfigureAwait(false);

            SetState(AiModelReadinessStatus.Validating, "Validazione file scaricato in corso...", modelDef);

            if (_downloader.ValidateModelFile(modelDef, modelPath, out var validationError))
            {
                _logger?.LogInformation("Download e validazione del modello '{ModelKey}' completati con successo. Stato: Ready.", modelDef.Key);
                SetState(AiModelReadinessStatus.Ready, "Modello scaricato, validato e pronto all'uso.", modelDef);
            }
            else
            {
                var errorMsg = validationError ?? "Validazione di integrita del file scaricato fallita.";
                _logger?.LogError("Errore di validazione del modello '{ModelKey}': {Error}", modelDef.Key, errorMsg);
                SetErrorState(errorMsg, modelDef);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Download del modello AI interrotto dall'utente.");
            SetState(AiModelReadinessStatus.Error, "Download interrotto dall'utente.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante il download o la verifica del modello AI: {Message}", ex.Message);
            SetErrorState($"Errore durante il download del modello: {ex.Message}", ActiveModel);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void CancelDownload()
    {
        _downloadCts?.Cancel();
    }

    private void SetState(AiModelReadinessStatus status, string? message, AiModelDefinition? model = null)
    {
        CurrentStatus = status;
        CurrentProgress = new AiModelDownloadProgress(
            ModelKey: model?.Key ?? ActiveModel?.Key ?? string.Empty,
            ModelDisplayName: model?.DisplayName ?? ActiveModel?.DisplayName ?? string.Empty,
            BytesDownloaded: status == AiModelReadinessStatus.Ready && model != null ? model.ExpectedSizeBytes : 0,
            TotalBytes: model?.ExpectedSizeBytes ?? 0,
            Percentage: status == AiModelReadinessStatus.Ready ? 100.0 : 0.0,
            BytesPerSecond: 0,
            EstimatedRemaining: null,
            StatusMessage: message ?? string.Empty,
            State: status);

        NotifyStatusChanged();
    }

    private void SetErrorState(string errorMessage, AiModelDefinition? model)
    {
        CurrentStatus = AiModelReadinessStatus.Error;
        CurrentProgress = new AiModelDownloadProgress(
            ModelKey: model?.Key ?? ActiveModel?.Key ?? string.Empty,
            ModelDisplayName: model?.DisplayName ?? ActiveModel?.DisplayName ?? string.Empty,
            BytesDownloaded: 0,
            TotalBytes: model?.ExpectedSizeBytes ?? 0,
            Percentage: 0,
            BytesPerSecond: 0,
            EstimatedRemaining: null,
            StatusMessage: errorMessage,
            State: AiModelReadinessStatus.Error,
            ErrorMessage: errorMessage);

        NotifyStatusChanged();
    }

    private void NotifyStatusChanged()
    {
        try
        {
            OnStatusChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Eccezione durante l'invocazione di OnStatusChanged.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
        _semaphore.Dispose();
    }
}
