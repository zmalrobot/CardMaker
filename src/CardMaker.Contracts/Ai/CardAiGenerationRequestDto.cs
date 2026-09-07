namespace CardMaker.Contracts.Ai;

/// <summary>
/// Richiesta di generazione AI per una carta di gioco specifica.
/// </summary>
public sealed class CardAiGenerationRequestDto
{
    public string GameKey { get; set; } = string.Empty;
    public string CardTypeKey { get; set; } = string.Empty;
    public string StyleKey { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string? ExistingTitle { get; set; }
}
