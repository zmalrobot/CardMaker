namespace CardMaker.Contracts.Ai;

/// <summary>
/// Richiesta di generazione di un'immagine / illustrazione per una carta da gioco tramite AI locale.
/// </summary>
public sealed class CardAiImageGenerationRequestDto
{
    public string GameKey { get; set; } = string.Empty;
    public string CardTypeKey { get; set; } = string.Empty;
    public string? CardTitle { get; set; }
    public string? CardDescriptionOrLore { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string StyleKey { get; set; } = "anime-tcg";
    public string? NegativePrompt { get; set; }
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int Steps { get; set; } = 20;
    public float CfgScale { get; set; } = 7.0f;
    public long Seed { get; set; } = -1;
}
