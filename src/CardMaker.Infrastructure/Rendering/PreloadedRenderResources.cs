using CardMaker.Contracts.Layout;
using CardMaker.Rendering;
using CardMaker.Rendering.Fonts;
using SkiaSharp;

namespace CardMaker.Infrastructure.Rendering;

/// <summary>
/// Risorse caricate in memoria prima del render. Il motore le consuma in modo sincrono,
/// quindi l'accesso al database avviene tutto prima, una volta sola.
/// </summary>
public sealed class PreloadedRenderResources : IRenderResources, IDisposable
{
    private readonly Dictionary<Guid, SKImage> _byId = [];
    private readonly Dictionary<string, SKImage> _byKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SKImage> _symbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SKTypeface> _fonts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SKImage> _owned = [];
    private bool _disposed;

    public void AddImage(Guid assetId, byte[] bytes)
    {
        var image = Decode(bytes);
        if (image is not null)
        {
            AddImage(assetId, image, owned: true);
        }
    }

    /// <summary>
    /// Registra un'immagine gia' decodificata. Se <paramref name="owned"/> e' falso (proviene da
    /// <see cref="IDecodedImageCache"/>), <see cref="Dispose"/> non la smaltisce: appartiene alla
    /// cache condivisa fra le richieste, non a questa singola richiesta di render.
    /// </summary>
    public void AddImage(Guid assetId, SKImage image, bool owned)
    {
        _byId[assetId] = image;
        if (owned)
        {
            _owned.Add(image);
        }
    }

    public void AddImageKey(string key, byte[] bytes)
    {
        var image = Decode(bytes);
        if (image is not null)
        {
            AddImageKey(key, image, owned: true);
        }
    }

    public void AddImageKey(string key, SKImage image, bool owned)
    {
        _byKey[key] = image;
        if (owned)
        {
            _owned.Add(image);
        }
    }

    public void AddSymbol(string symbolSetKey, string symbolKey, byte[] bytes)
    {
        var image = Decode(bytes);
        if (image is not null)
        {
            AddSymbol(symbolSetKey, symbolKey, image, owned: true);
        }
    }

    public void AddSymbol(string symbolSetKey, string symbolKey, SKImage image, bool owned)
    {
        _symbols[SymbolKey(symbolSetKey, symbolKey)] = image;
        if (owned)
        {
            _owned.Add(image);
        }
    }

    public void AddFont(string roleAlias, byte[] bytes)
    {
        var typeface = FontRegistry.FromBytes(bytes);
        if (typeface is not null)
        {
            _fonts[roleAlias] = typeface;
        }
    }

    public SKImage? GetImage(Guid assetId) => _byId.GetValueOrDefault(assetId);

    public SKImage? GetImageByKey(string assetKey) => _byKey.GetValueOrDefault(assetKey);

    public SKImage? GetSymbol(string symbolSetKey, string symbolKey) =>
        _symbols.GetValueOrDefault(SymbolKey(symbolSetKey, symbolKey));

    public SKTypeface ResolveFont(string? roleAlias, out bool isFallback)
    {
        if (!string.IsNullOrWhiteSpace(roleAlias) && _fonts.TryGetValue(roleAlias, out var typeface))
        {
            isFallback = false;
            return typeface;
        }

        isFallback = true;
        return FontRegistry.Fallback;
    }

    private static string SymbolKey(string setKey, string symbolKey) => setKey + "/" + symbolKey;

    private static SKImage? Decode(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        using var data = SKData.CreateCopy(bytes);
        return SKImage.FromEncodedData(data);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Le immagini prese in prestito dalla cache condivisa (owned: false) non vanno smaltite qui.
        foreach (var image in _owned)
        {
            image.Dispose();
        }

        foreach (var typeface in _fonts.Values)
        {
            typeface.Dispose();
        }

        _byId.Clear();
        _byKey.Clear();
        _symbols.Clear();
        _fonts.Clear();
        _owned.Clear();
    }
}

/// <summary>Estrae dal layout i riferimenti da caricare: si legge dal database solo cio' che serve.</summary>
internal static class LayoutReferences
{
    public static (HashSet<Guid> AssetIds, HashSet<string> AssetKeys, HashSet<string> FontAliases,
        List<(string Set, string Key)> Symbols) Collect(
        CardLayout layout, IReadOnlyDictionary<string, CardValue> values)
    {
        var assetIds = new HashSet<Guid>();
        var assetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fontAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var symbols = new List<(string, string)>();

        foreach (var style in layout.TextStyles.Values)
        {
            if (!string.IsNullOrWhiteSpace(style.Font))
            {
                fontAliases.Add(style.Font);
            }
        }

        foreach (var layer in layout.EnumerateLayers())
        {
            switch (layer)
            {
                case StaticImageLayer image:
                    if (image.AssetId is { } id)
                    {
                        assetIds.Add(id);
                    }

                    if (!string.IsNullOrWhiteSpace(image.AssetKey))
                    {
                        assetKeys.Add(image.AssetKey);
                    }

                    break;

                case ImageSlotLayer slot:
                    if (slot.PlaceholderAssetId is { } placeholder)
                    {
                        assetIds.Add(placeholder);
                    }

                    if (values.TryGetValue(slot.FieldKey, out var value)
                        && Guid.TryParse(value.AsText(), out var artworkId))
                    {
                        assetIds.Add(artworkId);
                    }

                    break;

                case SymbolSlotLayer symbol:
                    var key = symbol.FieldKey is { } field
                        ? values.GetValueOrDefault(field)?.AsText()
                        : symbol.SymbolKey;

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        symbols.Add((symbol.SymbolSetKey, key));
                    }

                    break;

                case TextLayer text:
                    var alias = text.StyleOverrides?.Font;
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        fontAliases.Add(alias);
                    }

                    break;
            }
        }

        return (assetIds, assetKeys, fontAliases, symbols);
    }
}
