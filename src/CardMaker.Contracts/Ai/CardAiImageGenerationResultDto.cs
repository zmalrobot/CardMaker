namespace CardMaker.Contracts.Ai;

/// <summary>
/// Risultato di una generazione di immagine completata dall'AI.
/// </summary>
public sealed class CardAiImageGenerationResultDto
{
    public byte[] ImageBytes { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
    public int Width { get; set; }
    public int Height { get; set; }
    public long SeedUsed { get; set; }
    public long DurationMs { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}
