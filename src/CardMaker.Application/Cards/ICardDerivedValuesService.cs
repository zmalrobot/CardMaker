using CardMaker.Contracts.Layout;

namespace CardMaker.Application.Cards;

public interface ICardDerivedValuesService
{
    void CalculateDerivedValues(
        string? gameKey,
        CardTypeDetailDto? cardType,
        IReadOnlyCollection<string> selectedTraits,
        IDictionary<string, CardValue> cardValues);
}

