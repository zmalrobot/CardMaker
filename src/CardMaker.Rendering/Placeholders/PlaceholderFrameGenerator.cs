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
            DrawLabel(canvas, geometry, spec.Label);
        }

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
        PlaceholderLayout.Pokemon => new PlaceholderRegions
        {
            LevelStrip = new NormalizedRect(0.075, 0.035, 0.120, 0.050),
            NameBox = new NormalizedRect(0.200, 0.035, 0.430, 0.050),
            DefBox = new NormalizedRect(0.630, 0.035, 0.190, 0.050),
            AttributeBox = new NormalizedRect(0.835, 0.035, 0.090, 0.050),
            ArtWindow = new NormalizedRect(0.075, 0.100, 0.850, 0.420),
            TypeLineBox = new NormalizedRect(0.075, 0.522, 0.850, 0.028),
            EffectBox = new NormalizedRect(0.075, 0.555, 0.850, 0.325),
            AtkBox = new NormalizedRect(0.075, 0.885, 0.850, 0.045),
        },
        PlaceholderLayout.PokemonTrainer => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.080, 0.040, 0.650, 0.050),
            AttributeBox = new NormalizedRect(0.740, 0.040, 0.180, 0.045),
            LevelStrip = new NormalizedRect(0.080, 0.095, 0.840, 0.030),
            ArtWindow = new NormalizedRect(0.080, 0.130, 0.840, 0.440),
            TypeLineBox = new NormalizedRect(0.080, 0.575, 0.840, 0.020),
            EffectBox = new NormalizedRect(0.080, 0.600, 0.840, 0.285),
            AtkBox = new NormalizedRect(0.080, 0.890, 0.840, 0.045),
            DefBox = null,
        },
        PlaceholderLayout.PokemonEnergy => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.080, 0.040, 0.700, 0.050),
            AttributeBox = new NormalizedRect(0.800, 0.035, 0.120, 0.060),
            LevelStrip = new NormalizedRect(0, 0, 0, 0),
            ArtWindow = new NormalizedRect(0.150, 0.200, 0.700, 0.500),
            TypeLineBox = new NormalizedRect(0, 0, 0, 0),
            EffectBox = new NormalizedRect(0.080, 0.720, 0.840, 0.160),
            AtkBox = new NormalizedRect(0, 0, 0, 0),
            DefBox = null,
        },
        PlaceholderLayout.Mtg => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.065, 0.040, 0.580, 0.048),
            AttributeBox = new NormalizedRect(0.650, 0.040, 0.285, 0.048),
            LevelStrip = new NormalizedRect(0, 0, 0, 0),
            ArtWindow = new NormalizedRect(0.075, 0.100, 0.850, 0.450),
            TypeLineBox = new NormalizedRect(0.065, 0.558, 0.870, 0.046),
            EffectBox = new NormalizedRect(0.065, 0.612, 0.870, 0.260),
            AtkBox = new NormalizedRect(0.740, 0.865, 0.180, 0.048),
            DefBox = null,
        },
        _ => new PlaceholderRegions
        {
            NameBox = new NormalizedRect(0.065, 0.030, 0.730, 0.068),
            AttributeBox = new NormalizedRect(0.815, 0.028, 0.120, 0.072),
            LevelStrip = new NormalizedRect(0.100, 0.115, 0.800, 0.042),
            ArtWindow = new NormalizedRect(0.115, 0.170, 0.770, 0.540),
            TypeLineBox = new NormalizedRect(0.070, 0.748, 0.860, 0.032),
            EffectBox = new NormalizedRect(0.070, 0.785, 0.860, 0.145),
            AtkBox = new NormalizedRect(0.510, 0.935, 0.200, 0.032),
            DefBox = new NormalizedRect(0.730, 0.935, 0.200, 0.032),
        },
    };

    private static void DrawBackground(SKCanvas canvas, CardGeometry geometry, PlaceholderFrameSpec spec)
    {
        var frameColor = new SKColor(spec.FrameColor);

        using var background = new SKPaint { Color = frameColor, IsAntialias = true };
        canvas.DrawRect(0, 0, geometry.MasterWidthPx, geometry.MasterHeightPx, background);

        // Cornice interna: visibile solo per Yu-Gi-Oh (il frame originale ha una linea di delimitazione esterna)
        if (spec.Layout is PlaceholderLayout.Monster or PlaceholderLayout.MonsterPendulum or PlaceholderLayout.SpellTrap)
        {
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
    }

    private static void DrawBoxes(SKCanvas canvas, CardGeometry geometry, PlaceholderFrameSpec spec, PlaceholderRegions regions)
    {
        var frameColor = new SKColor(spec.FrameColor);

        if (spec.Layout == PlaceholderLayout.Pokemon)
        {
            var headerRect = new NormalizedRect(0.060, 0.026, 0.880, 0.066);
            DrawBox(canvas, geometry, headerRect, Lighten(frameColor, 0.25f));
            DrawBox(canvas, geometry, regions.TypeLineBox, Parchment.WithAlpha(0xF0));
            DrawBox(canvas, geometry, regions.EffectBox, Parchment);
            DrawBox(canvas, geometry, regions.AtkBox, Lighten(frameColor, 0.20f));
            return;
        }

        if (spec.Layout == PlaceholderLayout.Mtg)
        {
            var headerRect = new NormalizedRect(0.065, 0.034, 0.870, 0.058);
            DrawBox(canvas, geometry, headerRect, Lighten(frameColor, 0.25f));
            DrawBox(canvas, geometry, regions.TypeLineBox, Lighten(frameColor, 0.20f));
            DrawBox(canvas, geometry, regions.EffectBox, Parchment);
            if (regions.AtkBox is { } pt)
            {
                DrawBox(canvas, geometry, pt, Lighten(frameColor, 0.30f));
            }
            return;
        }

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
