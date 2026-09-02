namespace CardMaker.Application.Content;

public sealed record ContentSeedResult(bool Created, string GameKey, int CardTypeCount, int TemplateCount, int SymbolSetCount, int OptionListCount, int TraitCount);

/// <summary>
/// Popola il database con il gioco Yu-Gi-Oh! (classico + Rush Duel): tipi di carta, campi,
/// template pubblicati, set di simboli e liste opzioni. Idempotente: se il gioco esiste gia'
/// (per <c>Key</c>) non lo tocca (F3).
/// </summary>
public interface IYuGiOhContentSeeder
{
    Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}
