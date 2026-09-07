namespace CardMaker.Contracts.Ai;

/// <summary>
/// Stile narrativo o tematico applicabile alla generazione di titolo e testo della carta.
/// </summary>
public sealed record AiStyleDto(
    string Key,
    string Name,
    string Description,
    string PromptModifier);
