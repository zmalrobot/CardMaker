namespace CardMaker.Application.Abstractions;

public sealed record NormalizedImage(byte[] Content, int Width, int Height, string ContentType);

public sealed record FontInfo(string FamilyName, string StyleName, int Weight, bool IsItalic);

/// <summary>
/// Ricodifica le immagini in ingresso: elimina metadati ed eventuali payload nascosti nel file originale.
/// </summary>
public interface IImageProcessor
{
    NormalizedImage? Normalize(byte[] source);
}

public interface IFontProcessor
{
    FontInfo? Probe(byte[] source);
}
