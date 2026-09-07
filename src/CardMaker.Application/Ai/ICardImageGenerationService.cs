using CardMaker.AI.Abstractions;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Ai;

/// <summary>
/// Contratto del servizio applicativo di generazione illustrazioni e artwork per carte da gioco.
/// Gestisce la composizione contestuale dei prompt, stili artistici TCG e l''invocazione del motore AI.
/// </summary>
public interface ICardImageGenerationService
{
    IReadOnlyList<AiImageStyleDto> GetAvailableStyles();

    Task<CardAiImageGenerationResultDto> GenerateArtworkAsync(
        CardAiImageGenerationRequestDto request,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);

    Task UnloadModelAsync();
}
