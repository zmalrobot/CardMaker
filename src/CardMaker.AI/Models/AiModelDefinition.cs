namespace CardMaker.AI.Models;

/// <summary>
/// Definizione immutabile dei metadati di un modello GGUF supportato da CardMaker.AI.
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
    string Description,
    int DefaultContextSize = 2048);
