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

    event Action? OnStatusChanged;

    Task EnsureActiveModelReadyAsync(bool forceDownload = false, CancellationToken cancellationToken = default);
    void CancelDownload();
}
