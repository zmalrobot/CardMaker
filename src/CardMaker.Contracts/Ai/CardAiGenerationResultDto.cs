namespace CardMaker.Contracts.Ai;

/// <summary>
/// Risultato strutturato generato dall'AI pronto per essere popolato nei campi della carta.
/// </summary>
public sealed class CardAiGenerationResultDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? Attack { get; set; }
    public int? Defense { get; set; }
    public string RawOutput { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}
