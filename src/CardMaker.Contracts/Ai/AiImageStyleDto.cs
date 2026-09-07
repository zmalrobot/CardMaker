namespace CardMaker.Contracts.Ai;

/// <summary>
/// Stile visivo / artistico per la generazione dell'artwork di una carta.
/// </summary>
public sealed record AiImageStyleDto(
    string Key,
    string Name,
    string Description,
    string PromptModifier,
    string? SuggestedNegativePrompt = null);
