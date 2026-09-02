namespace CardMaker.Application.Content;

/// <summary>
/// Popola il database con il gioco Pokémon TCG: tipi di carta, campi,
/// template pubblicati, set di simboli (energie e rarità) e liste opzioni.
/// </summary>
public interface IPokemonContentSeeder
{
    Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}

