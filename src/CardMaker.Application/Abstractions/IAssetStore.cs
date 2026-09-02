namespace CardMaker.Application.Abstractions;

public sealed record StoredBlob(string Sha256, long ByteSize);

/// <summary>
/// Archivio binario content-addressed: il nome del file e' lo SHA-256 del contenuto (ADR-005).
/// Nessun percorso deriva mai dall'input utente, quindi il path traversal e' impossibile.
/// </summary>
public interface IAssetStore
{
    Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default);

    bool Exists(string sha256);

    Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default);
}
