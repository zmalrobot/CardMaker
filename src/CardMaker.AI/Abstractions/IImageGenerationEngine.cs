namespace CardMaker.AI.Abstractions;

/// <summary>
/// Contratto marcatore per future estensioni alla generazione di immagini (es. Stable Diffusion, SD.cpp).
/// Garantisce che CardMaker.AI sia progettata come libreria AI polivalente e non limitata solo a LLM testuali.
/// </summary>
public interface IImageGenerationEngine : IAiModelSession
{
    /// <summary>
    /// Esegue l'inferenza di generazione dell'immagine (text-to-image). Se il modello non e caricato,
    /// viene allocato automaticamente. Se gia caricato per lo stesso percorso, la sessione viene riutilizzata all'istante.
    /// </summary>
    Task<ImageGenerationResult> GenerateImageAsync(
        string modelPath,
        ImagePromptRequest request,
        int threads = 0,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
