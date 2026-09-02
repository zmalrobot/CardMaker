namespace CardMaker.Contracts.Geometry;

/// <summary>
/// Rettangolo in coordinate normalizzate 0..1 rispetto al trim della carta (ADR-008).
/// Rende il layout indipendente dal DPI: anteprima ed export usano gli stessi numeri.
/// </summary>
public readonly record struct NormalizedRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public static NormalizedRect FromPixels(float x, float y, float width, float height, float trimWidth, float trimHeight) =>
        new(x / trimWidth, y / trimHeight, width / trimWidth, height / trimHeight);
}
