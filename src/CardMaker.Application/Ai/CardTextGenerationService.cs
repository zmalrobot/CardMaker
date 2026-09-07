using System.Text.Json;
using System.Text.RegularExpressions;
using CardMaker.AI.Abstractions;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;

namespace CardMaker.Application.Ai;

/// <summary>
/// Implementazione del servizio applicativo di generazione testo carta.
/// Si occupa della contestualizzazione dei prompt per ciascun TCG, dell''invocazione del motore AI
/// e del parsing/sanitizzazione difensiva del JSON generato.
/// </summary>
public sealed class CardTextGenerationService : ICardTextGenerationService
{
    private readonly ITextGenerationEngine _engine;
    private readonly IAiConfigurationService _configService;
    private readonly ILogger<CardTextGenerationService>? _logger;

    private static readonly AiStyleDto[] AvailableStyles =
    [
        new("classico", "Classico", "Stile canonico e convenzionale del gioco di carte selezionato.",
            "Usa il tono e le convenzioni canoniche tipiche di questo specifico gioco di carte."),
        new("epico", "Epico / Mitologico", "Tono solenne, leggendario ed enfatico con vocabolario altisonante.",
            "Usa un tono solenne, leggendario, ancestrale e mitologico, con termini altisonanti e maestosi."),
        new("oscuro", "Oscuro / Gotico", "Tono inquietante, gotico, corrotto o sinistro.",
            "Usa un tono oscuro, sinistro, tenebroso e gotico, con atmosfere cupe e misteriose."),
        new("umoristico", "Umoristico / Satirico", "Tono ironico, divertente, bizzarro e parodico.",
            "Usa un tono ironico, stravagante, divertente e parodico, senza prenderti sul serio."),
        new("tecnico", "Tattico / Tecnico", "Tono metodico, calcolato e focalizzato sull''efficienza di gioco.",
            "Usa un tono analitico, freddo, matematico e fortemente focalizzato sulla precisione tattica.")
    ];

    public CardTextGenerationService(
        ITextGenerationEngine engine,
        IAiConfigurationService configService,
        ILogger<CardTextGenerationService>? logger = null)
    {
        _engine = engine;
        _configService = configService;
        _logger = logger;
    }

    public IReadOnlyList<AiStyleDto> GetAvailableStyles() => AvailableStyles;

    public async Task<CardAiGenerationResultDto> GenerateCardTextAsync(
        CardAiGenerationRequestDto request,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var isEnabled = await _configService.IsAiEnabledAsync(cancellationToken).ConfigureAwait(false);
        if (!isEnabled)
        {
            throw new InvalidOperationException("La funzionalita di generazione AI e attualmente disabilitata dalle impostazioni.");
        }

        var modelDef = await _configService.GetActiveModelDefinitionAsync(cancellationToken).ConfigureAwait(false);
        var modelPath = await _configService.GetActiveModelPathAsync(cancellationToken).ConfigureAwait(false);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                $"Il modello '{modelDef.DisplayName}' non e presente su disco nel percorso:\n'{modelPath}'.\n" +
                $"Scarica il file '{modelDef.FileName}' e collocalo nella cartella dei modelli per procedere.",
                modelPath);
        }

        var settings = await _configService.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request);

        var promptRequest = new TextPromptRequest(
            SystemPrompt: systemPrompt,
            UserPrompt: userPrompt,
            Temperature: 0.7f,
            MaxTokens: 512);

        _logger?.LogInformation("Avvio generazione testo carta con modello {ModelKey}...", modelDef.Key);

        var aiResult = await _engine.GenerateTextAsync(
            modelPath: modelPath,
            request: promptRequest,
            contextSize: modelDef.DefaultContextSize,
            threads: settings.CpuThreads,
            progress: progress,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var (title, description) = ParseAiJsonOutput(aiResult.Text, request.ExistingTitle);

        return new CardAiGenerationResultDto
        {
            Title = title,
            Description = description,
            RawOutput = aiResult.Text,
            DurationMs = aiResult.DurationMs,
            ModelUsed = modelDef.DisplayName
        };
    }

    public void ResetContext() => _engine.ResetContext();

    public Task UnloadModelAsync() => _engine.UnloadModelAsync();

    private static string BuildSystemPrompt()
    {
        return
            "Sei un lead game designer esperto di giochi di carte collezionabili (TCG).\n" +
            "Il tuo compito e creare un titolo (nome della carta) e una descrizione/effetto per una carta di gioco basandoti sull'idea dell'utente.\n" +
            "Devi rispondere ESCLUSIVAMENTE con un JSON valido strutturato esattamente come segue:\n" +
            "{\n" +
            "  \"title\": \"Nome della carta\",\n" +
            "  \"description\": \"Testo dell'effetto o descrizione narrativa della carta\"\n" +
            "}\n" +
            "REGOLE ASSOLUTE:\n" +
            "1. Rispondi solo ed esclusivamente con il blocco JSON.\n" +
            "2. Non inserire preamboli, spiegazioni, saluti o commenti al di fuori del JSON.\n" +
            "3. Il JSON deve essere valido e ben formato.\n" +
            "4. Usa la lingua italiana a meno che l'utente non richieda esplicitamente un'altra lingua.";
    }

    private static string BuildUserPrompt(CardAiGenerationRequestDto request)
    {
        var gameName = request.GameKey.ToLowerInvariant() switch
        {
            "yugioh" => "Yu-Gi-Oh!",
            "pokemon" => "Pokemon",
            "mtg" => "Magic: The Gathering",
            _ => string.IsNullOrWhiteSpace(request.GameKey) ? "Gioco di carte fantasy" : request.GameKey
        };

        var styleMod = "Stile standard.";
        for (var i = 0; i < AvailableStyles.Length; i++)
        {
            if (AvailableStyles[i].Key.Equals(request.StyleKey, StringComparison.OrdinalIgnoreCase))
            {
                styleMod = AvailableStyles[i].PromptModifier;
                break;
            }
        }

        var gameSpecificGuidance = request.GameKey.ToLowerInvariant() switch
        {
            "yugioh" =>
                "Per Yu-Gi-Oh!: Il titolo deve essere evocativo. La descrizione deve essere formulata come un effetto di gioco autentico " +
                "(es. condizioni di attivazione, costi con punto e virgola, effetti distinti per turno, o testo flavour epico se normale).",
            "pokemon" =>
                "Per Pokemon: Il titolo e il nome della creatura. La descrizione deve essere una combinazione di voce Pokedex e/o effetto/attacco distintivo.",
            "mtg" =>
                "Per Magic: The Gathering: Il titolo deve essere breve e d'impatto. La descrizione deve seguire le convenzioni del rules text o un flavor text evocativo.",
            _ => "Crea un nome di grande impatto e un testo di abilità o descrizione coerente."
        };

        var promptParts = new List<string>
        {
            $"Gioco di riferimento: {gameName}",
            $"Direttiva di stile: {styleMod}",
            $"Convenzioni di gioco: {gameSpecificGuidance}"
        };

        if (!string.IsNullOrWhiteSpace(request.CardTypeKey))
        {
            promptParts.Add($"Tipologia carta: {request.CardTypeKey}");
        }

        if (!string.IsNullOrWhiteSpace(request.ExistingTitle))
        {
            promptParts.Add($"Titolo provvisorio attuale: \"{request.ExistingTitle}\" (puoi migliorarlo o ispirarti ad esso)");
        }

        promptParts.Add($"Idea o tema dell'utente: {request.UserPrompt}");
        promptParts.Add("Genera ora l'oggetto JSON con 'title' e 'description':");

        return string.Join("\n", promptParts);
    }

    /// <summary>
    /// Estrae difensivamente 'title' e 'description' dalla risposta testuale dell'AI,
    /// gestendo blocchi markdown, preamboli o malformazioni parziali.
    /// </summary>
    public static (string Title, string Description) ParseAiJsonOutput(string rawOutput, string? fallbackTitle)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return (fallbackTitle ?? "Nuova Carta", string.Empty);
        }

        var trimmed = rawOutput.Trim();

        // Rimuove eventuali blocchi ```json ... ```
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var lines = trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var filteredLines = lines
                .Where(l => !l.StartsWith("```", StringComparison.Ordinal))
                .ToArray();
            trimmed = string.Join("\n", filteredLines);
        }

        // Cerca la prima parentesi graffa '{' e l'ultima '}'
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            var jsonCandidate = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
            try
            {
                using var doc = JsonDocument.Parse(jsonCandidate);
                var root = doc.RootElement;

                var title = string.Empty;
                var description = string.Empty;

                if (root.TryGetProperty("title", out var titleElem) || root.TryGetProperty("Title", out titleElem))
                {
                    title = titleElem.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("description", out var descElem) || root.TryGetProperty("Description", out descElem))
                {
                    description = descElem.GetString() ?? string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(description))
                {
                    return (
                        string.IsNullOrWhiteSpace(title) ? (fallbackTitle ?? "Nuova Carta") : title.Trim(),
                        description.Trim()
                    );
                }
            }
            catch (JsonException)
            {
                // Fallback con regex se il JSON era parzialmente malformato
            }
        }

        // Fallback tramite Regex per trovare "title": "..." e "description": "..."
        var titleMatch = Regex.Match(rawOutput, "\"([tT]itle)\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.None, TimeSpan.FromSeconds(1));
        var descMatch = Regex.Match(rawOutput, "\"([dD]escription)\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.None, TimeSpan.FromSeconds(1));

        var parsedTitle = titleMatch.Success ? titleMatch.Groups[2].Value : (fallbackTitle ?? "Nuova Carta");
        var parsedDesc = descMatch.Success ? descMatch.Groups[2].Value : rawOutput.Trim();

        return (parsedTitle.Trim(), parsedDesc.Trim());
    }
}
