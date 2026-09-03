namespace CardMaker.Application.Assets;

public sealed record PlaceholderSeedResult(int Created, int AlreadyPresent, IReadOnlyList<string> Keys);

/// <summary>
/// Genera i frame segnaposto usati finche' non arrivano gli asset reali.
/// </summary>
public interface IPlaceholderSeeder
{
    Task<PlaceholderSeedResult> SeedYuGiOhAsync(
        string? userId = null,
        bool showGuides = false,
        Guid? gameId = null,
        CancellationToken cancellationToken = default);

    Task<PlaceholderSeedResult> SeedPokemonAsync(
        string? userId = null,
        bool showGuides = false,
        Guid? gameId = null,
        CancellationToken cancellationToken = default);

    Task<PlaceholderSeedResult> SeedMtgAsync(
        string? userId = null,
        bool showGuides = false,
        Guid? gameId = null,
        CancellationToken cancellationToken = default);
}
