using System.Reflection;
using SkiaSharp;

namespace CardMaker.Rendering.Tests;

/// <summary>
/// Font di test unico e deterministico (Roboto Regular, embedded), cosi' le metriche non
/// dipendono dai font installati sulla macchina/CI (vedi handover/08-resume-prompt.md).
/// </summary>
internal static class TestFonts
{
    public static SKTypeface Default { get; } = Load();

    private static SKTypeface Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("CardMaker.Rendering.Tests.TestAssets.Roboto-Regular.ttf")
            ?? throw new InvalidOperationException("Embedded test font not found.");
        return SKTypeface.FromStream(stream) ?? throw new InvalidOperationException("Failed to load test font.");
    }
}
