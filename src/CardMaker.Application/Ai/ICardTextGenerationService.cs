using CardMaker.AI.Abstractions;
using CardMaker.Contracts.Ai;

namespace CardMaker.Application.Ai;

/// <summary>
/// Servizio di applicazione per la generazione guidata di titolo e descrizione di carte da gioco tramite AI locale.
/// </summary>
public interface ICardTextGenerationService
{
    IReadOnlyList<AiStyleDto> GetAvailableStyles();

    Task<CardAiGenerationResultDto> GenerateCardTextAsync(
        CardAiGenerationRequestDto request,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);

    void ResetContext();

    Task UnloadModelAsync();
}
