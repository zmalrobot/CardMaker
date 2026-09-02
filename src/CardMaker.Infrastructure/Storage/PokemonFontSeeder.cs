using System.Reflection;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

public sealed class PokemonFontSeeder(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger<PokemonFontSeeder> logger) : IPokemonFontSeeder
{
    private static readonly (string Alias, string ResourceFileName, string License)[] DefaultMappings =
    [
        ("pokemon-name", "GillSansBold.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-hp", "Futura-Bold.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-stage", "GillSansBold.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-attack-name", "GillSansBold.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-attack-damage", "Futura-Bold.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-body", "GillSans.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-flavor", "GillSansItalic.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-small", "GillSans.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-illustrator", "GillSans.ttf", "Font ufficiale Pokémon TCG"),
        ("pokemon-rule", "GillSans.ttf", "Font ufficiale Pokémon TCG"),
    ];

    public async Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default)
    {
        var game = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == PokemonSeedData.GameKey, cancellationToken)
            .ConfigureAwait(false);

        var gameId = game?.Id;
        var asm = typeof(PokemonFontSeeder).Assembly;

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
                logger.LogWarning("Risorsa font incorporata '{Resource}' non trovata per Pokémon.", resourceName);
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
                logger.LogInformation("Registrato font Pokémon predefinito: '{Alias}' -> '{FileName}'", alias, fileName);
            }
            else
            {
                logger.LogWarning("Registrazione font predefinito '{Alias}' fallita: {Error}", alias, outcome.ErrorCode);
            }
        }
    }
}

