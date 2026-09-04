using CardMaker.Application.Cards;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Templates;

namespace CardMaker.Application.Tests.Cards;

public sealed class CardDerivedValuesServiceTests
{
    private readonly CardDerivedValuesService _service = new();

    [Fact]
    public void CalculateDerivedValuesYuGiOhNormalMonsterFormatsCorrectly()
    {
        var cardType = new CardTypeDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "yugioh-normal-monster",
            "Normal Monster",
            [
                new FieldDefinitionDto(
                    Guid.NewGuid(), "race", "Race", "", FieldKind.Enum, false, null, null, 0, null,
                    [new OptionItemDto("spellcaster", "Incantatore")], [])
            ],
            [new TraitOptionDto(Guid.NewGuid(), "tuner", "Tuner", "Types")],
            []);

        var selectedTraits = new List<string> { "tuner" };
        var cardValues = new Dictionary<string, CardValue>
        {
            ["race"] = CardValue.FromText("spellcaster")
        };

        _service.CalculateDerivedValues("yugioh", cardType, selectedTraits, cardValues);

        Assert.Equal("Incantatore", cardValues["raceName"].AsText());
        Assert.Equal("Normale", cardValues["effectFlag"].AsText());
        Assert.Equal("[Incantatore / Tuner / Normale]", cardValues["typeLine"].AsText());
    }

    [Fact]
    public void CalculateDerivedValuesYuGiOhFusionMonsterFormatsCorrectly()
    {
        var cardType = new CardTypeDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "yugioh-fusion-monster",
            "Fusion Monster",
            [],
            [],
            []);

        var selectedTraits = new List<string>();
        var cardValues = new Dictionary<string, CardValue>();

        _service.CalculateDerivedValues("yugioh", cardType, selectedTraits, cardValues);

        Assert.Equal("Drago", cardValues["raceName"].AsText());
        Assert.Equal("Fusione / Effetto", cardValues["effectFlag"].AsText());
        Assert.Equal("[Drago / Fusione / Effetto]", cardValues["typeLine"].AsText());
    }

    [Fact]
    public void CalculateDerivedValuesMtgAddsLegendaryPrefix()
    {
        var cardType = new CardTypeDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mtg-creature",
            "Creature",
            [],
            [new TraitOptionDto(Guid.NewGuid(), "legendary", "Leggendario", "Supertype")],
            []);

        var selectedTraits = new List<string> { "legendary" };
        var cardValues = new Dictionary<string, CardValue>
        {
            ["typeLine"] = CardValue.FromText("Creatura — Angelo")
        };

        _service.CalculateDerivedValues("mtg", cardType, selectedTraits, cardValues);

        Assert.Equal("Creatura Leggendaria — Angelo", cardValues["typeLine"].AsText());
        Assert.Equal("Leggendario", cardValues["supertype"].AsText());
    }

    [Fact]
    public void CalculateDerivedValuesPokemonFormatsStageTraitSuffix()
    {
        var cardType = new CardTypeDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "pokemon-stage1",
            "Stage 1",
            [],
            [new TraitOptionDto(Guid.NewGuid(), "rapid_strike", "Colpo Rapido", "Battle Style")],
            []);

        var selectedTraits = new List<string> { "rapid_strike" };
        var cardValues = new Dictionary<string, CardValue>();

        _service.CalculateDerivedValues("pokemon", cardType, selectedTraits, cardValues);

        Assert.Equal("Colpo Rapido", cardValues["traitBadge"].AsText());
        Assert.Equal(" · COLPO RAPIDO", cardValues["stageTraitSuffix"].AsText());
    }
}

