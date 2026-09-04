using System.Globalization;
using CardMaker.Contracts.Layout;

namespace CardMaker.Contracts.Layout;

/// <summary>
/// Fase EVALUATE: valuta l'AST delle condizioni.
/// Insieme chiuso di operatori, nessuna stringa interpretata: niente code injection (ADR-006).
/// </summary>
public sealed class ConditionEvaluator(ValueBinder binder)
{
    public bool IsSatisfied(Condition? condition)
    {
        if (condition is null)
        {
            return true;
        }

        return condition.Op switch
        {
            ConditionOps.And => (condition.Args ?? []).All(IsSatisfied),
            ConditionOps.Or => (condition.Args ?? []).Any(IsSatisfied),
            ConditionOps.Not => !(condition.Args ?? []).Any(IsSatisfied),

            ConditionOps.Equal => TextOf(condition).Equals(condition.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            ConditionOps.NotEqual => !TextOf(condition).Equals(condition.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase),

            ConditionOps.GreaterThan => Compare(condition) is { } gt && gt > 0,
            ConditionOps.GreaterOrEqual => Compare(condition) is { } gte && gte >= 0,
            ConditionOps.LessThan => Compare(condition) is { } lt && lt < 0,
            ConditionOps.LessOrEqual => Compare(condition) is { } lte && lte <= 0,

            ConditionOps.In => Matches(condition),
            ConditionOps.NotIn => !Matches(condition),

            ConditionOps.IsEmpty => binder.Get(condition.Field) is not { } empty || empty.IsEmpty,
            ConditionOps.NotEmpty => binder.Get(condition.Field) is { } filled && !filled.IsEmpty,
            ConditionOps.IsTrue => IsTrue(condition),

            // Un operatore sconosciuto nasconde il layer invece di far fallire il render.
            _ => false,
        };
    }

    private string TextOf(Condition condition) => binder.Get(condition.Field)?.AsText() ?? string.Empty;

    private bool IsTrue(Condition condition)
    {
        var value = binder.Get(condition.Field);
        if (value is null)
        {
            return false;
        }

        return value.Flag
            ?? (value.AsNumber() is { } n ? n != 0 : value.AsText().Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private bool Matches(Condition condition)
    {
        var candidates = condition.Values ?? (condition.Value is null ? [] : [condition.Value]);
        var value = binder.Get(condition.Field);
        if (value is null)
        {
            return false;
        }

        var actual = value.Items is { Count: > 0 } ? value.Items : [value.AsText()];
        return actual.Any(a => candidates.Contains(a, StringComparer.OrdinalIgnoreCase));
    }

    private int? Compare(Condition condition)
    {
        var value = binder.Get(condition.Field);
        if (value is null || condition.Value is null)
        {
            return null;
        }

        var left = value.AsNumber();
        if (left is not null
            && double.TryParse(condition.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var right))
        {
            return left.Value.CompareTo(right);
        }

        return string.Compare(value.AsText(), condition.Value, StringComparison.OrdinalIgnoreCase);
    }
}
