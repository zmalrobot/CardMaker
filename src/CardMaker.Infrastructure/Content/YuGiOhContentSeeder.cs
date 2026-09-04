using CardMaker.Application.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Content;

/// <summary>Orchestrazione EF Core del seed: <see cref="YuGiOhSeedData"/> resta puro dato/testabile senza database.</summary>
public sealed class YuGiOhContentSeeder(CardMakerDbContext db) : IYuGiOhContentSeeder
{
    public Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default) =>
        ContentGraphSeeder.SeedGraphAsync(db, YuGiOhSeedData.Build(), YuGiOhSeedData.GameKey, cancellationToken);
}
