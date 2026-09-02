namespace CardMaker.Domain.Common;

public abstract class Entity
{
    /// <summary>UUIDv7: ordinato nel tempo, riduce la frammentazione degli indici SQLite.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
