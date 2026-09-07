namespace CardMaker.Contracts.Ai;

/// <summary>
/// Dettaglio del profilo hardware rilevato sulla macchina host e associazione con il modello consigliato.
/// </summary>
public sealed record HardwareProfileDto(
    long TotalPhysicalMemoryBytes,
    double TotalPhysicalMemoryGb,
    string RecommendedModelKey,
    string RecommendedModelDisplayName,
    bool IsModelDownloaded,
    string ModelPath);
