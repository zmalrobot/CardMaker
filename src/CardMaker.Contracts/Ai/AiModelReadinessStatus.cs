namespace CardMaker.Contracts.Ai;

/// <summary>
/// Stato globale di disponibilita e ciclo di vita del modello AI locale sul filesystem.
/// </summary>
public enum AiModelReadinessStatus
{
    /// <summary>La funzionalita AI e disabilitata dalle impostazioni amministrative.</summary>
    Disabled,

    /// <summary>Verifica della presenza e validita del file del modello in corso.</summary>
    Checking,

    /// <summary>Download del modello da repository remoto in corso.</summary>
    Downloading,

    /// <summary>Validazione dell'integrita del file (dimensione, header GGUF, checksum) in corso.</summary>
    Validating,

    /// <summary>Il modello e presente localmente su disco, validato e pronto all'uso.</summary>
    Ready,

    /// <summary>Si e verificato un errore durante il controllo, il download o la validazione del modello.</summary>
    Error,
}
