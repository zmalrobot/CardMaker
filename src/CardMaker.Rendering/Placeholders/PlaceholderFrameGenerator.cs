using CardMaker.Contracts.Geometry;
using SkiaSharp;

namespace CardMaker.Rendering.Placeholders;

/// <summary>
/// Genera frame segnaposto conformi alla specifica asset: master canvas con abbondanza,
/// finestra dell'artwork <b>trasparente</b>, nessun testo di carta e angoli non arrotondati.
/// </summary>
public sealed class PlaceholderFrameGenerator
{
    private static readonly SKColor Ink = new(0x20, 0x20, 0x20);
    private static readonly SKColor Parchment = new(0xFF, 0xFA, 0xE8);

    public byte[] Generate(PlaceholderFrameSpec spec, CardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(geometry);

        var info = new SKImageInfo(geometry.MasterWidthPx, geometry.MasterHeightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var regions = GetRegions(spec.Layout);

        DrawBackground(canvas, geometry, spec);

        if (spec.Layout == PlaceholderLayout.Back)
        {
            DrawBackFace(canvas, geometry, spec);
        }
        else
        {
            DrawBoxes(canvas, geometry, spec, regions);
            ClearArtWindow(canvas, geometry, regions.ArtWindow);
            if (regions.PendulumBox is { } pendulum)
            {
                DrawBox(canvas, geometry, pendulum, Parchment.WithAlpha(0xEE));
            }
        }

        if (spec.ShowGuides)
        {
            DrawGuides(canvas, geometry);
        }

        DrawLabel(canvas, geometry, spec.Label);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    public static PlaceholderRegions GetRegions(PlaceholderLayout layout) => layout switch
    {
        PlaceholderLayout.MonsterPendulum => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.070, 0.038, 0.720, 0.058),
            AttributeBox = new NormalizedRect(0.820, 0.033, 0.110, 0.072),
            LevelStrip = new NormalizedRect(0.100, 0.120, 0.800, 0.042),
            ArtWindow = new NormalizedRect(0.050, 0.170, 0.900, 0.480),
            PendulumBox = new NormalizedRect(0.075, 0.660, 0.850, 0.090),
            TypeLineBox = new NormalizedRect(0.070, 0.762, 0.860, 0.030),
            EffectBox = new NormalizedRect(0.070, 0.796, 0.860, 0.132),
            AtkBox = new NormalizedRect(0.540, 0.938, 0.180, 0.030),
            DefBox = new NormalizedRect(0.740, 0.938, 0.180, 0.030),
        },
        PlaceholderLayout.SpellTrap => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.070, 0.038, 0.720, 0.058),
            AttributeBox = new NormalizedRect(0.820, 0.033, 0.110, 0.072),
            LevelStrip = new NormalizedRect(0.500, 0.112, 0.430, 0.032),
            ArtWindow = new NormalizedRect(0.115, 0.165, 0.770, 0.540),
            TypeLineBox = new NormalizedRect(0.070, 0.720, 0.860, 0.030),
            EffectBox = new NormalizedRect(0.070, 0.756, 0.860, 0.185),
            AtkBox = new NormalizedRect(0.540, 0.950, 0.180, 0.028),
            DefBox = null,
        },
        _ => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.070, 0.038, 0.720, 0.058),
            AttributeBox = new NormalizedRect(0.820, 0.033, 0.110, 0.072),
            LevelStrip = new NormalizedRect(0.100, 0.120, 0.800, 0.042),
            ArtWindow = new NormalizedRect(0.115, 0.175, 0.770, 0.560),
            TypeLineBox = new NormalizedRect(0.070, 0.752, 0.860, 0.032),
            EffectBox = new NormalizedRect(0.070, 0.790, 0.860, 0.140),
            AtkBox = new NormalizedRect(0.540, 0.940, 0.180, 0.030),
            DefBox = new NormalizedRect(0.740, 0.940, 0.180, 0.030),
        },
    };

    private static void DrawBackground(SKCanvas canvas, CardGeometry geometry, PlaceholderFrameSpec spec)
    {
        var frameColor = new SKColor(spec.FrameColor);

        using var background = new SKPaint { Color = frameColor, IsAntialias = true };
        canvas.DrawRect(0, 0, geometry.MasterWidthPx, geometry.MasterHeightPx, background);

        // Cornice interna, per rendere visibile dove finisce l'area di taglio.
        var inset = geometry.BleedPx + (geometry.TrimWidthPx * 0.03f);
        using var border = new SKPaint
        {
            Color = Darken(frameColor, 0.72f),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = geometry.TrimWidthPx * 0.006f,
        };
        canvas.DrawRect(
            inset,
            inset,
            geometry.MasterWidthPx - (2 * inset),
            geometry.MasterHeightPx - (2 * inset),
            border);
    }

    private static void DrawBoxes(SKCanvas canvas, CardGeometry geometry, PlaceholderFrameSpec spec, PlaceholderRegions regions)
    {
        var frameColor = new SKColor(spec.FrameColor);

        DrawBox(canvas, geometry, regions.NameBox, Lighten(frameColor, 0.35f));
        DrawBox(canvas, geometry, regions.AttributeBox, Lighten(frameColor, 0.45f));
        DrawBox(canvas, geometry, regions.LevelStrip, Lighten(frameColor, 0.20f));
        DrawBox(canvas, geometry, regions.TypeLineBox, Parchment.WithAlpha(0xF0));
        DrawBox(canvas, geometry, regions.EffectBox, Parchment);
        DrawBox(canvas, geometry, regions.AtkBox, Lighten(frameColor, 0.25f));

        if (spec.HasDefenseBox && regions.DefBox is { } def)
        {
            DrawBox(canvas, geometry, def, Lighten(frameColor, 0.25f));
        }
    }

    private static void DrawBox(SKCanvas canvas, CardGeometry geometry, NormalizedRect rect, SKColor color)
    {
        var (x, y, w, h) = geometry.ToMasterPixels(rect);
        using var fill = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawRoundRect(new SKRect(x, y, x + w, y + h), 6, 6, fill);

        using var outline = new SKPaint
        {
            Color = Ink.WithAlpha(0x50),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
        };
        canvas.DrawRoundRect(new SKRect(x, y, x + w, y + h), 6, 6, outline);
    }

    /// <summary>La finestra dell'artwork deve restare trasparente: l'immagine va disegnata sotto il frame.</summary>
    private static void ClearArtWindow(SKCanvas canvas, CardGeometry geometry, NormalizedRect artWindow)
    {
        var (x, y, w, h) = geometry.ToMasterPixels(artWindow);

        using var frame = new SKPaint
        {
            Color = Ink.WithAlpha(0xAA),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = geometry.TrimWidthPx * 0.008f,
        };
        canvas.DrawRect(new SKRect(x, y, x + w, y + h), frame);

        using var clear = new SKPaint { BlendMode = SKBlendMode.Clear };
        var stroke = frame.StrokeWidth / 2f;
        canvas.DrawRect(new SKRect(x + stroke, y + stroke, x + w - stroke, y + h - stroke), clear);
    }

    private static void DrawBackFace(SKCanvas canvas, CardGeometry geometry, PlaceholderFrameSpec spec)
    {
        var frameColor = new SKColor(spec.FrameColor);
        var centerX = geometry.MasterWidthPx / 2f;
        var centerY = geometry.MasterHeightPx / 2f;

        using var disc = new SKPaint { Color = Lighten(frameColor, 0.30f), IsAntialias = true };
        canvas.DrawCircle(centerX, centerY, geometry.TrimWidthPx * 0.28f, disc);

        using var ring = new SKPaint
        {
            Color = Darken(frameColor, 0.65f),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = geometry.TrimWidthPx * 0.015f,
        };
        canvas.DrawCircle(centerX, centerY, geometry.TrimWidthPx * 0.34f, ring);
    }

    private static void DrawGuides(SKCanvas canvas, CardGeometry geometry)
    {
        using var trim = new SKPaint
        {
            Color = SKColors.Magenta.WithAlpha(0xB0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            PathEffect = SKPathEffect.CreateDash([18f, 12f], 0),
        };
        canvas.DrawRect(
            geometry.BleedPx,
            geometry.BleedPx,
            geometry.TrimWidthPx,
            geometry.TrimHeightPx,
            trim);

        using var safe = new SKPaint
        {
            Color = SKColors.Cyan.WithAlpha(0xB0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            PathEffect = SKPathEffect.CreateDash([10f, 10f], 0),
        };
        var offset = geometry.BleedPx + geometry.SafeZonePx;
        canvas.DrawRect(
            offset,
            offset,
            geometry.TrimWidthPx - (2 * geometry.SafeZonePx),
            geometry.TrimHeightPx - (2 * geometry.SafeZonePx),
            safe);
    }

    private static void DrawLabel(SKCanvas canvas, CardGeometry geometry, string label)
    {
        using var typeface = SKTypeface.FromFamilyName(
            "Segoe UI",
            SKFontStyleWeight.Bold,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright) ?? SKTypeface.Default;

        using var font = new SKFont(typeface, geometry.TrimHeightPx * 0.018f);
        using var paint = new SKPaint { Color = Ink.WithAlpha(0xAA), IsAntialias = true };

        // In alto, sopra la casella del nome: in basso si sovrapporrebbe ad ATK/DEF.
        var y = geometry.BleedPx + (geometry.TrimHeightPx * 0.030f);
        canvas.DrawText(label, geometry.MasterWidthPx / 2f, y, SKTextAlign.Center, font, paint);
    }

    private static SKColor Lighten(SKColor color, float amount) => new(
        (byte)Math.Clamp(color.Red + (255 - color.Red) * amount, 0, 255),
        (byte)Math.Clamp(color.Green + (255 - color.Green) * amount, 0, 255),
        (byte)Math.Clamp(color.Blue + (255 - color.Blue) * amount, 0, 255),
        color.Alpha);

    private static SKColor Darken(SKColor color, float factor) => new(
        (byte)Math.Clamp(color.Red * factor, 0, 255),
        (byte)Math.Clamp(color.Green * factor, 0, 255),
        (byte)Math.Clamp(color.Blue * factor, 0, 255),
        color.Alpha);
}
