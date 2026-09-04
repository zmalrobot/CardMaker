using System.Text.Json;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Templates;

using Microsoft.Extensions.Logging;

namespace CardMaker.Application.Content;

/// <summary>
/// Sceglie la <see cref="Template"/> giusta in base ai valori inseriti dall'utente, applicando
/// <see cref="Template.SelectionRuleJson"/>. Stesso AST condizionale e stesso valutatore usati per
/// <c>VisibleWhen</c> nel motore di rendering: una regola si scrive e si legge allo stesso modo
/// ovunque compaia (ADR-024).
/// </summary>
public interface ITemplateSelector
{
    /// <summary>
    /// Restituisce il primo template (in ordine di <see cref="Template.SortOrder"/>) la cui regola
    /// e' soddisfatta dai valori forniti; se nessuna regola scatta, il template predefinito
    /// (<see cref="Template.IsDefault"/>) o, in mancanza, il primo disponibile.
    /// </summary>
    Template? SelectTemplate(IEnumerable<Template> templates, IReadOnlyDictionary<string, CardValue> values);
}

public sealed class TemplateSelector(ILogger<TemplateSelector>? logger = null) : ITemplateSelector
{
    public Template? SelectTemplate(IEnumerable<Template> templates, IReadOnlyDictionary<string, CardValue> values)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(values);

        var ordered = templates.OrderBy(t => t.SortOrder).ToList();
        var binder = new ValueBinder(values, []);
        var evaluator = new ConditionEvaluator(binder);

        foreach (var template in ordered)
        {
            if (string.IsNullOrWhiteSpace(template.SelectionRuleJson))
            {
                continue;
            }

            Condition? condition;
            try
            {
                condition = JsonSerializer.Deserialize<Condition>(template.SelectionRuleJson, LayoutSerializer.Options);
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Regola di selezione non valida per il template {TemplateId} ({TemplateKey}).", template.Id, template.Key);
                continue;
            }

            if (condition is not null && evaluator.IsSatisfied(condition))
            {
                return template;
            }
        }

        return ordered.FirstOrDefault(t => t.IsDefault) ?? ordered.FirstOrDefault();
    }
}
