using System.Text.Json;
using CardMaker.Domain.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CardMaker.Infrastructure.Persistence;

/// <summary>
/// Persiste <see cref="LocalizedText"/> come colonna JSON: {"it":"...","en":"..."}.
/// </summary>
public sealed class LocalizedTextConverter : ValueConverter<LocalizedText, string>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public LocalizedTextConverter()
        : base(v => Serialize(v), v => Deserialize(v))
    {
    }

    private static string Serialize(LocalizedText value) =>
        JsonSerializer.Serialize(value.Values, Options);

    private static LocalizedText Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new LocalizedText();
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options);
        return values is null ? new LocalizedText() : new LocalizedText(values);
    }
}

public sealed class LocalizedTextComparer : ValueComparer<LocalizedText>
{
    public LocalizedTextComparer()
        : base(
            (a, b) => Equal(a, b),
            v => v.Values.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
            v => new LocalizedText(v.Values.ToDictionary(kv => kv.Key, kv => kv.Value)))
    {
    }

    private static bool Equal(LocalizedText? a, LocalizedText? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Values.Count != b.Values.Count)
        {
            return false;
        }

        return a.Values.All(kv => b.Values.TryGetValue(kv.Key, out var other) && other == kv.Value);
    }
}

/// <summary>
/// SQLite non sa ordinare ne' confrontare i DateTimeOffset: vanno persistiti come tick UTC.
/// Tutti i timestamp dell'applicazione sono in UTC, quindi l'offset non porta informazione.
/// </summary>
public sealed class DateTimeOffsetToTicksConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToTicksConverter()
        : base(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero))
    {
    }
}
