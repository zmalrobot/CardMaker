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
        ("card-name", "GillSansBold.ttf", "Font Pokémon TCG fan-made"),
        ("card-name-bold", "GillSansBold.ttf", "Font Pokémon TCG fan-made"),
        ("hp", "Futura-Bold.ttf", "Font Pokémon TCG fan-made"),
        ("stage", "GillSansBold.ttf", "Font Pokémon TCG fan-made"),
        ("evolves-from", "GillSansItalic.ttf", "Font Pokémon TCG fan-made"),
        ("attack-name", "GillSansBold.ttf", "Font Pokémon TCG fan-made"),
        ("attack-damage", "Futura-Bold.ttf", "Font Pokémon TCG fan-made"),
        ("attack-desc", "GillSans.ttf", "Font Pokémon TCG fan-made"),
        ("trainer-desc", "GillSans.ttf", "Font Pokémon TCG fan-made"),
        ("special-desc", "GillSans.ttf", "Font Pokémon TCG fan-made"),
        ("body-text", "GillSans.ttf", "Font Pokémon TCG fan-made"),
        ("italic-text", "GillSansItalic.ttf", "Font Pokémon TCG fan-made"),
        ("bold-text", "GillSansBold.ttf", "Font Pokémon TCG fan-made"),
        ("subtext", "GillSans.ttf", "Font Pokémon TCG fan-made"),
        ("weakness-val", "GillSansBold.ttf", "Font Pokémon TCG fan-made"),
        ("flavor-text", "GillSansItalic.ttf", "Font Pokémon TCG fan-made"),
    ];

    public Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default) =>
        SeedFontsCoreAsync(PokemonSeedData.GameKey, DefaultMappings, cancellationToken);
}
