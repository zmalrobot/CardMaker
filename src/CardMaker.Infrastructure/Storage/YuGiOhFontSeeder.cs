using System.Reflection;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

public sealed class YuGiOhFontSeeder(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger<YuGiOhFontSeeder> logger) : IYuGiOhFontSeeder
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

    public async Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default)
    {
        var game = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == YuGiOhSeedData.GameKey, cancellationToken)
            .ConfigureAwait(false);

        var gameId = game?.Id;
        var asm = typeof(YuGiOhFontSeeder).Assembly;

        foreach (var (alias, fileName, license) in DefaultMappings)
        {
            var exists = await db.FontAssets.AnyAsync(
                f => f.GameId == gameId && f.Alias == alias,
                cancellationToken).ConfigureAwait(false);

            if (exists)
            {
                continue;
            }

            var resourceName = $"CardMaker.Infrastructure.Resources.Fonts.{fileName}";
            await using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                logger.LogWarning("Risorsa font '{ResourceName}' non trovata nell'assembly", resourceName);
                continue;
            }

            var outcome = await fontCatalog.RegisterAsync(stream, new FontRegistrationRequest
            {
                FileName = fileName,
                Alias = alias,
                LicenseNote = license,
                GameId = gameId,
            }, cancellationToken).ConfigureAwait(false);

            if (!outcome.Succeeded)
            {
                logger.LogWarning("Impossibile registrare il font default '{FileName}' per il ruolo '{Alias}': {ErrorCode}",
                    fileName, alias, outcome.ErrorCode);
            }
            else
            {
                logger.LogInformation("Font default registrato con successo: ruolo '{Alias}', file '{FileName}'",
                    alias, fileName);
            }
        }
    }
}
