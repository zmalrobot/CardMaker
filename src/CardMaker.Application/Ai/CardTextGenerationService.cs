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

        var (title, description, attack, defense) = ParseAiJsonOutput(aiResult.Text, request.ExistingTitle);

        return new CardAiGenerationResultDto
        {
            Title = title,
            Description = description,
            Attack = attack,
            Defense = defense,
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
            "Sei un lead game designer esperto di giochi di carte collezionabili (TCG, es. Yu-Gi-Oh!, Magic: The Gathering, Pokémon).\n" +
            "Il tuo compito e creare il contenuto testuale e i valori di una carta di gioco basandoti sull'idea dell'utente, sul gioco e sulla tipologia di carta.\n" +
            "Devi rispondere ESCLUSIVAMENTE con un blocco JSON valido formattato esattamente come segue:\n" +
            "{\n" +
            "  \"title\": \"Nome della carta\",\n" +
            "  \"description\": \"Testo dell'effetto o descrizione narrativa della carta\",\n" +
            "  \"attack\": 2500,\n" +
            "  \"defense\": 2000\n" +
            "}\n" +
            "REGOLE CRUCIALI:\n" +
            "1. Rispondi solo ed esclusivamente con il blocco JSON valido, senza preamboli, saluti o markdown al di fuori del JSON.\n" +
            "2. Se la carta non e un mostro o creatura (es. Carta Magia, Carta Trappola, Incantesimo), imposta \"attack\": null e \"defense\": null (oppure omettili).\n" +
            "3. Se la carta e un mostro o creatura, fornisci valori numerici interi realistici e bilanciati per \"attack\" e \"defense\" (per Yu-Gi-Oh! tipicamente tra 0 e 4000 a multipli di 50/100, per Magic tra 0 e 15).\n" +
            "4. Usa la lingua italiana a meno che l'utente non richieda esplicitamente un'altra lingua.";
    }

    private static string BuildUserPrompt(CardAiGenerationRequestDto request)
    {
        var gameName = request.GameKey.ToLowerInvariant() switch
        {
            "yugioh" => "Yu-Gi-Oh!",
            "pokemon" => "Pokémon",
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
                "Per Yu-Gi-Oh!: Il titolo deve essere evocativo. Le carte devono rispettare il linguaggio e la sintassi tipica del gioco.",
            "pokemon" =>
                "Per Pokémon: Il titolo e il nome della creatura. La descrizione deve essere una combinazione di voce Pokédex e/o effetto/attacco distintivo.",
            "mtg" =>
                "Per Magic: The Gathering: Il titolo deve essere breve e d'impatto. La descrizione deve seguire le convenzioni del rules text o flavor text.",
            _ => "Crea un nome di grande impatto e un testo coerente."
        };

        var promptParts = new List<string>
        {
            $"Gioco di riferimento: {gameName}",
            $"Direttiva di stile: {styleMod}",
            $"Convenzioni generali: {gameSpecificGuidance}"
        };

        var typeLabel = !string.IsNullOrWhiteSpace(request.CardTypeName)
            ? request.CardTypeName
            : request.CardTypeKey;

        var isNormalMonster = request.CardTypeKey.Contains("normal", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(request.CardTypeName) && (
                request.CardTypeName.Contains("normale", StringComparison.OrdinalIgnoreCase) ||
                request.CardTypeName.Contains("normal", StringComparison.OrdinalIgnoreCase)));

        var isSpellOrTrap = request.CardTypeKey.Contains("spell", StringComparison.OrdinalIgnoreCase)
            || request.CardTypeKey.Contains("trap", StringComparison.OrdinalIgnoreCase)
            || request.CardTypeKey.Contains("sorcery", StringComparison.OrdinalIgnoreCase)
            || request.CardTypeKey.Contains("instant", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(request.CardTypeName) && (
                request.CardTypeName.Contains("magia", StringComparison.OrdinalIgnoreCase) ||
                request.CardTypeName.Contains("trappola", StringComparison.OrdinalIgnoreCase)));

        if (isNormalMonster)
        {
            promptParts.Add($"Tipologia carta selezionata: {typeLabel} (MOSTRO NORMALE SENZA EFFETTO)");
            promptParts.Add(
                "VINCOLO TASSATIVO TIPOLOGIA - MOSTRO NORMALE:\n" +
                "- Questa carta e RIGOROSAMENTE un Mostro Normale! NON DEVE AVERE ALCUN EFFETTO DI GIOCO.\n" +
                "- Vietate frasi regolamentari (es. NON scrivere 'Una volta per turno...', 'Puoi...', 'Quando questa carta viene evocata...', costi, condizioni o trigger).\n" +
                "- La 'description' DEVE ESSERE ESCLUSIVAMENTE un testo di colore / lore narrativa descrittiva sul mostro, la sua leggenda o la sua potenza.\n" +
                "- DEVI specificare valori coerenti per 'attack' e 'defense'.");
        }
        else if (isSpellOrTrap)
        {
            promptParts.Add($"Tipologia carta selezionata: {typeLabel}");
            promptParts.Add(
                "VINCOLO TIPOLOGIA - MAGIA / TRAPPOLA:\n" +
                "- Questa carta e una Magia o Trappola. La 'description' deve descrivere chiaramente l'effetto della carta.\n" +
                "- NON deve avere valori di combattimento ('attack' e 'defense' devono essere null).");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(typeLabel))
            {
                promptParts.Add($"Tipologia carta selezionata: {typeLabel}");
            }

            if (request.SupportsAttack || request.SupportsDefense || request.CardTypeKey.Contains("monster", StringComparison.OrdinalIgnoreCase) || request.CardTypeKey.Contains("creature", StringComparison.OrdinalIgnoreCase))
            {
                promptParts.Add(
                    "VINCOLO TIPOLOGIA - MOSTRO CON EFFETTO / CREATURA:\n" +
                    "- La 'description' deve contenere un effetto di gioco dettagliato e regolamentare.\n" +
                    "- Fornisci valori adeguati per 'attack' e 'defense'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ExistingTitle))
        {
            promptParts.Add($"Titolo provvisorio attuale: \"{request.ExistingTitle}\" (puoi migliorarlo o trarre ispirazione)");
        }

        promptParts.Add($"Idea o tema specificato dall'utente: {request.UserPrompt}");
        promptParts.Add("Genera ora l'oggetto JSON con 'title', 'description', 'attack' e 'defense':");

        return string.Join("\n", promptParts);
    }

    /// <summary>
    /// Estrae difensivamente 'title', 'description', 'attack' e 'defense' dalla risposta testuale dell'AI,
    /// gestendo blocchi markdown, preamboli o malformazioni parziali.
    /// </summary>
    public static (string Title, string Description, int? Attack, int? Defense) ParseAiJsonOutput(string rawOutput, string? fallbackTitle)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return (fallbackTitle ?? "Nuova Carta", string.Empty, null, null);
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
                int? attack = null;
                int? defense = null;

                if (root.TryGetProperty("title", out var titleElem) || root.TryGetProperty("Title", out titleElem))
                {
                    title = titleElem.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("description", out var descElem) || root.TryGetProperty("Description", out descElem))
                {
                    description = descElem.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("attack", out var atkElem) || root.TryGetProperty("atk", out atkElem) || root.TryGetProperty("Attack", out atkElem) || root.TryGetProperty("power", out atkElem))
                {
                    if (atkElem.ValueKind == JsonValueKind.Number && atkElem.TryGetInt32(out var num))
                    {
                        attack = num;
                    }
                    else if (atkElem.ValueKind == JsonValueKind.String && int.TryParse(atkElem.GetString(), out var parsedNum))
                    {
                        attack = parsedNum;
                    }
                }

                if (root.TryGetProperty("defense", out var defElem) || root.TryGetProperty("def", out defElem) || root.TryGetProperty("Defense", out defElem) || root.TryGetProperty("toughness", out defElem))
                {
                    if (defElem.ValueKind == JsonValueKind.Number && defElem.TryGetInt32(out var num))
                    {
                        defense = num;
                    }
                    else if (defElem.ValueKind == JsonValueKind.String && int.TryParse(defElem.GetString(), out var parsedNum))
                    {
                        defense = parsedNum;
                    }
                }

                if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(description))
                {
                    return (
                        string.IsNullOrWhiteSpace(title) ? (fallbackTitle ?? "Nuova Carta") : title.Trim(),
                        description.Trim(),
                        attack,
                        defense
                    );
                }
            }
            catch (JsonException)
            {
                // Fallback con regex se il JSON era parzialmente malformato
            }
        }

        // Fallback tramite Regex per trovare i vari campi
        var titleMatch = Regex.Match(rawOutput, "\"([tT]itle)\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.None, TimeSpan.FromSeconds(1));
        var descMatch = Regex.Match(rawOutput, "\"([dD]escription)\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.None, TimeSpan.FromSeconds(1));
        var atkMatch = Regex.Match(rawOutput, "\"([aA]ttack|[aA]tk|[pP]ower)\"\\s*:\\s*(\\d+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var defMatch = Regex.Match(rawOutput, "\"([dD]efense|[dD]ef|[tT]oughness)\"\\s*:\\s*(\\d+)", RegexOptions.None, TimeSpan.FromSeconds(1));

        var parsedTitle = titleMatch.Success ? titleMatch.Groups[2].Value : (fallbackTitle ?? "Nuova Carta");
        var parsedDesc = descMatch.Success ? descMatch.Groups[2].Value : rawOutput.Trim();
        int? parsedAtk = atkMatch.Success && int.TryParse(atkMatch.Groups[2].Value, out var a) ? a : null;
        int? parsedDef = defMatch.Success && int.TryParse(defMatch.Groups[2].Value, out var d) ? d : null;

        return (parsedTitle.Trim(), parsedDesc.Trim(), parsedAtk, parsedDef);
    }
}
