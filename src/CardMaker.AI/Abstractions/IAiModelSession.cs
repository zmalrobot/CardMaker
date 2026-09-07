namespace CardMaker.AI.Abstractions;

/// <summary>
/// Contratto base per il ciclo di vita di una sessione di modello AI locale (LLM o Image Generation).
/// Gestisce caricamento, deallocazione deterministica e tracciamento dello stato di allocazione nativa.
/// </summary>
public interface IAiModelSession : IAsyncDisposable, IDisposable
{
    /// <summary>Identificativo o percorso del modello attualmente associato alla sessione.</summary>
    string? ModelPath { get; }

    /// <summary>Indica se il modello e le sue strutture native (pesi, contesti) sono correntemente allocati in memoria.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Alloca e inizializza il modello in memoria (se non già caricato per lo stesso percorso).
    /// </summary>
    Task LoadModelAsync(string modelPath, int contextSize = 2048, int threads = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rilascia immediatamente il modello, il contesto nativo e i buffer allocati in RAM/VRAM.
    /// </summary>
    Task UnloadModelAsync();
}

