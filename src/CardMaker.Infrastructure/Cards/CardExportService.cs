using System.Text.Json;
using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Application.Cards;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using CardMaker.Rendering;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CardMaker.Infrastructure.Cards;

public sealed class CardExportService(
    CardMakerDbContext db,
    IAssetStore store,
    IFontCatalog fonts,
    IDecodedImageCache imageCache,
    CardRenderer renderer,
    PdfExporter pdfExporter) : ICardExportService
{
    private static readonly JsonSerializerOptions JsonOptions = LayoutSerializer.Options;

    public async Task<CardExportResult> ExportCardAsync(
        Guid cardId,
        string userId,
        CardExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(options);

        var card = await db.Cards.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.TemplateVersion)
            .Include(c => c.BackTemplateVersion)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.OwnerUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (card is null)
        {
            return new CardExportResult(false, null, null, null, $"Carta con ID '{cardId}' non trovata.");
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, CardValue>>(card.ValuesJson, JsonOptions) ?? [];

        // 1. Risoluzione Front Layout
        var frontLayout = LayoutSerializer.Deserialize(card.TemplateVersion.LayoutJson);
        if (frontLayout is null)
        {
            return new CardExportResult(false, null, null, null, "Layout front non valido.");
        }

        using var frontResources = await LoadResourcesAsync(frontLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);

        var frontResult = renderer.Render(new CardRenderRequest
        {
            Layout = frontLayout,
            Values = values,
            Resources = frontResources,
            Dpi = Math.Clamp(options.Dpi, 72, 1200),
            IncludeBleed = options.IncludeBleed,
            RoundCorners = options.RoundCorners,
            Format = options.Format == RenderFormat.Jpg ? RenderOutputFormat.Jpeg : RenderOutputFormat.Png,
        });

        if (frontResult.Content is null || frontResult.Content.Length == 0)
        {
            return new CardExportResult(false, null, null, null, "Rendering del fronte non riuscito.");
        }

        var safeTitle = string.Join("_", card.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim().Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "card";
        }

        // 2. Export in base al formato
        if (options.Format == RenderFormat.Pdf)
        {
            CardRenderResult? backResult = null;
            if (options.BothFaces && card.BackTemplateVersion is not null)
            {
                var backLayout = LayoutSerializer.Deserialize(card.BackTemplateVersion.LayoutJson);
                if (backLayout is not null)
                {
                    using var backResources = await LoadResourcesAsync(backLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
                    backResult = renderer.Render(new CardRenderRequest
                    {
                        Layout = backLayout,
                        Values = values,
                        Resources = backResources,
                        Dpi = Math.Clamp(options.Dpi, 72, 1200),
                        IncludeBleed = options.IncludeBleed,
                        RoundCorners = options.RoundCorners,
                        Format = RenderOutputFormat.Png,
                    });
                }
            }

            var pdfBytes = pdfExporter.Export(frontResult, backResult);
            return new CardExportResult(true, pdfBytes, "application/pdf", $"{safeTitle}.pdf", null);
        }

        if (options.Face == CardFace.Back && card.BackTemplateVersion is not null)
        {
            var backLayout = LayoutSerializer.Deserialize(card.BackTemplateVersion.LayoutJson);
            if (backLayout is not null)
            {
                using var backResources = await LoadResourcesAsync(backLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
                var backResult = renderer.Render(new CardRenderRequest
                {
                    Layout = backLayout,
                    Values = values,
                    Resources = backResources,
                    Dpi = Math.Clamp(options.Dpi, 72, 1200),
                    IncludeBleed = options.IncludeBleed,
                    RoundCorners = options.RoundCorners,
                    Format = options.Format == RenderFormat.Jpg ? RenderOutputFormat.Jpeg : RenderOutputFormat.Png,
                });

                var ext = options.Format == RenderFormat.Jpg ? "jpg" : "png";
                var mime = options.Format == RenderFormat.Jpg ? "image/jpeg" : "image/png";
                return new CardExportResult(true, backResult.Content, mime, $"{safeTitle}_back.{ext}", null);
            }
        }

        var extension = options.Format == RenderFormat.Jpg ? "jpg" : "png";
        var mimeType = options.Format == RenderFormat.Jpg ? "image/jpeg" : "image/png";
        return new CardExportResult(true, frontResult.Content, mimeType, $"{safeTitle}.{extension}", null);
    }

    private async Task<PreloadedRenderResources> LoadResourcesAsync(
        CardLayout layout,
        IReadOnlyDictionary<string, CardValue> values,
        Guid gameId,
        CancellationToken cancellationToken)
    {
        var resources = new PreloadedRenderResources();
        var (assetIds, assetKeys, fontAliases, symbols) = LayoutReferences.Collect(layout, values);

        if (assetIds.Count > 0)
        {
            var assets = await db.Assets.AsNoTracking()
                .Where(a => assetIds.Contains(a.Id))
                .Select(a => new { a.Id, a.Sha256 })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var asset in assets)
            {
                var image = await GetOrDecodeAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
                if (image is not null)
                {
                    resources.AddImage(asset.Id, image, owned: false);
                }
            }
        }

        foreach (var key in assetKeys)
        {
            var fileName = key + ".png";
            var asset = await db.Assets.AsNoTracking()
                .Where(a => a.OriginalFileName == fileName)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => new { a.Sha256 })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (asset is not null)
            {
                var image = await GetOrDecodeAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
                if (image is not null)
                {
                    resources.AddImageKey(key, image, owned: false);
                }
            }
        }

        foreach (var (setKey, symbolKey) in symbols)
        {
            var symbol = await db.Symbols.AsNoTracking()
                .Include(s => s.SymbolSet)
                .Include(s => s.Asset)
                .Where(s => s.SymbolSet.Key == setKey && s.Key == symbolKey && s.Asset != null)
                .Select(s => new { Sha = s.Asset!.Sha256 })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (symbol is not null)
            {
                var image = await GetOrDecodeAsync(symbol.Sha, cancellationToken).ConfigureAwait(false);
                if (image is not null)
                {
                    resources.AddSymbol(setKey, symbolKey, image, owned: false);
                }
            }
        }

        foreach (var alias in fontAliases)
        {
            var bytes = await fonts.GetBytesByAliasAsync(gameId, alias, cancellationToken).ConfigureAwait(false);
            if (bytes is not null)
            {
                resources.AddFont(alias, bytes);
            }
        }

        return resources;
    }

    private async Task<SKImage?> GetOrDecodeAsync(string sha256, CancellationToken cancellationToken)
    {
        var cached = imageCache.TryGet(sha256);
        if (cached is not null)
        {
            return cached;
        }

        var stream = await store.OpenReadAsync(sha256, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            using var data = SKData.CreateCopy(buffer.ToArray());
            var image = SKImage.FromEncodedData(data);
            if (image is not null)
            {
                imageCache.Store(sha256, image);
            }
            return image;
        }
    }
}
