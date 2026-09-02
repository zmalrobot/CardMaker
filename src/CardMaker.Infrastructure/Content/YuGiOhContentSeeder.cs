using CardMaker.Application.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Content;

/// <summary>Orchestrazione EF Core del seed: <see cref="YuGiOhSeedData"/> resta puro dato/testabile senza database.</summary>
public sealed class YuGiOhContentSeeder(CardMakerDbContext db) : IYuGiOhContentSeeder
{
    public async Task<ContentSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var graph = YuGiOhSeedData.Build();
        var existing = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == YuGiOhSeedData.GameKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var game = await db.Games.FindAsync([existing.Id], cancellationToken).ConfigureAwait(false);
            if (game is not null)
            {
                game.Name = graph.Game.Name;
                game.Description = graph.Game.Description;
            }

            var dbSymbolSets = await db.SymbolSets
                .Include(s => s.Symbols)
                .Where(s => s.GameId == existing.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var set in dbSymbolSets)
            {
                var seedSet = graph.SymbolSets.FirstOrDefault(s => s.Key == set.Key);
                if (seedSet is not null)
                {
                    set.Name = seedSet.Name;
                    foreach (var sym in set.Symbols)
                    {
                        var seedSym = seedSet.Symbols.FirstOrDefault(s => s.Key == sym.Key);
                        if (seedSym is not null)
                        {
                            sym.Name = seedSym.Name;
                        }
                    }
                }
            }

            var dbOptionLists = await db.OptionLists
                .Include(o => o.Items)
                .Where(o => o.GameId == existing.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var list in dbOptionLists)
            {
                var seedList = graph.OptionLists.FirstOrDefault(o => o.Key == list.Key);
                if (seedList is not null)
                {
                    list.Name = seedList.Name;
                    foreach (var item in list.Items)
                    {
                        var seedItem = seedList.Items.FirstOrDefault(i => i.Key == item.Key);
                        if (seedItem is not null)
                        {
                            item.Label = seedItem.Label;
                        }
                    }
                }
            }

            var dbTraits = await db.Traits
                .Where(t => t.GameId == existing.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var trait in dbTraits)
            {
                var seedTrait = graph.Traits.FirstOrDefault(t => t.Key == trait.Key);
                if (seedTrait is not null)
                {
                    trait.Name = seedTrait.Name;
                }
            }

            var dbCardTypes = await db.CardTypes
                .Include(c => c.Fields)
                .Include(c => c.Templates)
                .Where(c => c.GameId == existing.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var ct in dbCardTypes)
            {
                var seedCt = graph.CardTypes.FirstOrDefault(c => c.Key == ct.Key);
                if (seedCt is not null)
                {
                    ct.Name = seedCt.Name;
                    foreach (var field in ct.Fields)
                    {
                        var seedField = seedCt.Fields.FirstOrDefault(f => f.Key == field.Key);
                        if (seedField is not null)
                        {
                            field.Label = seedField.Label;
                        }
                    }
                    foreach (var tpl in ct.Templates)
                    {
                        var seedTpl = seedCt.Templates.FirstOrDefault(t => t.Key == tpl.Key);
                        if (seedTpl is not null)
                        {
                            tpl.Name = seedTpl.Name;
                        }
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var cardTypeCount = dbCardTypes.Count;
            var templateCount = dbCardTypes.Sum(c => c.Templates.Count);
            var symbolSetCount = dbSymbolSets.Count;
            var optionListCount = dbOptionLists.Count;
            var traitCount = dbTraits.Count;

            return new ContentSeedResult(false, existing.Key, cardTypeCount, templateCount, symbolSetCount, optionListCount, traitCount);
        }

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
