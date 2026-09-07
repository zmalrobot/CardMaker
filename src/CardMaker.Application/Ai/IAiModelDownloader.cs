using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Ai;

/// <summary>
/// Contratto per il download asincrono e sicuro dei file GGUF dei modelli AI con supporto a resume,
/// verifica dello spazio disco, calcolo del progresso e validazione di integrita.
/// </summary>
public interface IAiModelDownloader
{
    Task DownloadModelAsync(
        AiModelDefinition model,
        string targetFilePath,
        IProgress<AiModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    bool ValidateModelFile(AiModelDefinition model, string targetFilePath, out string? validationError);
}
