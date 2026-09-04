using System.Reflection;
using CardMaker.Application.Assets;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

/// <summary>
/// Classe base per il seed dei font incorporati nei cataloghi di gioco (DUP-001).
/// </summary>
public abstract class GameFontSeederBase(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger logger)
{
    protected async Task SeedFontsCoreAsync(
        string gameKey,
        IReadOnlyList<(string Alias, string ResourceFileName, string License)> mappings,
        CancellationToken cancellationToken = default)
    {
        var game = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == gameKey, cancellationToken)
            .ConfigureAwait(false);

        var gameId = game?.Id;
        var asm = typeof(GameFontSeederBase).Assembly;

        var existingAliases = gameId.HasValue
            ? (await db.FontAssets
                .Where(f => f.GameId == gameId.Value)
                .Select(f => f.Alias)
                .ToListAsync(cancellationToken).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (alias, fileName, license) in mappings)
        {
            if (existingAliases.Contains(alias))
            {
                continue;
            }

            var resourceName = $"CardMaker.Infrastructure.Resources.Fonts.{fileName}";
            await using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                logger.LogWarning("Risorsa font '{ResourceName}' non trovata nell'assembly per il gioco '{GameKey}'", resourceName, gameKey);
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
                logger.LogWarning("Impossibile registrare il font default '{FileName}' per il ruolo '{Alias}' ({GameKey}): {ErrorCode}",
                    fileName, alias, gameKey, outcome.ErrorCode);
            }
            else
            {
                logger.LogInformation("Font default registrato con successo: ruolo '{Alias}', file '{FileName}' ({GameKey})",
                    alias, fileName, gameKey);
            }
        }
    }
}
