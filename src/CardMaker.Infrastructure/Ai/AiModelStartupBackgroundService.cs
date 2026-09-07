using CardMaker.Application.Ai;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Ai;

/// <summary>
/// Servizio in background che all''avvio dell''applicazione (sia Desktop sia Web)
/// innesca la verifica e l''eventuale download asincrono del modello AI,
/// senza bloccare l''avvio della UI ne l''elaborazione delle richieste.
/// </summary>
public sealed class AiModelStartupBackgroundService : BackgroundService
{
    private readonly IAiModelManager _modelManager;
    private readonly ILogger<AiModelStartupBackgroundService>? _logger;

    public AiModelStartupBackgroundService(
        IAiModelManager modelManager,
        ILogger<AiModelStartupBackgroundService>? logger = null)
    {
        _modelManager = modelManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Breve ritardo iniziale per consentire il completamento del bootstrap del frame Blazor/Host
        await Task.Delay(500, stoppingToken).ConfigureAwait(false);

        _logger?.LogInformation("AiModelStartupBackgroundService avviato: esecuzione verifica e predisposizione modello AI...");
        try
        {
            await _modelManager.EnsureActiveModelReadyAsync(forceDownload: false, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Arresto controllato dell'applicazione
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore non gestito durante lo startup del modello AI: {Message}", ex.Message);
        }
    }
}
