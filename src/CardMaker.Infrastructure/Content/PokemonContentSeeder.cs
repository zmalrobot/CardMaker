using CardMaker.Application.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Content;

/// <summary>Orchestrazione EF Core del seed Pokémon TCG.</summary>
public sealed class PokemonContentSeeder(CardMakerDbContext db) : IPokemonContentSeeder
{
    public Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default) =>
        ContentGraphSeeder.SeedGraphAsync(db, PokemonSeedData.Build(), PokemonSeedData.GameKey, cancellationToken);
}

