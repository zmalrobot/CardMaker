using CardMaker.Contracts.Geometry;

namespace CardMaker.Rendering.Placeholders;

public enum PlaceholderLayout
{
    Monster,
    MonsterPendulum,
    SpellTrap,
    Back,
    Pokemon,
    PokemonTrainer,
    PokemonEnergy,
}

/// <summary>
/// Descrive un frame segnaposto. Serve a sviluppare e testare l'intera catena prima che
/// esistano gli asset reali: stesse misure di <c>06-asset-spec.md</c>, estetica volutamente neutra.
/// </summary>
public sealed record PlaceholderFrameSpec
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public required uint FrameColor { get; init; }

    public PlaceholderLayout Layout { get; init; } = PlaceholderLayout.Monster;

    public bool HasDefenseBox { get; init; } = true;

    public bool ShowGuides { get; init; }

    public static IReadOnlyList<PlaceholderFrameSpec> YuGiOhSet() =>
    [
        new() { Key = "monster-normal", Label = "MONSTER NORMALE", FrameColor = 0xFFFDE68A },
        new() { Key = "monster-effect", Label = "MONSTER EFFETTO", FrameColor = 0xFFFF8B53 },
        new() { Key = "monster-ritual", Label = "MONSTER RITUALE", FrameColor = 0xFF9DB5CC },
        new() { Key = "monster-fusion", Label = "MONSTER FUSIONE", FrameColor = 0xFFA086B7 },
        new() { Key = "monster-synchro", Label = "MONSTER SYNCHRO", FrameColor = 0xFFEFEFEF },
        new() { Key = "monster-xyz", Label = "MONSTER XYZ", FrameColor = 0xFF2B2B2B },
        new() { Key = "monster-link", Label = "MONSTER LINK", FrameColor = 0xFF0C6FB0, HasDefenseBox = false },
        new() { Key = "token", Label = "TOKEN", FrameColor = 0xFFA9A9A9 },
        new() { Key = "spell", Label = "MAGIA", FrameColor = 0xFF1D9E74, Layout = PlaceholderLayout.SpellTrap, HasDefenseBox = false },
        new() { Key = "trap", Label = "TRAPPOLA", FrameColor = 0xFFBC5A84, Layout = PlaceholderLayout.SpellTrap, HasDefenseBox = false },
        new() { Key = "skill", Label = "SKILL", FrameColor = 0xFF6FA4C8, Layout = PlaceholderLayout.SpellTrap, HasDefenseBox = false },
        new() { Key = "pendulum-effect", Label = "PENDULUM EFFETTO", FrameColor = 0xFFFF8B53, Layout = PlaceholderLayout.MonsterPendulum },
        new() { Key = "rush-monster-effect", Label = "RUSH \u2014 EFFETTO", FrameColor = 0xFFE8663D },
        new() { Key = "rush-spell", Label = "RUSH \u2014 MAGIA", FrameColor = 0xFF16A085, Layout = PlaceholderLayout.SpellTrap, HasDefenseBox = false },
        new() { Key = "back-classic", Label = "RETRO", FrameColor = 0xFF7A4B2A, Layout = PlaceholderLayout.Back },
    ];

    public static IReadOnlyList<PlaceholderFrameSpec> PokemonSet() =>
    [
        new() { Key = "pokemon-frame-grass", Label = "POKÉMON ERBA", FrameColor = 0xFF5DBE62, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-fire", Label = "POKÉMON FUOCO", FrameColor = 0xFFE8553E, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-water", Label = "POKÉMON ACQUA", FrameColor = 0xFF4A90E2, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-lightning", Label = "POKÉMON LAMPO", FrameColor = 0xFFF5B025, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-psychic", Label = "POKÉMON PSICO", FrameColor = 0xFF8E44AD, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-fighting", Label = "POKÉMON LOTTA", FrameColor = 0xFFC0392B, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-darkness", Label = "POKÉMON OSCURITÀ", FrameColor = 0xFF34495E, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-metal", Label = "POKÉMON METALLO", FrameColor = 0xFF95A5A6, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-fairy", Label = "POKÉMON FOLLETTO", FrameColor = 0xFFE84393, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-dragon", Label = "POKÉMON DRAGO", FrameColor = 0xFFB7950B, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-colorless", Label = "POKÉMON INCOLORE", FrameColor = 0xFFBDC3C7, Layout = PlaceholderLayout.Pokemon },
        new() { Key = "pokemon-frame-trainer", Label = "ALLENATORE", FrameColor = 0xFF00A8FF, Layout = PlaceholderLayout.PokemonTrainer },
        new() { Key = "pokemon-frame-energy", Label = "ENERGIA", FrameColor = 0xFF2ECC71, Layout = PlaceholderLayout.PokemonEnergy },
        new() { Key = "pokemon-back", Label = "POKÉMON RETRO", FrameColor = 0xFF0984E3, Layout = PlaceholderLayout.Back },
    ];
}

/// <summary>Rettangoli notevoli del frame, equivalenti al file .meta.json chiesto al grafico.</summary>
public sealed record PlaceholderRegions
{
    public required NormalizedRect ArtWindow { get; init; }

    public required NormalizedRect NameBox { get; init; }

    public required NormalizedRect AttributeBox { get; init; }

    public required NormalizedRect LevelStrip { get; init; }

    public required NormalizedRect TypeLineBox { get; init; }

    public required NormalizedRect EffectBox { get; init; }

    public NormalizedRect? PendulumBox { get; init; }

    public required NormalizedRect AtkBox { get; init; }

    public NormalizedRect? DefBox { get; init; }
}
