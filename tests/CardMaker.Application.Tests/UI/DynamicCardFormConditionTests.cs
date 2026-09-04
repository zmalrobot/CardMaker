using CardMaker.Contracts.Layout;
using CardMaker.Rendering;
using System.Collections.Concurrent;
using System.Text.Json;
using Xunit;

namespace CardMaker.Application.Tests.UI;

public sealed class DynamicCardFormConditionTests
{
    private static readonly ConcurrentDictionary<string, Condition?> ConditionCache = new();

    private static bool EvaluateVisibility(string? visibleWhenJson, IReadOnlyDictionary<string, CardValue> values)
    {
        if (string.IsNullOrWhiteSpace(visibleWhenJson))
        {
            return true;
        }

        var condition = ConditionCache.GetOrAdd(visibleWhenJson, static json =>
        {
            try
            {
                return JsonSerializer.Deserialize<Condition>(json, LayoutSerializer.Options);
            }
            catch
            {
                return null;
            }
        });

        if (condition is null)
        {
            return true;
        }

        var binder = new ValueBinder(values, []);
        var evaluator = new ConditionEvaluator(binder);
        return evaluator.IsSatisfied(condition);
    }

    [Fact]
    public void TEST_UNIT_020_ConditionCacheEvaluatesVisibilityAccurately()
    {
        // Condition: visible when 'type' equals 'Spell'
        var condition = Condition.Equal("type", "Spell");
        var json = JsonSerializer.Serialize(condition, LayoutSerializer.Options);

        var valuesMatching = new Dictionary<string, CardValue> { ["type"] = CardValue.FromText("Spell") };
        var valuesNonMatching = new Dictionary<string, CardValue> { ["type"] = CardValue.FromText("Trap") };

        // Act - SER-PERF-003: cached condition evaluation
        var isVisible1 = EvaluateVisibility(json, valuesMatching);
        var isVisible2 = EvaluateVisibility(json, valuesNonMatching);

        // Assert
        Assert.True(isVisible1);
        Assert.False(isVisible2);
        Assert.True(ConditionCache.ContainsKey(json));
    }

    [Fact]
    public void TEST_UNIT_021_InvalidConditionJsonFallsBackToVisible()
    {
        const string badJson = "{ invalid-json }";

        var isVisible = EvaluateVisibility(badJson, new Dictionary<string, CardValue>());

        Assert.True(isVisible);
    }
}
