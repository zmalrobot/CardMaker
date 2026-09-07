namespace CardMaker.AI.Models;

/// <summary>
/// Definizione immutabile dei metadati di un modello GGUF supportato da CardMaker.AI,
/// comprendente parametri di inferenza, requisiti hardware e coordinate per il download e la validazione.
/// </summary>
public sealed record AiModelDefinition(
    string Key,
    string DisplayName,
    string FileName,
    int RecommendedRamGb,
    double EstimatedMemoryUsageGb,
    string Quantization,
    string Family,
    string DownloadUrl,
    string DownloadUrlDirect,
    long ExpectedSizeBytes,
    string Description,
    int DefaultContextSize = 2048,
    string? ExpectedSha256 = null,
    long MinFreeDiskSpaceBytes = 0,
    string Version = "1.0",
    int DefaultSteps = 20,
    int DefaultWidth = 512,
    int DefaultHeight = 512);
