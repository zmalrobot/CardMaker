using System.Collections.Concurrent;
using SkiaSharp;

namespace CardMaker.Rendering.Fonts;

public sealed record ResolvedFont(SKTypeface Typeface, string Alias, bool IsFallback);

/// <summary>
/// Fornisce i byte di un font a partire dall'alias di ruolo (es. "card-name", "effect-italic").
/// Implementata dall'host, cosi' il motore non conosce ne' database ne' filesystem.
/// </summary>
public interface IFontSource
{
    byte[]? GetFontBytes(string roleAlias);
}

/// <summary>
/// Risolve gli alias di ruolo in <see cref="SKTypeface"/>, con cache e fallback.
/// Un alias mancante non fa fallire il render: si usa il font di ripiego e lo si segnala,
/// cosi' l'anteprima puo' avvisare l'admin invece di mostrare un errore.
/// </summary>
public sealed class FontRegistry(IFontSource source) : IDisposable
{
    private readonly ConcurrentDictionary<string, SKTypeface?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SKTypeface> _owned = [];
    private readonly Lock _gate = new();
    private bool _disposed;

    /// <summary>Font di ripiego: sempre disponibile, mai proprietario.</summary>
    public static SKTypeface Fallback { get; } =
        SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;

    public ResolvedFont Resolve(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return new ResolvedFont(Fallback, string.Empty, IsFallback: true);
        }

        var typeface = _cache.GetOrAdd(alias, Load);
        return typeface is null
            ? new ResolvedFont(Fallback, alias, IsFallback: true)
            : new ResolvedFont(typeface, alias, IsFallback: false);
    }

    public static SKTypeface? FromBytes(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        using var data = SKData.CreateCopy(bytes);
        return SKTypeface.FromData(data);
    }

    private SKTypeface? Load(string alias)
    {
        var typeface = FromBytes(source.GetFontBytes(alias));
        if (typeface is null)
        {
            return null;
        }

        lock (_gate)
        {
            _owned.Add(typeface);
        }

        return typeface;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_gate)
        {
            foreach (var typeface in _owned)
            {
                typeface.Dispose();
            }

            _owned.Clear();
        }

        _cache.Clear();
    }
}
