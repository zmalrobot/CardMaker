using System.Security.Cryptography;
using CardMaker.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardMaker.Infrastructure.Storage;

public sealed class AssetStoreOptions
{
    public string RootPath { get; set; } = string.Empty;
}

/// <summary>
/// Archivio su filesystem indirizzato dal contenuto: assets/ab/cd/abcd....bin
/// I due livelli di sottocartelle evitano directory con decine di migliaia di file.
/// </summary>
public sealed class FileSystemAssetStore : IAssetStore
{
    private readonly string _root;
    private readonly ILogger<FileSystemAssetStore> _logger;

    public FileSystemAssetStore(IOptions<AssetStoreOptions> options, ILogger<FileSystemAssetStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        _root = options.Value.RootPath;
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredBlob> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var temporaryPath = Path.Combine(_root, $".tmp-{Guid.CreateVersion7():N}");
        string hash;
        long size;

        await using (var temporary = new FileStream(
                         temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            using var sha = SHA256.Create();
            await using var hashing = new CryptoStream(temporary, sha, CryptoStreamMode.Write, leaveOpen: true);
            await content.CopyToAsync(hashing, cancellationToken).ConfigureAwait(false);
            await hashing.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);

            size = temporary.Length;
            hash = Convert.ToHexStringLower(sha.Hash!);
        }

        var finalPath = GetPath(hash);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        if (File.Exists(finalPath))
        {
            // Contenuto gia' presente: la deduplicazione e' un effetto gratuito dell'indirizzamento per hash.
            File.Delete(temporaryPath);
        }
        else
        {
            File.Move(temporaryPath, finalPath, overwrite: false);
            _logger.LogInformation("Asset memorizzato {Hash} ({Size} byte)", hash, size);
        }

        return new StoredBlob(hash, size);
    }

    public Task<Stream?> OpenReadAsync(string sha256, CancellationToken cancellationToken = default)
    {
        if (!TryGetValidatedPath(sha256, out var path) || !File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public bool Exists(string sha256) => TryGetValidatedPath(sha256, out var path) && File.Exists(path);

    public Task<bool> DeleteAsync(string sha256, CancellationToken cancellationToken = default)
    {
        if (!TryGetValidatedPath(sha256, out var path) || !File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    private string GetPath(string sha256) =>
        Path.Combine(_root, sha256[..2], sha256[2..4], sha256 + ".bin");

    private bool TryGetValidatedPath(string sha256, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(sha256) || sha256.Length != 64)
        {
            return false;
        }

        foreach (var c in sha256)
        {
            if (!char.IsAsciiDigit(c) && (c < 'a' || c > 'f'))
            {
                return false;
            }
        }

        path = GetPath(sha256);
        return true;
    }
}
