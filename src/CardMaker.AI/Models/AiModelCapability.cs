namespace CardMaker.AI.Models;

/// <summary>
/// Tipologia di capacita operativa svolta dal modello AI locale.
/// </summary>
public enum AiModelCapability
{
    /// <summary>Generazione di testo, titoli, descrizioni, effetti e attributi di gioco (LLM).</summary>
    Text,

    /// <summary>Generazione di illustrazioni e artwork grafici (Diffusione).</summary>
    Image
}
