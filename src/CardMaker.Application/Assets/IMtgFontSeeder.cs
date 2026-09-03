namespace CardMaker.Application.Assets;

public interface IMtgFontSeeder
{
    Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default);
}

