using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardMaker.Infrastructure.Storage;

public sealed class AssetService(
    CardMakerDbContext db,
    IAssetStore store,
    IImageProcessor imageProcessor,
    IFontProcessor fontProcessor,
    IOptions<UploadLimits> limits,
    ILogger<AssetService>? logger = null) : IAssetCatalog
{
    private readonly UploadLimits _limits = limits.Value;

    public async Task<AssetUploadOutcome> UploadAsync(
        Stream content,
        AssetUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.LicenseNote))
        {
            // La provenienza e' obbligatoria: e' il presidio che regge il principio "nessun asset nostro" (ADR-010).
            return AssetUploadOutcome.Fail("asset.licenseRequired");
        }

        var maxBytes = request.Kind == UploadKind.Font ? _limits.MaxFontBytes : _limits.MaxImageBytes;
        using var buffer = new MemoryStream();
        await CopyBoundedAsync(content, buffer, maxBytes, cancellationToken).ConfigureAwait(false);
        if (buffer.Length > maxBytes)
        {
            return AssetUploadOutcome.Fail("upload.tooLarge");
        }

        var raw = buffer.ToArray();
        var validation = UploadValidator.Validate(raw, request.Kind, _limits);
        if (!validation.IsValid)
        {
            return AssetUploadOutcome.Fail(validation.ErrorCode!);
        }

        byte[] payload;
        var contentType = validation.DetectedContentType!;
        var width = 0;
        var height = 0;

        if (request.Kind == UploadKind.Image)
        {
            var normalized = imageProcessor.Normalize(raw);
            if (normalized is null)
            {
                return AssetUploadOutcome.Fail("upload.imageRejected");
            }

            payload = normalized.Content;
            contentType = normalized.ContentType;
            width = normalized.Width;
            height = normalized.Height;
        }
        else
        {
            if (fontProcessor.Probe(raw) is null)
            {
                return AssetUploadOutcome.Fail("upload.fontRejected");
            }

            payload = raw;
        }

        var blob = await store.SaveAsync(new MemoryStream(payload, writable: false), cancellationToken)
            .ConfigureAwait(false);

        var existing = await db.Assets
            .FirstOrDefaultAsync(a => a.Sha256 == blob.Sha256 && a.GameId == request.GameId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            logger?.LogInformation("[Asset] Asset già presente per hash '{Sha256}' (ID: {Id}, File: '{FileName}')", existing.Sha256[..8], existing.Id, existing.OriginalFileName);
            return AssetUploadOutcome.Ok(existing);
        }

        var asset = new Asset
        {
            Sha256 = blob.Sha256,
            ByteSize = blob.ByteSize,
            OriginalFileName = Path.GetFileName(request.FileName),
            ContentType = contentType,
            PixelWidth = width,
            PixelHeight = height,
            Category = request.Category,
            LicenseNote = request.LicenseNote,
            SourceNote = request.SourceNote,
            GameId = request.GameId,
            UploadedByUserId = request.UploadedByUserId,
        };

        db.Assets.Add(asset);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger?.LogInformation("[Asset] Caricato asset '{FileName}' (ID: {Id}, Tipo: {ContentType}, {Bytes} bytes, {Width}x{Height} px)", asset.OriginalFileName, asset.Id, asset.ContentType, asset.ByteSize, asset.PixelWidth, asset.PixelHeight);
        return AssetUploadOutcome.Ok(asset);
    }

    public async Task<Asset?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Asset>> ListAsync(
        Guid? gameId = null,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var raw = await db.Assets.AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 5000))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (gameId.HasValue)
        {
            return raw.Where(a => a.GameId == gameId.Value).ToList();
        }

        return raw;
    }

    public Task<Stream?> OpenContentAsync(string sha256, CancellationToken cancellationToken = default) =>
        store.OpenReadAsync(sha256, cancellationToken);

    /// <summary>Copia al massimo <paramref name="maxBytes"/>+1 byte, per rilevare l'eccedenza senza leggere tutto.</summary>
    private static async Task CopyBoundedAsync(Stream source, Stream destination, long maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            total += read;
            await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            if (total > maxBytes)
            {
                return;
            }
        }
    }
}
