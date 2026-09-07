using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Ai;

/// <summary>
/// Coordinatore dello stato di disponibilita dei modelli AI su disco.
/// Gestisce la verifica all'avvio, il download asincrono in background, l'avanzamento
/// e le notifiche di stato verso l'interfaccia utente.
/// </summary>
public interface IAiModelManager
{
    AiModelReadinessStatus CurrentStatus { get; }
    AiModelDownloadProgress? CurrentProgress { get; }
    AiModelDefinition? ActiveModel { get; }
    string? ActiveModelPath { get; }

    AiModelReadinessStatus CurrentImageStatus { get; }
    AiModelDownloadProgress? CurrentImageProgress { get; }
    AiModelDefinition? ActiveImageModel { get; }
    string? ActiveImageModelPath { get; }

    /// <summary>Stato combinato globale dei modelli AI abilitati (Ready, PartiallyReady, Downloading, ecc.).</summary>
    AiModelReadinessStatus OverallStatus { get; }

    /// <summary>Avanzamento del download attualmente in corso (testo o immagine), se attivo.</summary>
    AiModelDownloadProgress? ActiveProgress { get; }

    /// <summary>Riepilogo testuale conciso dello stato dei modelli per la UI (es. footer e banner).</summary>
    string OverallStatusSummary { get; }

    event Action? OnStatusChanged;

    Task EnsureActiveModelReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default);
    Task EnsureActiveImageModelReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default);
    Task EnsureAllActiveModelsReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default);
    void CancelDownload();
    void CancelImageDownload();
}
