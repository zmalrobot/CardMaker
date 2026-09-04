using CardMaker.Contracts.Layout;

namespace CardMaker.Application.Cards;

public sealed class CardDerivedValuesService : ICardDerivedValuesService
{
    public void CalculateDerivedValues(
        string? gameKey,
        CardTypeDetailDto? cardType,
        IReadOnlyCollection<string> selectedTraits,
        IDictionary<string, CardValue> cardValues)
    {
        ArgumentNullException.ThrowIfNull(selectedTraits);
        ArgumentNullException.ThrowIfNull(cardValues);

        if (cardType is null)
        {
            return;
        }

        var selectedTraitDtos = cardType.AllowedTraits
            .Where(t => selectedTraits.Contains(t.Key))
            .ToList();

        var traitNames = selectedTraitDtos.Select(t => t.Name).ToList();

        if (string.Equals(gameKey, "yugioh", StringComparison.OrdinalIgnoreCase))
        {
            UpdateYuGiOhValues(cardType, traitNames, cardValues);
        }
        else if (string.Equals(gameKey, "mtg", StringComparison.OrdinalIgnoreCase))
        {
            UpdateMtgValues(selectedTraits, traitNames, cardValues);
        }
        else if (string.Equals(gameKey, "pokemon", StringComparison.OrdinalIgnoreCase))
        {
            UpdatePokemonValues(selectedTraits, traitNames, cardValues);
        }
    }

    private static void UpdateYuGiOhValues(
        CardTypeDetailDto cardType,
        List<string> traitNames,
        IDictionary<string, CardValue> cardValues)
    {
        var raceKey = cardValues.TryGetValue("race", out var rVal) ? rVal.AsText() : "dragon";
        var raceField = cardType.Fields.FirstOrDefault(f => f.Key == "race");
        var raceName = raceField?.Options.FirstOrDefault(o => o.Key == raceKey)?.Label ?? raceKey;
        if (string.IsNullOrWhiteSpace(raceName) || raceName == "dragon")
        {
            raceName = "Drago";
        }
        cardValues["raceName"] = CardValue.FromText(raceName);

        var isNormal = cardType.Key.Contains("normal", StringComparison.OrdinalIgnoreCase);
        var isFusion = cardType.Key.Contains("fusion", StringComparison.OrdinalIgnoreCase);
        var isSynchro = cardType.Key.Contains("synchro", StringComparison.OrdinalIgnoreCase);
        var isXyz = cardType.Key.Contains("xyz", StringComparison.OrdinalIgnoreCase);
        var isLink = cardType.Key.Contains("link", StringComparison.OrdinalIgnoreCase);
        var isRitual = cardType.Key.Contains("ritual", StringComparison.OrdinalIgnoreCase);
        var isPendulum = cardType.Key.Contains("pendulum", StringComparison.OrdinalIgnoreCase);

        var effectFlag = isNormal
            ? (traitNames.Count > 0 ? "Normale" : string.Empty)
            : (isFusion ? "Fusione / Effetto" :
               isSynchro ? "Synchro / Effetto" :
               isXyz ? "Xyz / Effetto" :
               isLink ? "Link / Effetto" :
               isRitual ? "Rituale / Effetto" :
               isPendulum ? "Pendulum / Effetto" : "Effetto");

        cardValues["effectFlag"] = CardValue.FromText(effectFlag);

        var parts = new List<string> { raceName };
        parts.AddRange(traitNames);
        if (!string.IsNullOrEmpty(effectFlag))
        {
            parts.Add(effectFlag);
        }

        cardValues["typeLine"] = CardValue.FromText($"[{string.Join(" / ", parts)}]");
    }

    private static void UpdateMtgValues(
        IReadOnlyCollection<string> selectedTraits,
        List<string> traitNames,
        IDictionary<string, CardValue> cardValues)
    {
        if (!cardValues.TryGetValue("typeLine", out var tv) || string.IsNullOrWhiteSpace(tv.AsText()))
        {
            cardValues["typeLine"] = CardValue.FromText("Creatura — Angelo");
        }

        var current = cardValues["typeLine"].AsText();
        var hasLegendary = selectedTraits.Contains("legendary");

        if (hasLegendary && !current.Contains("Leggendari", StringComparison.OrdinalIgnoreCase))
        {
            if (current.StartsWith("Creatura", StringComparison.OrdinalIgnoreCase))
            {
                current = "Creatura Leggendaria" + current[8..];
            }
            else
            {
                current = "Leggendario " + current;
            }
            cardValues["typeLine"] = CardValue.FromText(current);
        }
        else if (!hasLegendary && current.Contains("Creatura Leggendaria", StringComparison.OrdinalIgnoreCase))
        {
            current = "Creatura" + current["Creatura Leggendaria".Length..];
            cardValues["typeLine"] = CardValue.FromText(current);
        }

        cardValues["supertype"] = CardValue.FromText(string.Join(" ", traitNames));
    }

    private static void UpdatePokemonValues(
        IReadOnlyCollection<string> selectedTraits,
        List<string> traitNames,
        IDictionary<string, CardValue> cardValues)
    {
        var traitsStr = string.Join(" ", traitNames);
        cardValues["traitBadge"] = CardValue.FromText(traitsStr);
        if (selectedTraits.Count > 0)
        {
            cardValues["stageTraitSuffix"] = CardValue.FromText(" · " + traitsStr.ToUpperInvariant());
        }
        else
        {
            cardValues["stageTraitSuffix"] = CardValue.FromText(string.Empty);
        }
    }
}
