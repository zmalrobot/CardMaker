using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Ai;

/// <summary>
/// Fornisce informazioni sul profilo hardware della macchina host per consigliare il modello AI ottimale.
/// </summary>
public interface IHardwareProfileDetector
{
    /// <summary>Rileva la memoria RAM fisica totale installata (in byte).</summary>
    long GetTotalPhysicalMemoryBytes();

    /// <summary>Costruisce il riepilogo del profilo hardware e lo stato del modello consigliato.</summary>
    HardwareProfileDto GetHardwareProfile(string modelsDirectory);
}
