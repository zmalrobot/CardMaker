using CardMaker.Domain.Cards;
using CardMaker.Domain.Games;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;

namespace CardMaker.Infrastructure.Content;

/// <summary>
/// Contenitore in-memory del grafo di entità di un gioco per il popolamento iniziale o l'aggiornamento.
/// </summary>
public sealed record SeedGraph(
    Game Game,
    IReadOnlyList<CardType> CardTypes,
    IReadOnlyList<Trait> Traits,
    IReadOnlyList<OptionList> OptionLists,
    IReadOnlyList<SymbolSet> SymbolSets);
