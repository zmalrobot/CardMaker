namespace CardMaker.Application.Assets;

public interface IYuGiOhFontSeeder
{
    Task SeedDefaultFontsAsync(CancellationToken cancellationToken = default);
}
