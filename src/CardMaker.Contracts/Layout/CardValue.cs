namespace CardMaker.Contracts.Layout;

/// <summary>
/// Valore di un campo compilato dall'utente. Volutamente chiuso: niente <c>object</c>, cosi'
/// binding e condizioni non devono mai indovinare il tipo a runtime.
/// </summary>
public sealed record CardValue
{
    public string? Text { get; init; }

    public double? Number { get; init; }

    public bool? Flag { get; init; }

    public IReadOnlyList<string>? Items { get; init; }

    public static CardValue FromText(string? value) => new() { Text = value };

    public static CardValue FromNumber(double value) => new() { Number = value };

    public static CardValue FromFlag(bool value) => new() { Flag = value };

    public static CardValue FromList(IEnumerable<string> values) => new() { Items = [.. values] };

    public bool IsEmpty => Text is null or "" && Number is null && Flag is null && (Items is null || Items.Count == 0);

    public string AsText() => Text
        ?? Number?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
        ?? (Flag is { } f ? (f ? "true" : "false") : null)
        ?? (Items is null ? string.Empty : string.Join(", ", Items));

    public double? AsNumber()
    {
        if (Number is { } n)
        {
            return n;
        }

        return double.TryParse(Text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public IReadOnlyList<string> AsList() => Items ?? (string.IsNullOrEmpty(Text) ? [] : [Text]);
}
