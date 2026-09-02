using CardMaker.Application.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Content;

/// <summary>Orchestrazione EF Core del seed: <see cref="YuGiOhSeedData"/> resta puro dato/testabile senza database.</summary>
public sealed class YuGiOhContentSeeder(CardMakerDbContext db) : IYuGiOhContentSeeder
{
    public async Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == YuGiOhSeedData.GameKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var cardTypeCount = await db.CardTypes.CountAsync(c => c.GameId == existing.Id, cancellationToken).ConfigureAwait(false);
            var templateCount = await db.Templates.CountAsync(t => t.CardType.GameId == existing.Id, cancellationToken).ConfigureAwait(false);
            var symbolSetCount = await db.SymbolSets.CountAsync(s => s.GameId == existing.Id, cancellationToken).ConfigureAwait(false);
            var optionListCount = await db.OptionLists.CountAsync(o => o.GameId == existing.Id, cancellationToken).ConfigureAwait(false);
            var traitCount = await db.Traits.CountAsync(t => t.GameId == existing.Id, cancellationToken).ConfigureAwait(false);

            return new ContentSeedResult(false, existing.Key, cardTypeCount, templateCount, symbolSetCount, optionListCount, traitCount);
        }

        var graph = YuGiOhSeedData.Build();

        db.Games.Add(graph.Game);
        db.SymbolSets.AddRange(graph.SymbolSets);
        db.OptionLists.AddRange(graph.OptionLists);
        db.Traits.AddRange(graph.Traits);
        db.CardTypes.AddRange(graph.CardTypes);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var templateCountCreated = graph.CardTypes.Sum(c => c.Templates.Count);

        return new ContentSeedResult(
            true,
            graph.Game.Key,
            graph.CardTypes.Count,
            templateCountCreated,
            graph.SymbolSets.Count,
            graph.OptionLists.Count,
            graph.Traits.Count);
    }
}
