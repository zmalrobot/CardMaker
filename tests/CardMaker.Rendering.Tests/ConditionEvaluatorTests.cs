using CardMaker.Contracts.Layout;

namespace CardMaker.Rendering.Tests;

public class ConditionEvaluatorTests
{
    private static ConditionEvaluator Build(Dictionary<string, CardValue> values) =>
        new(new ValueBinder(values, []));

    private static readonly Dictionary<string, CardValue> Sample = new(StringComparer.Ordinal)
    {
        ["rarity"] = CardValue.FromText("ultra"),
        ["level"] = CardValue.FromNumber(8),
        ["abilities"] = CardValue.FromList(["tuner", "spirit"]),
        ["isPendulum"] = CardValue.FromFlag(true),
        ["flavor"] = CardValue.FromText(string.Empty),
    };

    [Fact]
    public void UnaCondizioneAssenteRendeIlLayerVisibile()
    {
        Assert.True(Build(Sample).IsSatisfied(null));
    }

    [Fact]
    public void ConfrontoDiUguaglianzaSenzaDistinzioneDiMaiuscole()
    {
        var evaluator = Build(Sample);

        Assert.True(evaluator.IsSatisfied(Condition.Equal("rarity", "ULTRA")));
        Assert.False(evaluator.IsSatisfied(Condition.Equal("rarity", "secret")));
    }

    [Fact]
    public void ConfrontiNumerici()
    {
        var evaluator = Build(Sample);

        Assert.True(evaluator.IsSatisfied(new Condition { Op = ConditionOps.GreaterOrEqual, Field = "level", Value = "5" }));
        Assert.False(evaluator.IsSatisfied(new Condition { Op = ConditionOps.LessThan, Field = "level", Value = "5" }));
    }

    [Fact]
    public void InFunzionaAncheSuUnaListaDiValori()
    {
        var evaluator = Build(Sample);

        Assert.True(evaluator.IsSatisfied(Condition.In("abilities", "toon", "tuner")));
        Assert.False(evaluator.IsSatisfied(Condition.In("abilities", "toon", "union")));
    }

    [Fact]
    public void VuotoENonVuoto()
    {
        var evaluator = Build(Sample);

        Assert.True(evaluator.IsSatisfied(new Condition { Op = ConditionOps.IsEmpty, Field = "flavor" }));
        Assert.True(evaluator.IsSatisfied(new Condition { Op = ConditionOps.NotEmpty, Field = "rarity" }));
        Assert.True(evaluator.IsSatisfied(new Condition { Op = ConditionOps.IsEmpty, Field = "campoInesistente" }));
    }

    [Fact]
    public void OperatoriLogiciComposti()
    {
        var evaluator = Build(Sample);

        var condition = Condition.And(
            Condition.Equal("rarity", "ultra"),
            Condition.Or(
                Condition.Equal("level", "8"),
                Condition.Equal("level", "12")),
            Condition.Not(Condition.Equal("rarity", "common")));

        Assert.True(evaluator.IsSatisfied(condition));
    }

    [Fact]
    public void UnOperatoreSconosciutoNascondeIlLayerInveceDiFarFallireIlRender()
    {
        var evaluator = Build(Sample);

        Assert.False(evaluator.IsSatisfied(new Condition { Op = "exec", Field = "rarity", Value = "ultra" }));
    }

    [Fact]
    public void IsTrueLeggeIFlagBooleani()
    {
        var evaluator = Build(Sample);

        Assert.True(evaluator.IsSatisfied(new Condition { Op = ConditionOps.IsTrue, Field = "isPendulum" }));
        Assert.False(evaluator.IsSatisfied(new Condition { Op = ConditionOps.IsTrue, Field = "flavor" }));
    }
}
