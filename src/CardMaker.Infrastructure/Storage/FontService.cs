using System.Text.RegularExpressions;
using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Storage;

public sealed partial class FontService(
    CardMakerDbContext db,
    IAssetCatalog assets,
    IAssetStore store,
    IFontProcessor fontProcessor) : IFontCatalog
{
    public async Task<FontRegistrationOutcome> RegisterAsync(
        Stream content,
        FontRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alias = NormalizeAlias(request.Alias);
        if (alias is null)
        {
            return FontRegistrationOutcome.Fail("font.invalidAlias");
        }

        var duplicate = await db.FontAssets
            .AnyAsync(f => f.GameId == request.GameId && f.Alias == alias, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate)
        {
            return FontRegistrationOutcome.Fail("font.aliasAlreadyUsed");
        }

        var upload = await assets.UploadAsync(
            content,
            new AssetUploadRequest
            {
                FileName = request.FileName,
                Category = AssetCategory.Font,
                LicenseNote = request.LicenseNote,
                GameId = request.GameId,
                UploadedByUserId = request.UploadedByUserId,
                Kind = UploadKind.Font,
            },
            cancellationToken).ConfigureAwait(false);

        if (!upload.Succeeded)
        {
            return FontRegistrationOutcome.Fail(upload.ErrorCode!);
        }

        var bytes = await ReadAssetAsync(upload.Asset!.Sha256, cancellationToken).ConfigureAwait(false);
        var info = bytes is null ? null : fontProcessor.Probe(bytes);
        if (info is null)
        {
            // SkiaSharp non sa aprire il file: sarebbe illeggibile anche in fase di render.
            return FontRegistrationOutcome.Fail("font.notReadableByRenderer");
        }

        var font = new FontAsset
        {
            AssetId = upload.Asset.Id,
            GameId = request.GameId,
            Alias = alias,
            FamilyName = info.FamilyName,
            StyleName = info.StyleName,
            Weight = info.Weight,
            IsItalic = info.IsItalic,
            LicenseNote = request.LicenseNote,
        };

        db.FontAssets.Add(font);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        font.Asset = upload.Asset;
        return FontRegistrationOutcome.Ok(font);
    }

    public async Task<IReadOnlyList<FontAsset>> ListAsync(
        Guid? gameId = null,
        CancellationToken cancellationToken = default)
    {
        var raw = await db.FontAssets.AsNoTracking()
            .Include(f => f.Asset)
            .OrderBy(f => f.Alias)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (gameId.HasValue)
        {
            return raw.Where(f => f.GameId == gameId.Value).ToList();
        }

        return raw;
    }

    public async Task<FontAsset?> FindByAliasAsync(
        Guid? gameId,
        string roleAlias,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAlias(roleAlias);
        if (normalized is null)
        {
            return null;
        }

        return await db.FontAssets.AsNoTracking()
            .Include(f => f.Asset)
            .Where(f => f.Alias == normalized && (gameId == null || f.GameId == gameId || f.GameId == null))
            .OrderBy(f => f.GameId == gameId ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<byte[]?> GetBytesAsync(Guid fontAssetId, CancellationToken cancellationToken = default)
    {
        var font = await db.FontAssets.AsNoTracking()
            .Include(f => f.Asset)
            .FirstOrDefaultAsync(f => f.Id == fontAssetId, cancellationToken)
            .ConfigureAwait(false);

        return font is null ? null : await ReadAssetAsync(font.Asset.Sha256, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]?> GetBytesByAliasAsync(
        Guid? gameId,
        string roleAlias,
        CancellationToken cancellationToken = default)
    {
        var font = await FindByAliasAsync(gameId, roleAlias, cancellationToken).ConfigureAwait(false);
        if (font is not null)
        {
            var bytes = await ReadAssetAsync(font.Asset.Sha256, cancellationToken).ConfigureAwait(false);
            if (bytes is not null && bytes.Length > 0)
            {
                return bytes;
            }
        }

        return GetEmbeddedFontBytes(roleAlias);
    }

    public static byte[]? GetEmbeddedFontBytes(string roleAlias)
    {
        var normalized = NormalizeAlias(roleAlias);
        if (normalized is null)
        {
            return null;
        }

        var fileName = normalized switch
        {
            // Yu-Gi-Oh!
            "card-name" or "atk-def-value" or "link-rating" or "pendulum-scale" or "rush-maximum-atk" => "Matrix-Bold.otf",
            "atk-def-label" => "MatrixBoldSmallCaps.ttf",
            "type-line" or "spell-trap-label" or "effect-bold" => "Stone Serif Semibold.ttf",
            "effect-italic" => "Stone Serif Italic.ttf",
            "rush-card-name" or "rush-section-label" or "rush-type-line" => "FOT-Rodin Pro M.ttf",

            // Pokémon TCG
            "pokemon-name" or "pokemon-attack-name" or "pokemon-stage" => "GillSansBold.ttf",
            "pokemon-hp" or "pokemon-attack-damage" => "Futura-Bold.ttf",
            "pokemon-flavor" => "GillSansItalic.ttf",
            "pokemon-body" or "pokemon-small" or "pokemon-illustrator" or "pokemon-rule" => "GillSans.ttf",

            // Magic: The Gathering
            "mtg-name" or "mtg-type-line" or "mtg-pt" => "Beleren2016-Bold.ttf",
            "mtg-header" or "mtg-small-caps" => "Beleren2016SmallCaps-Bold.ttf",
            "mtg-rules" or "mtg-flavor" or "mtg-body" or "mtg-small" or "mtg-collector" => "Mplantin.ttf",

            _ => "Stone Serif Regular.ttf",
        };

        var resourceName = $"CardMaker.Infrastructure.Resources.Fonts.{fileName}";
        using var stream = typeof(FontService).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        return mem.ToArray();
    }

    public async Task<bool> RemoveAsync(Guid fontAssetId, CancellationToken cancellationToken = default)
    {
        var font = await db.FontAssets.FirstOrDefaultAsync(f => f.Id == fontAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (font is null)
        {
            return false;
        }

        db.FontAssets.Remove(font);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task<byte[]?> ReadAssetAsync(string sha256, CancellationToken cancellationToken)
    {
        var stream = await store.OpenReadAsync(sha256, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer.ToArray();
        }
    }

    internal static string? NormalizeAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return null;
        }

        var trimmed = alias.Trim().ToLowerInvariant();
        return AliasPattern().IsMatch(trimmed) ? trimmed : null;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();
}
