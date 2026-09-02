using System.Text.Json.Serialization;

namespace CardMaker.Contracts.Layout;

/// <summary>
/// Condizione come AST tipizzato: nessuna stringa viene mai interpretata o eseguita (ADR-006).
/// </summary>
public sealed record Condition
{
    public required string Op { get; init; }

    public string? Field { get; init; }

    public string? Value { get; init; }

    public IReadOnlyList<string>? Values { get; init; }

    public IReadOnlyList<Condition>? Args { get; init; }

    public static Condition Equal(string field, string value) => new() { Op = ConditionOps.Equal, Field = field, Value = value };

    public static Condition In(string field, params string[] values) =>
        new() { Op = ConditionOps.In, Field = field, Values = values };

    public static Condition And(params Condition[] args) => new() { Op = ConditionOps.And, Args = args };

    public static Condition Or(params Condition[] args) => new() { Op = ConditionOps.Or, Args = args };

    public static Condition Not(Condition arg) => new() { Op = ConditionOps.Not, Args = [arg] };
}

public static class ConditionOps
{
    public const string Equal = "eq";
    public const string NotEqual = "neq";
    public const string GreaterThan = "gt";
    public const string GreaterOrEqual = "gte";
    public const string LessThan = "lt";
    public const string LessOrEqual = "lte";
    public const string In = "in";
    public const string NotIn = "notIn";
    public const string IsEmpty = "isEmpty";
    public const string NotEmpty = "notEmpty";
    public const string IsTrue = "isTrue";
    public const string And = "and";
    public const string Or = "or";
    public const string Not = "not";

    public static readonly string[] All =
    [
        Equal, NotEqual, GreaterThan, GreaterOrEqual, LessThan, LessOrEqual,
        In, NotIn, IsEmpty, NotEmpty, IsTrue, And, Or, Not,
    ];
}

public sealed record ComputedField
{
    public required string Key { get; init; }

    public required ComputedExpression Expr { get; init; }
}

/// <summary>Espressione dichiarativa per i campi derivati (type line, LINK-n).</summary>
public sealed record ComputedExpression
{
    public required string Op { get; init; }

    /// <summary>Argomenti: testo letterale oppure binding <c>{{campo}}</c>.</summary>
    public IReadOnlyList<string> Args { get; init; } = [];

    public string? Separator { get; init; }

    public string? Prefix { get; init; }

    public string? Suffix { get; init; }

    /// <summary>Per <c>count</c>: formato del risultato, es. "LINK-{0}".</summary>
    public string? Format { get; init; }

    [JsonIgnore]
    public bool SkipsEmptyArguments => Op is ComputedOps.Join;
}

public static class ComputedOps
{
    public const string Join = "join";
    public const string Concat = "concat";
    public const string Count = "count";
}
