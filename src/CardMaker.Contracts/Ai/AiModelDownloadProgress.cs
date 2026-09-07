namespace CardMaker.Contracts.Ai;

/// <summary>
/// Dettaglio in tempo reale dell'avanzamento del download o validazione del modello AI.
/// </summary>
public sealed record AiModelDownloadProgress(
    string ModelKey,
    string ModelDisplayName,
    long BytesDownloaded,
    long TotalBytes,
    double Percentage,
    double BytesPerSecond,
    TimeSpan? EstimatedRemaining,
    string StatusMessage,
    AiModelReadinessStatus State,
    string? ErrorMessage = null);
