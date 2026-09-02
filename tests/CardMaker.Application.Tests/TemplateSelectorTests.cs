using System.Text.Json;
using CardMaker.Application.Content;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Templates;

namespace CardMaker.Application.Tests;

public class TemplateSelectorTests
{
    private static Template MakeTemplate(string key, int sortOrder, Condition? rule, bool isDefault = false) => new()
    {
        Key = key,
        SortOrder = sortOrder,
        IsDefault = isDefault,
        SelectionRuleJson = rule is null ? null : JsonSerializer.Serialize(rule, LayoutSerializer.Options),
    };

    [Fact]
    public void SceglieIlTemplateLaCuiRegolaESoddisfatta()
    {
        var selector = new TemplateSelector();
        var templates = new[]
        {
            MakeTemplate("left", 0, Condition.Equal("maximumSlice", "left")),
            MakeTemplate("center", 1, Condition.Equal("maximumSlice", "center")),
            MakeTemplate("right", 2, Condition.Equal("maximumSlice", "right")),
        };
        var values = new Dictionary<string, CardValue> { ["maximumSlice"] = CardValue.FromText("center") };

        var selected = selector.SelectTemplate(templates, values);

        Assert.Equal("center", selected?.Key);
    }

    [Fact]
    public void SenzaRegoleSoddisfatteUsaIlTemplatePredefinito()
    {
        var selector = new TemplateSelector();
        var templates = new[]
        {
            MakeTemplate("left", 0, Condition.Equal("maximumSlice", "left")),
            MakeTemplate("default", 1, null, isDefault: true),
        };

        var selected = selector.SelectTemplate(templates, new Dictionary<string, CardValue>());

        Assert.Equal("default", selected?.Key);
    }

    [Fact]
    public void SenzaPredefinitoUsaIlPrimoInOrdine()
    {
        var selector = new TemplateSelector();
        var templates = new[]
        {
            MakeTemplate("b", 1, null),
            MakeTemplate("a", 0, null),
        };

        var selected = selector.SelectTemplate(templates, new Dictionary<string, CardValue>());

        Assert.Equal("a", selected?.Key);
    }
}
