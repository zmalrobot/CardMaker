using System.Runtime.CompilerServices;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

/// <summary>
/// Confronto pixel con tolleranza rispetto a un'immagine di riferimento salvata su disco.
/// Impostare la variabile d'ambiente <c>UPDATE_GOLDEN=1</c> per (ri)generare i riferimenti.
/// </summary>
internal static class GoldenImageAssert
{
    public static void Matches(
        byte[] actualPng,
        string goldenName,
        double maxDifferentPixelFraction = 0.01,
        int perChannelTolerance = 12,
        [CallerFilePath] string callerFilePath = "")
    {
        var callerDir = Path.GetDirectoryName(callerFilePath);
        if (string.IsNullOrEmpty(callerDir) || callerDir.StartsWith("/_", StringComparison.Ordinal) || !Directory.Exists(callerDir))
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.Exists)
            {
                var candidate = Path.Combine(dir.FullName, "tests", "CardMaker.Rendering.Tests", "GoldenImages");
                if (Directory.Exists(candidate))
                {
                    callerDir = Path.GetDirectoryName(candidate);
                    break;
                }
                dir = dir.Parent;
            }
        }

        var goldenDir = Path.Combine(callerDir ?? AppContext.BaseDirectory, "GoldenImages");
        Directory.CreateDirectory(goldenDir);
        var goldenPath = Path.Combine(goldenDir, goldenName + ".png");

        if (!File.Exists(goldenPath) || Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
        {
            File.WriteAllBytes(goldenPath, actualPng);
            return;
        }

        using var actual = SKBitmap.Decode(actualPng);
        using var expected = SKBitmap.Decode(File.ReadAllBytes(goldenPath));

        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        var totalPixels = expected.Width * expected.Height;
        var differentPixels = 0;

        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                var a = actual.GetPixel(x, y);
                var e = expected.GetPixel(x, y);

                if (Math.Abs(a.Red - e.Red) > perChannelTolerance
                    || Math.Abs(a.Green - e.Green) > perChannelTolerance
                    || Math.Abs(a.Blue - e.Blue) > perChannelTolerance
                    || Math.Abs(a.Alpha - e.Alpha) > perChannelTolerance)
                {
                    differentPixels++;
                }
            }
        }

        var fraction = (double)differentPixels / totalPixels;
        Assert.True(fraction <= maxDifferentPixelFraction,
            $"'{goldenName}': {differentPixels}/{totalPixels} pixel diversi ({fraction:P2}), tollerato {maxDifferentPixelFraction:P2}.");
    }
}
