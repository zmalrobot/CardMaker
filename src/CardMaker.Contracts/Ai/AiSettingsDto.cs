namespace CardMaker.Contracts.Ai;

/// <summary>
/// Impostazioni di configurazione per la generazione assistita da intelligenza artificiale.
/// </summary>
public sealed class AiSettingsDto
{
    /// <summary>
    /// Flag master per abilitare o disabilitare la funzionalita AI nell'intera applicazione.
    /// Se impostato a false, la UI non mostra pulsanti AI e nessun modello viene caricato in memoria.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Chiave del modello selezionato dall'amministratore ("Auto" per selezione automatica in base alla RAM).
    /// </summary>
    public string SelectedModelKey { get; set; } = "Auto";

    /// <summary>
    /// Numero di thread CPU allocati per l'inferenza (0 = rilevamento automatico Environment.ProcessorCount - 1).
    /// </summary>
    public int CpuThreads { get; set; }

    /// <summary>
    /// Percorso personalizzato per la cartella dei modelli GGUF (se null, usa DataRoot/models).
    /// </summary>
    public string? CustomModelsDirectory { get; set; }
}
