using System.Diagnostics.CodeAnalysis;

namespace CardMaker.Domain.Common;

/// <summary>
/// Testo tradotto per cultura, persistito come colonna JSON: {"it": "...", "en": "..."}.
/// </summary>
public sealed class LocalizedText
{
    public const string DefaultCulture = "it";

    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public LocalizedText()
    {
    }

    public LocalizedText(IDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var (culture, value) in values)
        {
            _values[culture] = value;
        }
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    public string this[string culture]
    {
        get => Get(culture);
        set => Set(culture, value);
    }

    public static LocalizedText From(string italian, string? english = null)
    {
        var text = new LocalizedText();
        text.Set("it", italian);
        text.Set("en", english ?? italian);
        return text;
    }

    /// <summary>
    /// Restituisce la traduzione richiesta, altrimenti quella di default, altrimenti la prima disponibile.
    /// </summary>
    public string Get(string? culture = null)
    {
        var targetCulture = !string.IsNullOrWhiteSpace(culture)
            ? culture
            : System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (!string.IsNullOrWhiteSpace(targetCulture))
        {
            if (_values.TryGetValue(targetCulture, out var exact))
            {
                return exact;
            }

            var neutral = targetCulture.Split('-')[0];
            if (_values.TryGetValue(neutral, out var neutralMatch))
            {
                return neutralMatch;
            }
        }

        if (_values.TryGetValue(DefaultCulture, out var fallback))
        {
            return fallback;
        }

        return _values.Count > 0 ? _values.Values.First() : string.Empty;
    }

    public void Set(string culture, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        _values[culture] = value ?? string.Empty;
    }

    public bool IsEmpty => _values.Count == 0 || _values.Values.All(string.IsNullOrWhiteSpace);

    [ExcludeFromCodeCoverage]
    public override string ToString() => Get();
}
