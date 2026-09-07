namespace CardMaker.AI.Abstractions;

/// <summary>
/// Motore di generazione testuale guidato da LLM locali (es. llama.cpp).
/// Supporta streaming di progresso, vincolo di grammatica (GBNF) e deallocazione nativa controllata.
/// </summary>
public interface ITextGenerationEngine : IAiModelSession
{
    /// <summary>
    /// Esegue l'inferenza del testo. Se il modello non è ancora caricato, viene caricato automaticamente
    /// utilizzando il percorso specificato. Se già caricato, la sessione viene riutilizzata all'istante.
    /// </summary>
    Task<TextPromptResult> GenerateTextAsync(
        string modelPath,
        TextPromptRequest request,
        int contextSize = 2048,
        int threads = 0,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Azzera la cronologia o il contesto di conversazione della sessione senza scaricare il modello dalla memoria RAM.
    /// Utile per la generazione di alternative rapide.
    /// </summary>
    void ResetContext();
}

