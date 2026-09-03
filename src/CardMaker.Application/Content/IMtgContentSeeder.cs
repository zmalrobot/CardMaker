namespace CardMaker.Application.Content;

/// <summary>
/// Popola il database con il gioco Magic: The Gathering: tipi di carta, campi,
/// template pubblicati, set di simboli (mana e rarità) e liste opzioni.
/// </summary>
public interface IMtgContentSeeder
{
    Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}
