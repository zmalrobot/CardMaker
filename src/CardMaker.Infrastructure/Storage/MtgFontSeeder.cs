using CardMaker.Application.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

public sealed class MtgFontSeeder(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger<MtgFontSeeder> logger) : GameFontSeederBase(db, fontCatalog, logger), IMtgFontSeeder
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

    public Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default) =>
        SeedFontsCoreAsync(MtgSeedData.GameKey, DefaultMappings, cancellationToken);
}
