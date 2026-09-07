namespace CardMaker.AI.Abstractions;

/// <summary>
/// Parametri di richiesta per la generazione locale di un'immagine tramite modelli di diffusione (es. SD, SDXL).
/// </summary>
public sealed record ImagePromptRequest(
    string PositivePrompt,
    string? NegativePrompt = null,
    int Width = 512,
    int Height = 512,
    int Steps = 20,
    float CfgScale = 7.0f,
    long Seed = -1,
    string? Sampler = "euler_a");

/// <summary>
/// Risultato della generazione di un'immagine restituito dal motore di inferenza visiva.
/// </summary>
public sealed record ImageGenerationResult(
    byte[] PngBytes,
    int Width,
    int Height,
    long SeedUsed,
    long DurationMs,
    string ModelUsed);
