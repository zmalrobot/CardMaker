using System.Globalization;
using System.Text;
using CardMaker.Contracts.Layout;

namespace CardMaker.Rendering.Pipeline;

/// <summary>
/// Fase BIND: risolve i binding <c>{{campo}}</c> e calcola i campi derivati (type line, LINK-n).
/// </summary>
public sealed class ValueBinder
{
    private readonly Dictionary<string, CardValue> _values;

    public ValueBinder(IReadOnlyDictionary<string, CardValue> values, IReadOnlyList<ComputedField> computed)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(computed);

        _values = new Dictionary<string, CardValue>(values, StringComparer.Ordinal);

        foreach (var field in computed)
        {
            _values[field.Key] = CardValue.FromText(Evaluate(field.Expr));
        }
    }

    public IReadOnlyDictionary<string, CardValue> Values => _values;

    public CardValue? Get(string? key) =>
        string.IsNullOrEmpty(key) ? null : _values.GetValueOrDefault(key);

    /// <summary>Sostituisce i <c>{{campo}}</c> presenti nel testo; il resto resta letterale.</summary>
    public string Bind(string? source)
    {
        if (string.IsNullOrEmpty(source) || !source.Contains("{{", StringComparison.Ordinal))
        {
            return source ?? string.Empty;
        }

        var result = new StringBuilder(source.Length);
        var index = 0;

        while (index < source.Length)
        {
            var open = source.IndexOf("{{", index, StringComparison.Ordinal);
            if (open < 0)
            {
                result.Append(source, index, source.Length - index);
                break;
            }

            var close = source.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                result.Append(source, index, source.Length - index);
                break;
            }

            result.Append(source, index, open - index);

            var key = source[(open + 2)..close].Trim();
            result.Append(Get(key)?.AsText() ?? string.Empty);

            index = close + 2;
        }

        return result.ToString();
    }

    private string Evaluate(ComputedExpression expr)
    {
        var parts = expr.Args.Select(Bind).ToList();

        return expr.Op switch
        {
            ComputedOps.Join => Wrap(expr, string.Join(
                expr.Separator ?? "/",
                parts.Where(p => !string.IsNullOrWhiteSpace(p)))),

            ComputedOps.Concat => Wrap(expr, string.Concat(parts)),

            ComputedOps.Count => FormatCount(expr, parts),

            _ => string.Empty,
        };
    }

    private static string Wrap(ComputedExpression expr, string body) =>
        body.Length == 0 ? string.Empty : (expr.Prefix ?? string.Empty) + body + (expr.Suffix ?? string.Empty);

    private string FormatCount(ComputedExpression expr, List<string> parts)
    {
        var count = 0;
        foreach (var arg in expr.Args)
        {
            var key = ExtractSingleBinding(arg);
            if (key is not null && Get(key) is { } value)
            {
                count += value.Items?.Count ?? (value.IsEmpty ? 0 : 1);
            }
        }

        if (count == 0)
        {
            count = parts.Count(p => !string.IsNullOrWhiteSpace(p));
        }

        var text = count.ToString(CultureInfo.InvariantCulture);
        return expr.Format is null
            ? Wrap(expr, text)
            : expr.Format.Replace("{0}", text, StringComparison.Ordinal);
    }

    private static string? ExtractSingleBinding(string arg)
    {
        var trimmed = arg.Trim();
        return trimmed.StartsWith("{{", StringComparison.Ordinal) && trimmed.EndsWith("}}", StringComparison.Ordinal)
            ? trimmed[2..^2].Trim()
            : null;
    }
}
