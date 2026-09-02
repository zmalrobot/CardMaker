namespace CardMaker.Application.Assets;

public interface IPokemonFontSeeder
{
    Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default);
}

