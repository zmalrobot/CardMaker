using CardMaker.AI.Abstractions;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;

namespace CardMaker.Application.Ai;

/// <summary>
/// Servizio applicativo che coordina la generazione di artwork per carte collezionabili.
/// </summary>
public sealed class CardImageGenerationService : ICardImageGenerationService
{
    private readonly IImageGenerationEngine _engine;
    private readonly IAiConfigurationService _configService;
    private readonly ILogger<CardImageGenerationService>? _logger;

    public const string DefaultNegativePrompt =
        "text, watermark, signature, artist name, border, card frame, blurry, low quality, " +
        "deformed, extra limbs, bad anatomy, duplicate, out of frame, cropped, jpeg artifacts, " +
        "worst quality, ugly, mutation, disfigured";

    private static readonly AiImageStyleDto[] AvailableStyles =
    [
        new("anime-tcg", "Anime TCG / Classic Manga",
            "Stile anime giapponese con line art nitida, cel shading e colori accesi.",
            "masterpiece, trading card game artwork, anime style, highly detailed cel shading, clean crisp lineart, vibrant colors"),

        new("fantasy-oil", "Fantasy Epico / Olio su Tela",
            "Pittura a olio solenne e materica con illuminazione drammatica stile MTG.",
            "masterpiece, epic fantasy oil painting, detailed brushwork, volumetric lighting, rich color palette, classical trading card illustration"),

        new("vibrant-monster", "Creatura Vivace / Pokémon Style",
            "Stile creatura dinamica con colori luminosi e ambientazione naturale.",
            "masterpiece, vibrant monster card illustration, colorful creature, dynamic action pose, beautiful scenic background"),

        new("retro-vintage", "Incisione Retro / Vintage Woodcut",
            "Stile xilografia medievale con pergamena antica e tratteggio a inchiostro.",
            "masterpiece, medieval woodcut etching, antique parchment background, vintage occult card illustration, crosshatching ink lineart"),

        new("cyber-scifi", "Cyberpunk / Mecha Sci-Fi",
            "Atmosfera futuristica con luci al neon, armature cromate e tecnologia avanzata.",
            "masterpiece, futuristic sci-fi mecha card art, neon cyber glow, intricate metallic armor plates, dystopian atmosphere")
    ];

    public CardImageGenerationService(
        IImageGenerationEngine engine,
        IAiConfigurationService configService,
        ILogger<CardImageGenerationService>? logger = null)
    {
        _engine = engine;
        _configService = configService;
        _logger = logger;
    }

    public IReadOnlyList<AiImageStyleDto> GetAvailableStyles() => AvailableStyles;

    public async Task<CardAiImageGenerationResultDto> GenerateArtworkAsync(
        CardAiImageGenerationRequestDto request,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var isEnabled = await _configService.IsImageGenerationEnabledAsync(cancellationToken).ConfigureAwait(false);
        if (!isEnabled)
        {
            throw new InvalidOperationException("La funzionalita di generazione immagini AI e disabilitata dalle impostazioni.");
        }

        var modelDef = await _configService.GetActiveImageModelDefinitionAsync(cancellationToken).ConfigureAwait(false);
        var modelPath = await _configService.GetActiveImageModelPathAsync(cancellationToken).ConfigureAwait(false);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                $"Il modello di diffusione '{modelDef.DisplayName}' non e presente su disco in '{modelPath}'.\n" +
                $"Scarica il file '{modelDef.FileName}' e collocalo nella cartella dei modelli per procedere.",
                modelPath);
        }

        var settings = await _configService.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        var positivePrompt = BuildPositivePrompt(request);
        var negativePrompt = !string.IsNullOrWhiteSpace(request.NegativePrompt)
            ? $"{DefaultNegativePrompt}, {request.NegativePrompt}"
            : DefaultNegativePrompt;

        var promptRequest = new ImagePromptRequest(
            PositivePrompt: positivePrompt,
            NegativePrompt: negativePrompt,
            Width: request.Width > 0 ? request.Width : modelDef.DefaultWidth,
            Height: request.Height > 0 ? request.Height : modelDef.DefaultHeight,
            Steps: request.Steps > 0 ? request.Steps : modelDef.DefaultSteps,
            CfgScale: request.CfgScale > 0 ? request.CfgScale : 7.0f,
            Seed: request.Seed);

        _logger?.LogInformation("Avvio generazione immagine TCG con modello {ModelKey}...", modelDef.Key);

        var result = await _engine.GenerateImageAsync(
            modelPath: modelPath,
            request: promptRequest,
            threads: settings.CpuThreads,
            progress: progress,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new CardAiImageGenerationResultDto
        {
            ImageBytes = result.PngBytes,
            ContentType = "image/png",
            Width = result.Width,
            Height = result.Height,
            SeedUsed = result.SeedUsed,
            DurationMs = result.DurationMs,
            ModelUsed = modelDef.DisplayName
        };
    }

    public Task UnloadModelAsync() => _engine.UnloadModelAsync();

    public static string BuildPositivePrompt(CardAiImageGenerationRequestDto request)
    {
        var styleMod = "masterpiece, trading card game artwork, highly detailed fantasy illustration";
        for (var i = 0; i < AvailableStyles.Length; i++)
        {
            if (AvailableStyles[i].Key.Equals(request.StyleKey, StringComparison.OrdinalIgnoreCase))
            {
                styleMod = AvailableStyles[i].PromptModifier;
                break;
            }
        }

        var gameContext = request.GameKey.ToLowerInvariant() switch
        {
            "yugioh" => "Yu-Gi-Oh! card art style",
            "pokemon" => "Pokemon creature card art style",
            "mtg" => "Magic the Gathering card fantasy style",
            _ => "trading card game illustration"
        };

        var parts = new List<string> { styleMod, gameContext };

        if (!string.IsNullOrWhiteSpace(request.CardTitle))
        {
            parts.Add($"subject: {request.CardTitle}");
        }

        if (!string.IsNullOrWhiteSpace(request.CardDescriptionOrLore))
        {
            var truncatedLore = request.CardDescriptionOrLore.Length > 120
                ? request.CardDescriptionOrLore[..120]
                : request.CardDescriptionOrLore;
            parts.Add($"visual theme: {truncatedLore}");
        }

        if (!string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            parts.Add(request.UserPrompt);
        }

        parts.Add("centered composition, dramatic lighting, 8k resolution, detailed background");

        return string.Join(", ", parts);
    }
}
