using CardMaker.Application.Assets;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Storage;

public sealed class PokemonFontSeeder(
    CardMakerDbContext db,
    IFontCatalog fontCatalog,
    ILogger<PokemonFontSeeder> logger) : GameFontSeederBase(db, fontCatalog, logger), IPokemonFontSeeder
{
    private static readonly (string Alias, string ResourceFileName, string License)[] DefaultMappings =
    [
        ("card-name", "Gill Sans Std Bold.otf", "Font Pokémon TCG fan-made"),
        ("card-name-bold", "Gill Sans Std Bold.otf", "Font Pokémon TCG fan-made"),
        ("hp", "Gill Sans Std Extra Bold.otf", "Font Pokémon TCG fan-made"),
        ("stage", "Gill Sans Std Bold.otf", "Font Pokémon TCG fan-made"),
        ("evolves-from", "Gill Sans Std Italic.otf", "Font Pokémon TCG fan-made"),
        ("attack-name", "Gill Sans Std Bold.otf", "Font Pokémon TCG fan-made"),
        ("attack-damage", "Gill Sans Std Extra Bold.otf", "Font Pokémon TCG fan-made"),
        ("attack-desc", "Optima Medium.otf", "Font Pokémon TCG fan-made"),
        ("trainer-desc", "Optima Medium.otf", "Font Pokémon TCG fan-made"),
        ("special-desc", "Optima Medium.otf", "Font Pokémon TCG fan-made"),
        ("body-text", "Optima Medium.otf", "Font Pokémon TCG fan-made"),
        ("italic-text", "Optima Italic.otf", "Font Pokémon TCG fan-made"),
        ("bold-text", "Optima Bold.otf", "Font Pokémon TCG fan-made"),
        ("subtext", "Gill Sans Std Regular.otf", "Font Pokémon TCG fan-made"),
        ("weakness-val", "Gill Sans Std Bold.otf", "Font Pokémon TCG fan-made"),
        ("flavor-text", "Optima Italic.otf", "Font Pokémon TCG fan-made"),
    ];

    public Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default) =>
        SeedFontsCoreAsync(PokemonSeedData.GameKey, DefaultMappings, cancellationToken);
}
