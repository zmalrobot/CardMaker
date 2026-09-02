using System.Text.RegularExpressions;

namespace CardMaker.Rendering.Text;

public enum RichRunKind
{
    Text = 0,
    Bold = 1,
    Italic = 2,
    BoldItalic = 3,
    Symbol = 4,
    SectionLabel = 5,
}

public sealed record RichRun
{
    public RichRunKind Kind { get; init; }

    /// <summary>Testo del run; vuoto per i run di tipo <see cref="RichRunKind.Symbol"/>.</summary>
    public string Text { get; init; } = string.Empty;

    public string? SymbolSetKey { get; init; }

    public string? SymbolKey { get; init; }
}

public sealed record RichParagraph(IReadOnlyList<RichRun> Runs, bool IsBullet);

/// <summary>
/// Parser di un piccolo markup: **grassetto**, *corsivo*, simboli inline "{sym:set.chiave}"
/// o "{sym:chiave}" (usa il set di default del layer), righe "[LABEL] testo" per le sezioni
/// etichettate (Rush Duel), righe "- testo" per i punti elenco. Nessuna valutazione arbitraria:
/// solo pattern matching su token noti (coerente con ADR-006).
/// </summary>
public static partial class RichTextParser
{
    [GeneratedRegex(@"\*\*(?<bold>.+?)\*\*|\*(?<italic>.+?)\*|\{sym:(?<sym>[A-Za-z0-9_\-]+(?:\.[A-Za-z0-9_\-]+)?)\}")]
    private static partial Regex InlineTokenPattern();

    [GeneratedRegex(@"^\[(?<label>[^\]]+)\]\s*")]
    private static partial Regex SectionLabelPattern();

    public static IReadOnlyList<RichParagraph> Parse(string? source, string? defaultSymbolSetKey)
    {
        if (string.IsNullOrEmpty(source))
        {
            return [];
        }

        var paragraphs = new List<RichParagraph>();
        foreach (var rawLine in source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine;
            var isBullet = false;
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                isBullet = true;
                line = line[2..];
            }

            var runs = new List<RichRun>();

            var labelMatch = SectionLabelPattern().Match(line);
            if (labelMatch.Success)
            {
                runs.Add(new RichRun { Kind = RichRunKind.SectionLabel, Text = $"[{labelMatch.Groups["label"].Value}]" });
                line = line[labelMatch.Length..];
            }

            runs.AddRange(ParseInline(line, defaultSymbolSetKey));
            paragraphs.Add(new RichParagraph(runs, isBullet));
        }

        return paragraphs;
    }

    private static IEnumerable<RichRun> ParseInline(string line, string? defaultSymbolSetKey)
    {
        var lastIndex = 0;
        foreach (var match in InlineTokenPattern().Matches(line).Cast<Match>())
        {
            if (match.Index > lastIndex)
            {
                yield return new RichRun { Kind = RichRunKind.Text, Text = line[lastIndex..match.Index] };
            }

            if (match.Groups["bold"].Success)
            {
                yield return new RichRun { Kind = RichRunKind.Bold, Text = match.Groups["bold"].Value };
            }
            else if (match.Groups["italic"].Success)
            {
                yield return new RichRun { Kind = RichRunKind.Italic, Text = match.Groups["italic"].Value };
            }
            else if (match.Groups["sym"].Success)
            {
                var token = match.Groups["sym"].Value;
                var dot = token.IndexOf('.');
                yield return dot < 0
                    ? new RichRun { Kind = RichRunKind.Symbol, SymbolSetKey = defaultSymbolSetKey, SymbolKey = token }
                    : new RichRun { Kind = RichRunKind.Symbol, SymbolSetKey = token[..dot], SymbolKey = token[(dot + 1)..] };
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < line.Length)
        {
            yield return new RichRun { Kind = RichRunKind.Text, Text = line[lastIndex..] };
        }
    }
}
