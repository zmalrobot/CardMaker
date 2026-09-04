using CardMaker.Application.Content;
using CardMaker.Infrastructure.Persistence;

namespace CardMaker.Infrastructure.Content;

/// <summary>Orchestrazione EF Core del seed Magic: The Gathering (MTG).</summary>
public sealed class MtgContentSeeder(CardMakerDbContext db) : IMtgContentSeeder
{
    public Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default) =>
        ContentGraphSeeder.SeedGraphAsync(db, MtgSeedData.Build(), MtgSeedData.GameKey, cancellationToken);
}
