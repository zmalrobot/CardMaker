using CardMaker.Application.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

public sealed class YuGiOhFontSeeder(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger<YuGiOhFontSeeder> logger) : GameFontSeederBase(db, fontCatalog, logger), IYuGiOhFontSeeder
{
    private static readonly (string Alias, string ResourceFileName, string License)[] DefaultMappings =
    [
        ("card-name", "Matrix-Bold.otf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("atk-def-value", "Matrix-Bold.otf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("link-rating", "Matrix-Bold.otf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("pendulum-scale", "Matrix-Bold.otf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("rush-maximum-atk", "Matrix-Bold.otf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),

        ("atk-def-label", "MatrixBoldSmallCaps.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),

        ("effect", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("pendulum-effect", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("set-code", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("edition", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("passcode", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("copyright", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("rush-effect", "Stone Serif Regular.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),

        ("effect-italic", "Stone Serif Italic.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),

        ("type-line", "Stone Serif Semibold.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("spell-trap-label", "Stone Serif Semibold.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),
        ("effect-bold", "Stone Serif Semibold.ttf", "Font ufficiale Yu-Gi-Oh! (uso interno fan-made)"),

        ("rush-card-name", "FOT-Rodin Pro M.ttf", "Font ufficiale Rush Duel (uso interno fan-made)"),
        ("rush-section-label", "FOT-Rodin Pro M.ttf", "Font ufficiale Rush Duel (uso interno fan-made)"),
        ("rush-type-line", "FOT-Rodin Pro M.ttf", "Font ufficiale Rush Duel (uso interno fan-made)"),
    ];

    public Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default) =>
        SeedFontsCoreAsync(YuGiOhSeedData.GameKey, DefaultMappings, cancellationToken);
}
