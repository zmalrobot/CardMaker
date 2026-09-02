using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Pipeline;

namespace CardMaker.Rendering.Tests;

public class ValueBinderTests
{
    private static ValueBinder Build(
        Dictionary<string, CardValue>? values = null,
        params ComputedField[] computed) =>
        new(values ?? new Dictionary<string, CardValue>(StringComparer.Ordinal), computed);

    [Fact]
    public void SostituisceIBindingNelTesto()
    {
        var binder = Build(new Dictionary<string, CardValue>(StringComparer.Ordinal)
        {
            ["atk"] = CardValue.FromNumber(3000),
        });

        Assert.Equal("ATK/3000", binder.Bind("ATK/{{atk}}"));
    }

    [Fact]
    public void UnCampoAssenteDiventaStringaVuota()
    {
        var binder = Build();

        Assert.Equal("ATK/", binder.Bind("ATK/{{mancante}}"));
    }

    [Fact]
    public void IlTestoSenzaBindingRestaIntatto()
    {
        var binder = Build();

        Assert.Equal("Testo { con } graffe", binder.Bind("Testo { con } graffe"));
    }

    [Fact]
    public void UnaGraffaNonChiusaNonRompeIlBinding()
    {
        var binder = Build();

        Assert.Equal("Nome {{incompleto", binder.Bind("Nome {{incompleto"));
    }

    [Fact]
    public void LaTypeLineIgnoraGliArgomentiVuoti()
    {
        var binder = Build(
            new Dictionary<string, CardValue>(StringComparer.Ordinal)
            {
                ["race"] = CardValue.FromText("Dragon"),
                ["summonMethod"] = CardValue.FromText(string.Empty),
                ["abilities"] = CardValue.FromText("Tuner"),
                ["effectFlag"] = CardValue.FromText("Effect"),
            },
            new ComputedField
            {
                Key = "typeLine",
                Expr = new ComputedExpression
                {
                    Op = ComputedOps.Join,
                    Separator = "/",
                    Prefix = "[",
                    Suffix = "]",
                    Args = ["{{race}}", "{{summonMethod}}", "{{abilities}}", "{{effectFlag}}"],
                },
            });

        Assert.Equal("[Dragon/Tuner/Effect]", binder.Get("typeLine")!.AsText());
    }

    [Fact]
    public void CountProduceIlLinkRating()
    {
        var binder = Build(
            new Dictionary<string, CardValue>(StringComparer.Ordinal)
            {
                ["linkArrows"] = CardValue.FromList(["top", "left", "bottomRight"]),
            },
            new ComputedField
            {
                Key = "linkRating",
                Expr = new ComputedExpression
                {
                    Op = ComputedOps.Count,
                    Args = ["{{linkArrows}}"],
                    Format = "LINK-{0}",
                },
            });

        Assert.Equal("LINK-3", binder.Get("linkRating")!.AsText());
    }
}
