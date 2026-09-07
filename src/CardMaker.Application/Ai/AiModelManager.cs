using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;

namespace CardMaker.Application.Ai;

/// <summary>
/// Implementazione thread-safe del coordinatore dello stato dei modelli AI (testo e immagini).
/// Gestisce il ciclo: Verifica configurazione -> Rilevamento locale -> Download / Resume -> Validazione -> Ready.
/// </summary>
public sealed class AiModelManager : IAiModelManager, IDisposable
{
    private readonly IAiConfigurationService _configService;
    private readonly IAiModelDownloader _downloader;
    private readonly ILogger<AiModelManager>? _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SemaphoreSlim _imageSemaphore = new(1, 1);

    private CancellationTokenSource? _downloadCts;
    private CancellationTokenSource? _imageDownloadCts;
    private bool _disposed;

    public AiModelReadinessStatus CurrentStatus { get; private set; } = AiModelReadinessStatus.Checking;
    public AiModelDownloadProgress? CurrentProgress { get; private set; }
    public AiModelDefinition? ActiveModel { get; private set; }
    public string? ActiveModelPath { get; private set; }

    public AiModelReadinessStatus CurrentImageStatus { get; private set; } = AiModelReadinessStatus.Checking;
    public AiModelDownloadProgress? CurrentImageProgress { get; private set; }
    public AiModelDefinition? ActiveImageModel { get; private set; }
    public string? ActiveImageModelPath { get; private set; }

    public AiModelDownloadProgress? ActiveProgress
    {
        get
        {
            if (CurrentStatus == AiModelReadinessStatus.Downloading && CurrentProgress != null)
            {
                return CurrentProgress;
            }
            if (CurrentImageStatus == AiModelReadinessStatus.Downloading && CurrentImageProgress != null)
            {
                return CurrentImageProgress;
            }
            return CurrentProgress ?? CurrentImageProgress;
        }
    }

    public AiModelReadinessStatus OverallStatus
    {
        get
        {
            var textStatus = CurrentStatus;
            var imageStatus = CurrentImageStatus;

            // Se entrambi disabilitati o non richiesti
            if ((textStatus == AiModelReadinessStatus.Disabled || textStatus == AiModelReadinessStatus.NotRequired) &&
                (imageStatus == AiModelReadinessStatus.Disabled || imageStatus == AiModelReadinessStatus.NotRequired))
            {
                return AiModelReadinessStatus.Disabled;
            }

            var textEnabled = textStatus != AiModelReadinessStatus.Disabled && textStatus != AiModelReadinessStatus.NotRequired;
            var imageEnabled = imageStatus != AiModelReadinessStatus.Disabled && imageStatus != AiModelReadinessStatus.NotRequired;

            if (textEnabled && !imageEnabled)
            {
                return textStatus;
            }

            if (!textEnabled && imageEnabled)
            {
                return imageStatus;
            }

            if (textStatus == AiModelReadinessStatus.Ready && imageStatus == AiModelReadinessStatus.Ready)
            {
                return AiModelReadinessStatus.Ready;
            }

            if (textStatus == AiModelReadinessStatus.Ready || imageStatus == AiModelReadinessStatus.Ready)
            {
                return AiModelReadinessStatus.PartiallyReady;
            }

            if (textStatus == AiModelReadinessStatus.Downloading || imageStatus == AiModelReadinessStatus.Downloading)
            {
                return AiModelReadinessStatus.Downloading;
            }

            if (textStatus == AiModelReadinessStatus.Validating || imageStatus == AiModelReadinessStatus.Validating)
            {
                return AiModelReadinessStatus.Validating;
            }

            if (textStatus == AiModelReadinessStatus.Error || imageStatus == AiModelReadinessStatus.Error)
            {
                return AiModelReadinessStatus.Error;
            }

            return AiModelReadinessStatus.Checking;
        }
    }

    public string OverallStatusSummary
    {
        get
        {
            var overall = OverallStatus;
            return overall switch
            {
                AiModelReadinessStatus.Ready => "Tutti i modelli AI pronti",
                AiModelReadinessStatus.Disabled => "AI disabilitata",
                AiModelReadinessStatus.PartiallyReady => CurrentStatus == AiModelReadinessStatus.Ready
                    ? $"Testo pronto · Immagini: {GetStatusLabel(CurrentImageStatus)}"
                    : $"Testo: {GetStatusLabel(CurrentStatus)} · Immagini pronte",
                AiModelReadinessStatus.Downloading => ActiveProgress != null
                    ? $"Download {ActiveProgress.ModelDisplayName} ({ActiveProgress.Percentage:0.0}%)"
                    : "Download in corso...",
                AiModelReadinessStatus.Validating => "Validazione file in corso...",
                AiModelReadinessStatus.Checking => "Verifica modelli locali...",
                AiModelReadinessStatus.Error => "Errore nei modelli AI",
                _ => overall.ToString()
            };
        }
    }

    private static string GetStatusLabel(AiModelReadinessStatus status) => status switch
    {
        AiModelReadinessStatus.Ready => "Pronto",
        AiModelReadinessStatus.Downloading => "In download",
        AiModelReadinessStatus.Validating => "Validazione",
        AiModelReadinessStatus.Checking => "Verifica",
        AiModelReadinessStatus.Disabled => "Disabilitato",
        AiModelReadinessStatus.Error => "Errore",
        _ => status.ToString()
    };

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
                _logger?.LogInformation("Funzionalita AI disabilitata: nessun controllo o download del modello testo richiesto.");
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

            await _downloader.DownloadModelAsync(
                modelDef,
                modelPath,
                progressReporter,
                _downloadCts.Token).ConfigureAwait(false);

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
            _logger?.LogError(ex, "Errore imprevisto durante la preparazione del modello AI.");
            SetErrorState($"Errore: {ex.Message}", ActiveModel);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task EnsureActiveImageModelReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _imageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var isEnabled = await _configService.IsImageGenerationEnabledAsync(cancellationToken).ConfigureAwait(false);
            if (!isEnabled)
            {
                SetImageState(AiModelReadinessStatus.Disabled, null);
                _logger?.LogInformation("Generazione immagini disabilitata: nessun controllo o download del modello immagini richiesto.");
                return;
            }

            SetImageState(AiModelReadinessStatus.Checking, "Verifica del modello immagini locale...");

            var modelDef = await _configService.GetActiveImageModelDefinitionAsync(cancellationToken).ConfigureAwait(false);
            var modelPath = await _configService.GetActiveImageModelPathAsync(cancellationToken).ConfigureAwait(false);

            ActiveImageModel = modelDef;
            ActiveImageModelPath = modelPath;

            if (!forceDownload && _downloader.ValidateModelFile(modelDef, modelPath, out _))
            {
                _logger?.LogInformation("Modello immagini '{ModelKey}' presente e valido in '{ModelPath}'. Stato: Ready.", modelDef.Key, modelPath);
                SetImageState(AiModelReadinessStatus.Ready, "Modello immagini pronto all'uso.", modelDef);
                return;
            }

            _logger?.LogInformation("Modello immagini '{ModelKey}' non presente o non valido. Avvio download asincrono...", modelDef.Key);

            _imageDownloadCts?.Cancel();
            _imageDownloadCts?.Dispose();
            _imageDownloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var progressReporter = new Progress<AiModelDownloadProgress>(p =>
            {
                if (CurrentImageStatus == AiModelReadinessStatus.Downloading)
                {
                    CurrentImageProgress = p;
                    NotifyStatusChanged();
                }
            });

            SetImageState(AiModelReadinessStatus.Downloading, "Avvio download modello immagini...", modelDef);

            await _downloader.DownloadModelAsync(
                modelDef,
                modelPath,
                progressReporter,
                _imageDownloadCts.Token).ConfigureAwait(false);

            SetImageState(AiModelReadinessStatus.Validating, "Validazione file scaricato in corso...", modelDef);

            if (_downloader.ValidateModelFile(modelDef, modelPath, out var validationError))
            {
                _logger?.LogInformation("Download e validazione del modello immagini '{ModelKey}' completati con successo. Stato: Ready.", modelDef.Key);
                SetImageState(AiModelReadinessStatus.Ready, "Modello immagini scaricato, validato e pronto all'uso.", modelDef);
            }
            else
            {
                var errorMsg = validationError ?? "Validazione di integrita del file scaricato fallita.";
                _logger?.LogError("Errore di validazione del modello immagini '{ModelKey}': {Error}", modelDef.Key, errorMsg);
                SetImageErrorState(errorMsg, modelDef);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Download del modello immagini interrotto dall'utente.");
            SetImageState(AiModelReadinessStatus.Error, "Download interrotto dall'utente.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore imprevisto durante la preparazione del modello immagini.");
            SetImageErrorState($"Errore: {ex.Message}", ActiveImageModel);
        }
        finally
        {
            _imageSemaphore.Release();
        }
    }

    public async Task EnsureAllActiveModelsReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger?.LogInformation("Avvio verifica centralizzata all'avvio dei modelli AI attivi...");
        await Task.WhenAll(
            EnsureActiveModelReadyAsync(forceDownload, cancellationToken),
            EnsureActiveImageModelReadyAsync(forceDownload, cancellationToken)
        ).ConfigureAwait(false);
        _logger?.LogInformation("Verifica centralizzata modelli AI completata. Stato complessivo: {OverallStatus}", OverallStatus);
    }

    public void CancelDownload()
    {
        _downloadCts?.Cancel();
    }

    public void CancelImageDownload()
    {
        _imageDownloadCts?.Cancel();
    }

    private void SetState(AiModelReadinessStatus status, string? message, AiModelDefinition? model = null)
    {
        CurrentStatus = status;
        CurrentProgress = new AiModelDownloadProgress(
            ModelKey: model?.Key ?? ActiveModel?.Key ?? string.Empty,
            ModelDisplayName: model?.DisplayName ?? ActiveModel?.DisplayName ?? string.Empty,
            BytesDownloaded: status == AiModelReadinessStatus.Ready && model != null ? model.ExpectedSizeBytes : 0,
            TotalBytes: model?.ExpectedSizeBytes ?? ActiveModel?.ExpectedSizeBytes ?? 0,
            Percentage: status == AiModelReadinessStatus.Ready ? 100.0 : 0.0,
            BytesPerSecond: 0,
            EstimatedRemaining: null,
            StatusMessage: message ?? status.ToString(),
            State: status,
            ErrorMessage: null);

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

    private void SetImageState(AiModelReadinessStatus status, string? message, AiModelDefinition? model = null)
    {
        CurrentImageStatus = status;
        CurrentImageProgress = new AiModelDownloadProgress(
            ModelKey: model?.Key ?? ActiveImageModel?.Key ?? string.Empty,
            ModelDisplayName: model?.DisplayName ?? ActiveImageModel?.DisplayName ?? string.Empty,
            BytesDownloaded: status == AiModelReadinessStatus.Ready && model != null ? model.ExpectedSizeBytes : 0,
            TotalBytes: model?.ExpectedSizeBytes ?? ActiveImageModel?.ExpectedSizeBytes ?? 0,
            Percentage: status == AiModelReadinessStatus.Ready ? 100.0 : 0.0,
            BytesPerSecond: 0,
            EstimatedRemaining: null,
            StatusMessage: message ?? status.ToString(),
            State: status,
            ErrorMessage: null);

        NotifyStatusChanged();
    }

    private void SetImageErrorState(string errorMessage, AiModelDefinition? model)
    {
        CurrentImageStatus = AiModelReadinessStatus.Error;
        CurrentImageProgress = new AiModelDownloadProgress(
            ModelKey: model?.Key ?? ActiveImageModel?.Key ?? string.Empty,
            ModelDisplayName: model?.DisplayName ?? ActiveImageModel?.DisplayName ?? string.Empty,
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
        _imageDownloadCts?.Cancel();
        _imageDownloadCts?.Dispose();
        _semaphore.Dispose();
        _imageSemaphore.Dispose();
    }
}
