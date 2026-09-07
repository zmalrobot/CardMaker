using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Ai;

/// <summary>
/// Gestione delle impostazioni AI persistenti dell'applicazione e risoluzione del modello attivo.
/// </summary>
public interface IAiConfigurationService
{
    Task<AiSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(AiSettingsDto settings, CancellationToken cancellationToken = default);
    Task<bool> IsAiEnabledAsync(CancellationToken cancellationToken = default);
    Task<bool> IsImageGenerationEnabledAsync(CancellationToken cancellationToken = default);
    Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default);
    Task<AiModelDefinition> GetActiveModelDefinitionAsync(CancellationToken cancellationToken = default);
    Task<string> GetActiveImageModelPathAsync(CancellationToken cancellationToken = default);
    Task<AiModelDefinition> GetActiveImageModelDefinitionAsync(CancellationToken cancellationToken = default);
    string GetModelsDirectory();
}
