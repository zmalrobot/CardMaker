using System.Reflection;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

public sealed class MtgFontSeeder(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger<MtgFontSeeder> logger) : IMtgFontSeeder
{
    private static readonly (string Alias, string ResourceFileName, string License)[] DefaultMappings =
    [
        ("mtg-name", "Beleren2016-Bold.ttf", "Font ufficiale Magic: The Gathering (Beleren Bold)"),
        ("mtg-type-line", "Beleren2016-Bold.ttf", "Font ufficiale Magic: The Gathering (Beleren Bold)"),
        ("mtg-pt", "Beleren2016-Bold.ttf", "Font ufficiale Magic: The Gathering (Beleren Bold)"),
        ("mtg-header", "Beleren2016SmallCaps-Bold.ttf", "Font ufficiale Magic: The Gathering (Beleren SmallCaps)"),
        ("mtg-small-caps", "Beleren2016SmallCaps-Bold.ttf", "Font ufficiale Magic: The Gathering (Beleren SmallCaps)"),
        ("mtg-rules", "Mplantin.ttf", "Font ufficiale Magic: The Gathering (MPlantin)"),
        ("mtg-flavor", "Mplantin.ttf", "Font ufficiale Magic: The Gathering (MPlantin)"),
        ("mtg-body", "Mplantin.ttf", "Font ufficiale Magic: The Gathering (MPlantin)"),
        ("mtg-small", "Mplantin.ttf", "Font ufficiale Magic: The Gathering (MPlantin)"),
        ("mtg-collector", "Mplantin.ttf", "Font ufficiale Magic: The Gathering (MPlantin)"),
    ];

    public async Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default)
    {
        var game = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == MtgSeedData.GameKey, cancellationToken)
            .ConfigureAwait(false);

        var gameId = game?.Id;
        var asm = typeof(MtgFontSeeder).Assembly;

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
                logger.LogWarning("Risorsa font incorporata '{Resource}' non trovata per Magic.", resourceName);
                continue;
            }

            var outcome = await fontCatalog.RegisterAsync(stream, new FontRegistrationRequest
            {
                FileName = fileName,
                Alias = alias,
                GameId = gameId,
                LicenseNote = license,
            }, cancellationToken).ConfigureAwait(false);

            if (outcome.Succeeded)
            {
                logger.LogInformation("Registrato font Magic predefinito: '{Alias}' -> '{FileName}'", alias, fileName);
            }
            else
            {
                logger.LogWarning("Registrazione font predefinito '{Alias}' fallita: {Error}", alias, outcome.ErrorCode);
            }
        }
    }
}

